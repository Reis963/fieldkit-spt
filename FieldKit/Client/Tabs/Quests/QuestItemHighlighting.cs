
namespace FieldKit
{
    public sealed partial class Plugin
    {
        private readonly List<QuestMarkerEntry> _questMarkerEntries =
            new List<QuestMarkerEntry>(64);
        private readonly Dictionary<string, int> _questMarkerIndexes =
            new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, List<QuestConditionRecord>>
            _questFindItemConditions =
                new Dictionary<string, List<QuestConditionRecord>>(
                    StringComparer.Ordinal);
        private ExperienceTrigger[] _questExperienceTriggers =
            new ExperienceTrigger[0];
        private PlaceItemTrigger[] _questPlaceItemTriggers =
            new PlaceItemTrigger[0];
        private bool _questStartupRefreshPending;
        private float _nextQuestTriggerRefresh;
        private float _nextQuestObjectiveRefresh;
        private bool _questObjectiveDataDirty = true;

        private void AppendQuestHighlights(
            Vector3 localPosition,
            ref int labelIndex)
        {
            if (!IsQuestOverlayEnabled() ||
                _world == null ||
                _localPlayer == null)
            {
                _questMarkerEntries.Clear();
                return;
            }

            RefreshQuestObjectiveEntries();

            float maxDistance = _questItemRenderDistance.Value;
            float maxDistanceSq = maxDistance * maxDistance;
            for (int i = 0; i < _questMarkerEntries.Count; i++)
            {
                QuestMarkerEntry entry = _questMarkerEntries[i];
                if (!IsActiveQuestMarker(entry))
                    continue;

                Vector3 worldPosition = entry.Target.position;
                float distanceSq =
                    (worldPosition - localPosition).sqrMagnitude;
                if (distanceSq > maxDistanceSq)
                    continue;

                Vector2 markerPosition;
                if (!TryWorldPointToCanvas(
                        _camera,
                        _canvasRect,
                        worldPosition,
                        out markerPosition))
                    continue;

                Color color = GetQuestMarkerColor(entry.Kind);
                AddQuestMarker(markerPosition, color, entry.Kind);

                string text = GetQuestMarkerText(
                    entry,
                    Mathf.Sqrt(distanceSq));
                if (string.IsNullOrEmpty(text))
                    continue;

                Text label = GetLabel(labelIndex++);
                RectTransform labelRect =
                    (RectTransform)label.transform;
                label.text = text;
                label.color = color;
                label.fontSize = _questItemLabelSize.Value;
                label.supportRichText = false;
                labelRect.pivot = new Vector2(0.5f, 0f);
                labelRect.anchoredPosition =
                    markerPosition + new Vector2(0f, 9f);
                labelRect.sizeDelta = new Vector2(700f, 120f);
                label.gameObject.SetActive(true);
            }
        }

        private void RefreshQuestObjectiveEntries()
        {
            if (!_questObjectiveDataDirty &&
                Time.unscaledTime < _nextQuestObjectiveRefresh)
                return;

            _questObjectiveDataDirty = false;
            _nextQuestObjectiveRefresh = Time.unscaledTime + 1f;
            _questMarkerEntries.Clear();
            _questMarkerIndexes.Clear();
            _questFindItemConditions.Clear();

            Profile profile = _localPlayer == null
                ? null
                : _localPlayer.Profile;
            if (profile == null ||
                profile.QuestsData == null ||
                _world == null)
                return;

            QuestDataClass[] startedQuests = profile.QuestsData
                .Where(quest =>
                    quest != null &&
                    quest.Status == EQuestStatus.Started &&
                    quest.Template != null)
                .ToArray();
            if (startedQuests.Length == 0)
                return;

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.isLoaded)
                return;

            if (_questItemHighlighting.Value)
            {
                RefreshFindItemMarkers(
                    startedQuests,
                    _world);
            }

            if (!_questLocationHighlighting.Value)
                return;


            if (_questVisitLocations.Value)
            {
                RefreshVisitLocationMarkers(
                    startedQuests,
                    profile);
            }

            if (_questPlaceLocations.Value)
            {
                RefreshPlaceOrRepairMarkers(
                    startedQuests,
                    profile);
            }
        }

