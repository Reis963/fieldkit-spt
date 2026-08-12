
namespace FieldKit
{
    public sealed partial class Plugin : BaseUnityPlugin
    {
        private void RefreshCamera()
        {
            Camera main = Camera.main;
            if (IsUsableMainCamera(main))
            {
                SetMainCamera(main, "Camera.main");
                return;
            }

            Camera best = null;
            float bestScore = float.MinValue;

            Camera[] cameras = Resources.FindObjectsOfTypeAll<Camera>();

            foreach (Camera candidate in cameras)
            {
                if (!IsUsableMainCamera(candidate))
                    continue;

                float score = candidate.depth;

                if (candidate.CompareTag("MainCamera"))
                    score += 1000f;

                if (candidate.name.IndexOf("FPS", StringComparison.OrdinalIgnoreCase) >= 0)
                    score += 500f;

                if (candidate == _camera)
                    score += 0.25f;

                if (score > bestScore)
                {
                    best = candidate;
                    bestScore = score;
                }
            }

            SetMainCamera(best, "fallback");
        }

        private static bool IsUsableMainCamera(Camera candidate)
        {
            return candidate != null &&
                   candidate.enabled &&
                   candidate.gameObject.activeInHierarchy &&
                   !candidate.orthographic &&
                   candidate.targetTexture == null;
        }

        private void SetMainCamera(Camera camera, string source)
        {
            if (_camera == camera)
                return;

            Camera previous = _camera;
            _camera = camera;
            _lastRenderFrame = -1;
            _scopeRefreshRequested = true;

            for (int i = 0; i < _targets.Count; i++)
            {
                Target target = _targets[i];
                target.IsOnMainScreen = false;
                target.HasSmoothedScreenRect = false;
                target.HasVisibility = false;
                target.HasPerBoneVisibility = false;
                target.NextScreenCheck = 0f;
                target.NextVisibilityUpdate = 0f;
            }

            if (_cameraDebug == null || !_cameraDebug.Value)
                return;

            LogSource.LogInfo(
                "[Camera Debug] Main projection camera changed from " +
                DescribeCamera(previous) + " to " +
                DescribeCamera(camera) + " via " + source + ".");
        }

        private static string DescribeCamera(Camera camera)
        {
            if (camera == null)
                return "<null>";

            return string.Format(
                "\"{0}\" fov={1:0.0} pixel={2}x{3} scaled={4}x{5} " +
                "screen={6}x{7} rect=({8:0},{9:0},{10:0},{11:0}) " +
                "depth={12:0.0} target={13}",
                camera.name,
                camera.fieldOfView,
                camera.pixelWidth,
                camera.pixelHeight,
                camera.scaledPixelWidth,
                camera.scaledPixelHeight,
                Screen.width,
                Screen.height,
                camera.pixelRect.x,
                camera.pixelRect.y,
                camera.pixelRect.width,
                camera.pixelRect.height,
                camera.depth,
                camera.targetTexture == null
                    ? "<screen>"
                    : camera.targetTexture.name + "(" +
                      camera.targetTexture.width + "x" +
                      camera.targetTexture.height + ")");
        }

        private static void LogActiveCameras(Camera[] cameras)
        {
            StringBuilder message = new StringBuilder(
                "[Camera Debug] Active cameras:");

            for (int i = 0; i < cameras.Length; i++)
            {
                message.Append("\n  ");
                message.Append(DescribeCamera(cameras[i]));
            }

            LogSource.LogInfo(message.ToString());
        }

