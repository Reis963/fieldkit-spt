
namespace FieldKit
{
    public sealed partial class Plugin
    {
        private static bool TryWorldPointToCanvas(
            Camera camera,
            RectTransform canvasRect,
            Vector3 worldPosition,
            out Vector2 localPosition)
        {
            localPosition = default(Vector2);
            if (camera == null || canvasRect == null)
                return false;

            Vector3 cameraSpace =
                camera.transform.InverseTransformPoint(worldPosition);
            float safeNearPlane =
                Mathf.Max(0.01f, camera.nearClipPlane + 0.01f);
            if (cameraSpace.z <= safeNearPlane)
                return false;

            // EFT's SSAA/FSR path shrinks Camera.rect to the internal
            // rendering ratio. WorldToViewportPoint already provides the
            // normalized output projection, so applying Camera.rect or a
            // second render-resolution scale here offsets the overlay.
            Vector3 viewport =
                camera.WorldToViewportPoint(worldPosition);
            if (!IsFiniteProjectionPoint(viewport))
                return false;

            Rect canvas = canvasRect.rect;
            localPosition = new Vector2(
                canvas.xMin + viewport.x * canvas.width,
                canvas.yMin + viewport.y * canvas.height);
            return IsFiniteCanvasPoint(localPosition);
        }

        private static bool TryWorldPointToCameraPixels(
            Camera camera,
            Vector3 worldPosition,
            out Vector2 pixelPosition)
        {
            pixelPosition = default(Vector2);
            if (camera == null)
                return false;

            Vector3 viewport =
                camera.WorldToViewportPoint(worldPosition);
            if (viewport.z <= 0f ||
                !IsFiniteProjectionPoint(viewport))
                return false;

            int width = camera.targetTexture == null
                ? camera.pixelWidth
                : camera.targetTexture.width;
            int height = camera.targetTexture == null
                ? camera.pixelHeight
                : camera.targetTexture.height;
            if (width <= 0 || height <= 0)
                return false;

            pixelPosition = new Vector2(
                viewport.x * width,
                viewport.y * height);
            return IsFiniteCanvasPoint(pixelPosition);
        }

        private static bool IsFiniteProjectionPoint(Vector3 point)
        {
            return !float.IsNaN(point.x) &&
                   !float.IsInfinity(point.x) &&
                   !float.IsNaN(point.y) &&
                   !float.IsInfinity(point.y) &&
                   !float.IsNaN(point.z) &&
                   !float.IsInfinity(point.z);
        }

        private static bool IsFiniteCanvasPoint(Vector2 point)
        {
            return !float.IsNaN(point.x) &&
                   !float.IsInfinity(point.x) &&
                   !float.IsNaN(point.y) &&
                   !float.IsInfinity(point.y);
        }
    }
}