        private void RefreshFindItemMarkers(
            QuestDataClass[] startedQuests,
            GameWorld world)
        {
            for (int questIndex = 0;
                 questIndex < startedQuests.Length;
                 questIndex++)
            {
                QuestDataClass quest = startedQuests[questIndex];
                ConditionFindItem[] conditions =
                    GetFinishConditions<ConditionFindItem>(quest);
                for (int conditionIndex = 0;
                     conditionIndex < conditions.Length;
                     conditionIndex++)
                {
                    ConditionFindItem condition =
                        conditions[conditionIndex];
                    if (IsQuestConditionCompleted(quest, condition) ||
                        condition.target == null)
                        continue;

                    foreach (string targetId in condition.target)
                    {
                        if (string.IsNullOrEmpty(targetId))
                            continue;

                        List<QuestConditionRecord> records;
                        if (!_questFindItemConditions.TryGetValue(
                                targetId,
                                out records))
                        {
                            records = new List<QuestConditionRecord>(2);
                            _questFindItemConditions[targetId] = records;
                        }

                        records.Add(new QuestConditionRecord
                        {
                            Condition = condition,
                            Quest = quest
                        });
                    }
                }
            }

            if (_questFindItemConditions.Count == 0 ||
                world.LootItems == null)
                return;

            int lootCount = world.LootItems.Count;
            for (int i = 0; i < lootCount; i++)
            {
                LootItem loot = world.LootItems.GetByIndex(i);
                if (!IsActiveQuestItem(loot))
                    continue;

                string templateId = loot.Item.TemplateId.ToString();
                List<QuestConditionRecord> records;
                if (!_questFindItemConditions.TryGetValue(
                        templateId,
                        out records))
                    continue;

                string key = "item:" + loot.GetInstanceID();
                for (int recordIndex = 0;
                     recordIndex < records.Count;
                     recordIndex++)
                {
                    QuestConditionRecord record = records[recordIndex];
                    AddOrMergeQuestMarker(
                        key,
                        QuestMarkerKind.Item,
                        loot.transform,
                        loot,
                        BuildQuestConditionLabel(
                            record.Condition,
                            record.Quest));
                }
            }
        }

        private void RefreshVisitLocationMarkers(
            QuestDataClass[] startedQuests,
            Profile profile)
        {
            for (int questIndex = 0;
                 questIndex < startedQuests.Length;
                 questIndex++)
            {
                QuestDataClass quest = startedQuests[questIndex];
                ConditionCounterCreator[] counters =
                    GetFinishConditions<ConditionCounterCreator>(quest);
                for (int counterIndex = 0;
                     counterIndex < counters.Length;
                     counterIndex++)
                {
                    ConditionCounterCreator counter =
                        counters[counterIndex];
                    if (IsQuestConditionCompleted(quest, counter) ||
                        counter.Conditions == null)
                        continue;

                    ConditionVisitPlace[] visits = counter.Conditions
                        .OfType<ConditionVisitPlace>()
                        .ToArray();
                    for (int visitIndex = 0;
                         visitIndex < visits.Length;
                         visitIndex++)
                    {
                        ConditionVisitPlace visit = visits[visitIndex];
                        ExperienceTrigger trigger =
                            FindExperienceTrigger(visit.target);
                        if (trigger == null ||
                            HasVisitedTrigger(profile, trigger.Id))
                            continue;

                        AddOrMergeQuestMarker(
                            "visit:" + trigger.GetInstanceID(),
                            QuestMarkerKind.Visit,
                            trigger.transform,
                            null,
                            BuildQuestConditionLabel(counter, quest));
                        break;
                    }
                }
            }
        }

        private void RefreshPlaceOrRepairMarkers(
            QuestDataClass[] startedQuests,
            Profile profile)
        {
            if (profile.Inventory == null)
                return;

            Item[] playerItems = profile.Inventory
                .GetPlayerItems()
                .ToArray();
            for (int questIndex = 0;
                 questIndex < startedQuests.Length;
                 questIndex++)
            {
                QuestDataClass quest = startedQuests[questIndex];
                ConditionZone[] conditions =
                    GetFinishConditions<ConditionZone>(quest);
                for (int conditionIndex = 0;
                     conditionIndex < conditions.Length;
                     conditionIndex++)
                {
                    ConditionZone condition = conditions[conditionIndex];
                    if (IsQuestConditionCompleted(quest, condition) ||
                        condition.target == null ||
                        !HasRequiredQuestItem(
                            playerItems,
                            condition.target))
                        continue;

                    PlaceItemTrigger trigger =
                        FindPlaceItemTrigger(condition.zoneId);
                    if (trigger == null)
                        continue;

                    AddOrMergeQuestMarker(
                        "place:" + trigger.GetInstanceID(),
                        QuestMarkerKind.Place,
                        trigger.transform,
                        null,
                        BuildQuestConditionLabel(condition, quest));
                }
            }
        }