        private void RefreshScopeOverlays()
        {
            if (_scopeEsp == null || !_scopeEsp.Value)
            {
                DestroyScopeOverlays();
                return;
            }

            for (int i = 0; i < _scopeOverlays.Count; i++)
                _scopeOverlays[i].Seen = false;

            Camera[] activeCameras = Camera.allCameras;
            if (_cameraDebug.Value)
                LogActiveCameras(activeCameras);

            foreach (Camera candidate in activeCameras)
            {
                if (!IsScopeCamera(candidate))
                    continue;

                ScopeOverlay existing = null;

                for (int i = 0; i < _scopeOverlays.Count; i++)
                {
                    if (_scopeOverlays[i].Camera == candidate)
                    {
                        existing = _scopeOverlays[i];
                        break;
                    }
                }

                if (existing != null)
                {
                    existing.Seen = true;
                    existing.LastSeenTime = Time.unscaledTime;

                    Renderer currentLens = ResolveOpticLens(candidate);

                    if (currentLens != null &&
                        currentLens != existing.LensRenderer)
                    {
                        existing.LensRenderer = currentLens;
                        existing.HasLensScreenRect = false;
                        if (_cameraDebug.Value)
                        {
                            LogSource.LogInfo(
                                "[Scope Debug] Active optic lens changed to: " +
                                TransformPath(currentLens.transform));
                        }
                    }
                    else if (currentLens == null &&
                             existing.LensRenderer != null &&
                             !existing.LensRenderer.gameObject.activeInHierarchy)
                    {
                        existing.LensRenderer = null;
                        existing.HasLensScreenRect = false;
                    }

                    if (existing.Pass.EnsureAttached() &&
                        _cameraDebug.Value)
                    {
                        LogSource.LogInfo(
                            "[Scope Debug] Reattached optic command buffer: " +
                            candidate.name);
                    }

                    continue;
                }
                MakeRoomForScopeOverlay();

                if (_scopeOverlays.Count < 4)
                {
                    if (_cameraDebug.Value)
                    {
                        LogSource.LogInfo(
                            "[Scope Debug] Camera: " + candidate.name +
                            ", texture=" + candidate.targetTexture.name);
                    }

                    ScopeOverlay created =
                        CreateScopeOverlay(candidate);
                    if (created != null)
                        _scopeOverlays.Add(created);
                }
            }

            for (int i = _scopeOverlays.Count - 1; i >= 0; i--)
            {
                ScopeOverlay overlay = _scopeOverlays[i];
                if (overlay.Camera != null &&
                    overlay.Pass != null &&
                    (overlay.Seen ||
                     Time.unscaledTime - overlay.LastSeenTime < 15f))
                    continue;

                DestroyScopeOverlay(overlay);
                _scopeOverlays.RemoveAt(i);
            }
        }

        private void MakeRoomForScopeOverlay()
        {
            if (_scopeOverlays.Count < 4)
                return;

            int oldestIndex = -1;
            float oldestTime = float.MaxValue;

            for (int i = 0; i < _scopeOverlays.Count; i++)
            {
                ScopeOverlay overlay = _scopeOverlays[i];

                if (overlay.Seen || overlay.LastSeenTime >= oldestTime)
                    continue;

                oldestTime = overlay.LastSeenTime;
                oldestIndex = i;
            }

            if (oldestIndex < 0)
                return;

            DestroyScopeOverlay(_scopeOverlays[oldestIndex]);
            _scopeOverlays.RemoveAt(oldestIndex);
        }