        private void RefreshQuestTriggerCaches()
        {
            _questExperienceTriggers =
                Object.FindObjectsOfType<ExperienceTrigger>();
            _questPlaceItemTriggers =
                Object.FindObjectsOfType<PlaceItemTrigger>();
            InvalidateQuestObjectiveData();
            _lastRenderFrame = -1;
        }

        private void UpdateQuestStartupRefresh()
        {
            if (!_questStartupRefreshPending || _world == null ||
                Time.unscaledTime < _nextQuestTriggerRefresh)
                return;

            _questStartupRefreshPending = false;
            RefreshQuestTriggerCaches();
        }

        private ExperienceTrigger FindExperienceTrigger(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            for (int i = 0; i < _questExperienceTriggers.Length; i++)
            {
                ExperienceTrigger trigger =
                    _questExperienceTriggers[i];
                if (trigger != null && trigger.Id == id)
                    return trigger;
            }

            return null;
        }

        private PlaceItemTrigger FindPlaceItemTrigger(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            for (int i = 0; i < _questPlaceItemTriggers.Length; i++)
            {
                PlaceItemTrigger trigger =
                    _questPlaceItemTriggers[i];
                if (trigger != null && trigger.Id == id)
                    return trigger;
            }

            return null;
        }

        private static bool HasVisitedTrigger(
            Profile profile,
            string triggerId)
        {
            if (profile == null || string.IsNullOrEmpty(triggerId))
                return false;

            try
            {
                return profile.Stats.Eft.OverallCounters.GetInt(
                    CounterTag.TriggerVisited,
                    triggerId) > 0;
            }
            catch
            {
                return false;
            }
        }

        private static bool HasRequiredQuestItem(
            Item[] playerItems,
            IEnumerable<string> targetIds)
        {
            if (playerItems == null || targetIds == null)
                return false;

            HashSet<string> targets = new HashSet<string>(
                targetIds,
                StringComparer.Ordinal);
            for (int i = 0; i < playerItems.Length; i++)
            {
                Item item = playerItems[i];
                if (item != null &&
                    targets.Contains(item.TemplateId.ToString()))
                    return true;
            }

            return false;
        }

        private static T[] GetFinishConditions<T>(QuestDataClass quest)
            where T : Condition
        {
            try
            {
                if (quest == null ||
                    quest.Template == null ||
                    quest.Template.Conditions == null)
                    return new T[0];

                return quest.Template
                    .Conditions[EQuestStatus.AvailableForFinish]
                    .OfType<T>()
                    .ToArray();
            }
            catch
            {
                return new T[0];
            }
        }

        private static bool IsQuestConditionCompleted(
            QuestDataClass quest,
            Condition condition)
        {
            return quest != null &&
                   condition != null &&
                   quest.CompletedConditions != null &&
                   quest.CompletedConditions.Contains(condition.id);
        }

        private string BuildQuestConditionLabel(
            Condition condition,
            QuestDataClass quest)
        {
            string objective = "";
            string questName = "";
            if (_questShowObjective.Value && condition != null)
            {
                try
                {
                    objective = condition.FormattedDescription;
                }
                catch { }
            }

            if (_questShowQuestName.Value &&
                quest != null &&
                quest.Template != null)
            {
                try
                {
                    questName = quest.Template.Name;
                }
                catch { }
            }

            if (!string.IsNullOrWhiteSpace(objective) &&
                !string.IsNullOrWhiteSpace(questName))
                return objective + "\n[" + questName + "]";
            if (!string.IsNullOrWhiteSpace(objective))
                return objective;
            if (!string.IsNullOrWhiteSpace(questName))
                return "[" + questName + "]";
            return "";
        }

        private void AddOrMergeQuestMarker(
            string key,
            QuestMarkerKind kind,
            Transform target,
            LootItem loot,
            string label)
        {
            if (target == null)
                return;

            int index;
            QuestMarkerEntry entry;
            if (_questMarkerIndexes.TryGetValue(key, out index))
            {
                entry = _questMarkerEntries[index];
            }
            else
            {
                entry = new QuestMarkerEntry
                {
                    Kind = kind,
                    Target = target,
                    Loot = loot
                };
                _questMarkerIndexes[key] = _questMarkerEntries.Count;
                _questMarkerEntries.Add(entry);
            }

            if (!string.IsNullOrWhiteSpace(label) &&
                !entry.Labels.Contains(label))
                entry.Labels.Add(label);
        }

        private string GetQuestMarkerText(
            QuestMarkerEntry entry,
            float distance)
        {
            string text = entry.Labels.Count == 0
                ? ""
                : string.Join("\n", entry.Labels);
            if (_questShowDistance.Value)
            {
                string distanceText = distance.ToString("0") + "m";
                text = string.IsNullOrEmpty(text)
                    ? distanceText
                    : text + "\n" + distanceText;
            }

            return text;
        }

        private static bool IsActiveQuestItem(LootItem loot)
        {
            return loot != null &&
                   loot.Item != null &&
                   loot.Item.QuestItem &&
                   loot.gameObject != null &&
                   loot.gameObject.activeInHierarchy;
        }

        private static bool IsActiveQuestMarker(QuestMarkerEntry entry)
        {
            if (entry == null || entry.Target == null)
                return false;
            if (entry.Kind == QuestMarkerKind.Item)
                return IsActiveQuestItem(entry.Loot);
            return entry.Target.gameObject != null;
        }

        private void AddQuestMarker(
            Vector2 position,
            Color color,
            QuestMarkerKind kind)
        {
            const float radius = 5f;
            switch (kind)
            {
                case QuestMarkerKind.Visit:
                    AddQuestMarkerLine(
                        position + new Vector2(0f, radius),
                        position + new Vector2(radius, 0f),
                        color);
                    AddQuestMarkerLine(
                        position + new Vector2(radius, 0f),
                        position + new Vector2(0f, -radius),
                        color);
                    AddQuestMarkerLine(
                        position + new Vector2(0f, -radius),
                        position + new Vector2(-radius, 0f),
                        color);
                    AddQuestMarkerLine(
                        position + new Vector2(-radius, 0f),
                        position + new Vector2(0f, radius),
                        color);
                    break;
                case QuestMarkerKind.Place:
                    AddQuestMarkerLine(
                        position + new Vector2(-radius, -radius),
                        position + new Vector2(radius, -radius),
                        color);
                    AddQuestMarkerLine(
                        position + new Vector2(radius, -radius),
                        position + new Vector2(radius, radius),
                        color);
                    AddQuestMarkerLine(
                        position + new Vector2(radius, radius),
                        position + new Vector2(-radius, radius),
                        color);
                    AddQuestMarkerLine(
                        position + new Vector2(-radius, radius),
                        position + new Vector2(-radius, -radius),
                        color);
                    break;
                default:
                    AddQuestMarkerLine(
                        position + new Vector2(-radius, -radius),
                        position + new Vector2(radius, radius),
                        color);
                    AddQuestMarkerLine(
                        position + new Vector2(-radius, radius),
                        position + new Vector2(radius, -radius),
                        color);
                    break;
            }
        }

        private void AddQuestMarkerLine(
            Vector2 start,
            Vector2 end,
            Color color)
        {
            _lines.Add(new LineCommand(
                start,
                end,
                color,
                2.5f));
        }

        private void InvalidateQuestObjectiveData()
        {
            _questObjectiveDataDirty = true;
            _nextQuestObjectiveRefresh = 0f;
        }

        private void ClearQuestObjectiveCaches()
        {
            _questMarkerEntries.Clear();
            _questMarkerIndexes.Clear();
            _questFindItemConditions.Clear();
            _questExperienceTriggers = new ExperienceTrigger[0];
            _questPlaceItemTriggers = new PlaceItemTrigger[0];
            _questStartupRefreshPending = false;
            _nextQuestTriggerRefresh = 0f;
            _questObjectiveDataDirty = true;
            _nextQuestObjectiveRefresh = 0f;
        }

        private enum QuestMarkerKind
        {
            Item,
            Visit,
            Place
        }

        private sealed class QuestMarkerEntry
        {
            public QuestMarkerKind Kind;
            public Transform Target;
            public LootItem Loot;
            public readonly List<string> Labels =
                new List<string>(2);
        }

        private sealed class QuestConditionRecord
        {
            public Condition Condition;
            public QuestDataClass Quest;
        }
    }
}