        private bool IsScopeCamera(Camera candidate)
        {
            if (candidate == null ||
                candidate == _camera ||
                !candidate.enabled ||
                !candidate.gameObject.activeInHierarchy ||
                candidate.orthographic ||
                candidate.targetTexture == null)
                return false;

            return candidate.name.IndexOf(
                       "Optic", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   candidate.targetTexture.name.IndexOf(
                       "Optic", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string TransformPath(Transform transform)
        {
            if (transform == null)
                return "<null>";

            StringBuilder path = new StringBuilder(transform.name);
            Transform parent = transform.parent;

            while (parent != null)
            {
                path.Insert(0, '/');
                path.Insert(0, parent.name);
                parent = parent.parent;
            }

            return path.ToString();
        }

        private ScopeOverlay CreateScopeOverlay(Camera camera)
        {
            if (camera == null)
                return null;

            if (_font == null)
                _font = LoadFont();
            if (_font == null)
            {
                LogSource.LogWarning(
                    "Scope ESP skipped because no Unity font is available.");
                return null;
            }

            try
            {
                return new ScopeOverlay
                {
                    Camera = camera,
                    Pass = new ScopeRenderPass(camera, _font),
                    LensRenderer = ResolveOpticLens(camera),
                    LastSeenTime = Time.unscaledTime,
                    Seen = true
                };
            }
            catch (Exception exception)
            {
                LogSource.LogWarning(
                    "Could not create scope ESP render pass: " +
                    exception.Message);
                return null;
            }
        }

        private static Renderer ResolveOpticLens(Camera camera)
        {
            if (camera == null)
                return null;

            Component[] components = camera.GetComponents<Component>();

            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];

                if (component == null ||
                    component.GetType().Name != "OpticComponentUpdater")
                    continue;

                try
                {
                    FieldInfo[] fields = component.GetType().GetFields(
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic);

                    for (int fieldIndex = 0;
                         fieldIndex < fields.Length;
                         fieldIndex++)
                    {
                        FieldInfo field = fields[fieldIndex];

                        if (field.FieldType.Name != "OpticSight")
                            continue;

                        object sight = field.GetValue(component);

                        if (sight == null)
                            continue;

                        FieldInfo lensField = sight.GetType().GetField(
                            "LensRenderer",
                            BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.NonPublic);

                        if (lensField != null)
                            return lensField.GetValue(sight) as Renderer;
                    }
                }
                catch (Exception exception)
                {
                    LogSource.LogWarning(
                        "[Scope Debug] Failed to resolve optic lens: " +
                        exception.Message);
                }
            }

            return null;
        }

        private bool TryGetActiveScopeMask(out Rect localRect)
        {
            localRect = default(Rect);

            for (int i = 0; i < _scopeOverlays.Count; i++)
            {
                ScopeOverlay overlay = _scopeOverlays[i];

                if (!IsScopeCamera(overlay.Camera))
                    continue;

                if (overlay.LensRenderer == null &&
                    Time.unscaledTime >= overlay.NextLensResolve)
                {
                    overlay.NextLensResolve = Time.unscaledTime + 1f;
                    overlay.LensRenderer = ResolveOpticLens(overlay.Camera);
                }

                Rect localLensRect;

                if (!TryProjectRendererBounds(
                        overlay.LensRenderer,
                        _camera,
                        _canvasRect,
                        out localLensRect))
                    continue;
                float insetX = localLensRect.width * 0.06f;
                float insetY = localLensRect.height * 0.06f;
                localLensRect.xMin += insetX;
                localLensRect.xMax -= insetX;
                localLensRect.yMin += insetY;
                localLensRect.yMax -= insetY;

                localRect = localLensRect;
                overlay.LastLensScreenRect = localLensRect;
                overlay.HasLensScreenRect = true;
                return true;
            }

            return false;
        }

        private static bool TryProjectRendererBounds(
            Renderer renderer,
            Camera camera,
            RectTransform canvasRect,
            out Rect localRect)
        {
            localRect = default(Rect);

            if (renderer == null ||
                camera == null ||
                canvasRect == null)
                return false;

            Bounds bounds = renderer.bounds;
            Vector3 center = bounds.center;
            Vector3 extents = bounds.extents;
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;
            bool found = false;

            for (int x = -1; x <= 1; x += 2)
                for (int y = -1; y <= 1; y += 2)
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 point = center + Vector3.Scale(
                            extents, new Vector3(x, y, z));
                        Vector2 local;
                        if (!TryWorldPointToCanvas(
                                camera,
                                canvasRect,
                                point,
                                out local))
                            continue;

                        found = true;
                        minX = Mathf.Min(minX, local.x);
                        minY = Mathf.Min(minY, local.y);
                        maxX = Mathf.Max(maxX, local.x);
                        maxY = Mathf.Max(maxY, local.y);
                    }

            if (!found || maxX - minX < 10f || maxY - minY < 10f)
                return false;

            localRect = Rect.MinMaxRect(minX, minY, maxX, maxY);
            return true;
        }

        private static bool IsInsideEllipse(Vector2 point, Rect bounds)
        {
            if (bounds.width <= 0f || bounds.height <= 0f)
                return false;

            float x = (point.x - bounds.center.x) /
                      (bounds.width * 0.5f);
            float y = (point.y - bounds.center.y) /
                      (bounds.height * 0.5f);

            return x * x + y * y <= 1f;
        }

        private bool HasValidScopeProjection(Target target)
        {
            for (int i = 0; i < _scopeOverlays.Count; i++)
            {
                ScopeOverlay overlay = _scopeOverlays[i];
                Rect scopeRect;
                if (overlay != null &&
                    IsScopeCamera(overlay.Camera) &&
                    TryGetScopeScreenRect(
                        target, overlay.Camera, out scopeRect))
                    return true;
            }
            return false;
        }

        private void RenderScopeOverlays(
            Vector3 localPosition,
            float maxDistanceSq)
        {
            for (int overlayIndex = 0;
                 overlayIndex < _scopeOverlays.Count;
                 overlayIndex++)
            {
                ScopeOverlay overlay = _scopeOverlays[overlayIndex];

                if (!IsScopeCamera(overlay.Camera) ||
                    overlay.Pass == null)
                    continue;

                overlay.Boxes.Clear();
                overlay.Lines.Clear();
                overlay.Text.Clear();

                if (overlay.LensRenderer == null &&
                    Time.unscaledTime >= overlay.NextLensResolve)
                {
                    overlay.NextLensResolve = Time.unscaledTime + 1f;
                    overlay.LensRenderer = ResolveOpticLens(overlay.Camera);
                }

                Target scopeVisibilityFocus = FindScopeVisibilityFocus(
                    overlay.Camera,
                    localPosition,
                    maxDistanceSq);
                for (int i = 0; i < _targets.Count; i++)
                {
                    Target target = _targets[i];
                    Player player = target.Player;

                    if (player == null ||
                        !target.IsAlive ||
                        target.Root == null ||
                        !ShouldShow(target))
                        continue;

                    float distanceSq =
                        (target.Root.position - localPosition).sqrMagnitude;

                    if (distanceSq > maxDistanceSq)
                        continue;

                    Rect rect;

                    if (!TryGetScopeScreenRect(
                        target, overlay.Camera, out rect))
                        continue;

                    BoneVisibility scopeVisibleBones =
                        GetScopeVisibleBones(
                            target,
                            overlay.Camera,
                            ReferenceEquals(target, scopeVisibilityFocus));
                    Color hiddenScopeColor =
                        GetRoleColor(target, true);
                    Color scopeColor =
                        scopeVisibleBones != BoneVisibility.None
                            ? target.Color
                            : hiddenScopeColor;
                    if (_showBoxes.Value)
                    {
                        overlay.Boxes.Add(new BoxCommand(
                            rect, scopeColor));
                    }
                    if (_showHealthBar.Value)
                    {
                        AddHealthBar(
                            rect,
                            target.HealthRatio,
                            overlay.Lines);
                    }
                    string label = GetTargetEspText(
                        target,
                        Mathf.Sqrt(distanceSq));
                    if (!string.IsNullOrEmpty(label))
                    {
                        overlay.Text.Add(new TextCommand(
                            new Vector2(rect.center.x, rect.yMax + 5f),
                            label,
                            scopeColor));
                    }

                }

                overlay.Pass.SetGeometry(
                    overlay.Boxes,
                    overlay.Lines,
                    overlay.Text,
                    _lineThickness.Value,
                    _scopeColorBrightness.Value,
                    _fontSize.Value);

                if (_cameraDebug.Value &&
                    Time.unscaledTime >= overlay.NextDebugLog)
                {
                    overlay.NextDebugLog = Time.unscaledTime + 2f;
                    string lensBounds = overlay.HasLensScreenRect
                        ? string.Format(
                            "({0:0},{1:0},{2:0},{3:0})",
                            overlay.LastLensScreenRect.x,
                            overlay.LastLensScreenRect.y,
                            overlay.LastLensScreenRect.width,
                            overlay.LastLensScreenRect.height)
                        : "<unresolved>";

                    LogSource.LogInfo(string.Format(
                        "[Scope Debug] camera=\"{0}\" boxes={1} lines={2} " +
                        "target={3} lens={4}",
                        overlay.Camera.name,
                        overlay.Boxes.Count,
                        overlay.Lines.Count,
                        overlay.Camera.targetTexture == null
                            ? "<screen>"
                            : overlay.Camera.targetTexture.name,
                        lensBounds));
                }
            }
        }

        private Target FindScopeVisibilityFocus(
            Camera camera,
            Vector3 localPosition,
            float maxDistanceSq)
        {
            Target closest = null;
            float closestDistanceSq = float.MaxValue;
            Vector2 screenCenter = new Vector2(
                camera.pixelWidth * 0.5f,
                camera.pixelHeight * 0.5f);
            for (int i = 0; i < _targets.Count; i++)
            {
                Target target = _targets[i];
                if (target == null ||
                    target.Player == null ||
                    !target.IsAlive ||
                    target.Root == null ||
                    !ShouldShow(target) ||
                    (target.Root.position - localPosition).sqrMagnitude >
                    maxDistanceSq)
                    continue;

                Rect rect;
                if (!TryGetScopeScreenRect(target, camera, out rect))
                    continue;

                float distanceSq =
                    (rect.center - screenCenter).sqrMagnitude;
                if (distanceSq >= closestDistanceSq)
                    continue;

                closest = target;
                closestDistanceSq = distanceSq;
            }

            return closest;
        }

        private void DestroyScopeOverlays()
        {
            for (int i = 0; i < _scopeOverlays.Count; i++)
                DestroyScopeOverlay(_scopeOverlays[i]);

            _scopeOverlays.Clear();
        }

        private static void DestroyScopeOverlay(ScopeOverlay overlay)
        {
            if (overlay != null && overlay.Pass != null)
            {
                overlay.Pass.Dispose();
                overlay.Pass = null;
            }

        }

    }
}
