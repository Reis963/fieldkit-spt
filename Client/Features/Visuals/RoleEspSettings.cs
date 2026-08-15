
namespace FieldKit
{
    public sealed partial class Plugin
    {
        private readonly Dictionary<string, EspRoleSettings>
            _espRolesByKey =
                new Dictionary<string, EspRoleSettings>(
                    StringComparer.OrdinalIgnoreCase);
        private readonly List<EspRoleSettings> _espRoles =
            new List<EspRoleSettings>(64);
        private readonly List<EspRoleGroup> _espRoleGroups =
            new List<EspRoleGroup>(8);
        private readonly Dictionary<string, EspRoleGroup>
            _espRoleGroupsByName =
                new Dictionary<string, EspRoleGroup>(
                    StringComparer.OrdinalIgnoreCase);
        private bool _espAllRolesExpanded;
        private const int CurrentEspPaletteVersion = 1;

        private void ConfigureRoleEsp()
        {
            AddRoleEsp("PMC-BEAR", "PMC - BEAR", EspKind.Pmc, "BEAR");
            AddRoleEsp("PMC-USEC", "PMC - USEC", EspKind.Pmc, "USEC");
            Array roles = Enum.GetValues(typeof(WildSpawnType));
            for (int i = 0; i < roles.Length; i++)
            {
                WildSpawnType role = (WildSpawnType)roles.GetValue(i);
                if (IsExcludedEspRole(role) ||
                    role == WildSpawnType.pmcBEAR ||
                    role == WildSpawnType.pmcUSEC)
                    continue;

                EspKind kind = RoleKind(role);
                string roleName = role.ToString();
                AddRoleEsp(
                    "ROLE-" + roleName,
                    RoleGroupName(role) + " - " + roleName,
                    kind,
                    roleName);
            }

            _espPaletteVersion = Config.Bind(
                "ESP Palette",
                "Preset Version",
                0,
                "Internal version of the applied FieldKit ESP palette.");
            ApplyEspPaletteMigration();
        }

        private void AddRoleEsp(
            string key,
            string label,
            EspKind kind,
            string configKey,
            bool followerSubcategory = false)
        {
            Color visible = GetRoleDefaultColor(key, kind);
            Color hidden = GetHiddenRoleDefaultColor(visible);
            EspRoleSettings settings = new EspRoleSettings
            {
                Key = key,
                Label = label,
                Group = label.Substring(0, label.IndexOf(" - ",
                    StringComparison.Ordinal)),
                FollowerSubcategory = followerSubcategory,
                Kind = kind,
                DefaultVisible = visible,
                DefaultHidden = hidden,
                Enabled = Config.Bind(
                    "ESP Roles",
                    configKey + " Enabled",
                    true,
                    "Render " + label + " targets."),
                VisibleColor = Config.Bind(
                    "ESP Role Colors",
                    configKey + " Visible",
                    "#" + ColorUtility.ToHtmlStringRGBA(visible),
                    "Visible ESP color for " + label + "."),
                HiddenColor = Config.Bind(
                    "ESP Role Colors",
                    configKey + " Hidden",
                    "#" + ColorUtility.ToHtmlStringRGBA(hidden),
                    "Occluded ESP color for " + label + ".")
            };
            _espRoles.Add(settings);
            _espRolesByKey.Add(key, settings);

            EspRoleGroup group;
            if (!_espRoleGroupsByName.TryGetValue(
                    settings.Group, out group))
            {
                group = new EspRoleGroup
                {
                    Name = settings.Group
                };
                _espRoleGroupsByName.Add(settings.Group, group);
                _espRoleGroups.Add(group);
            }
            group.Roles.Add(settings);
        }

        private static EspKind RoleKind(WildSpawnType role)
        {
            if (role == WildSpawnType.pmcBEAR ||
                role == WildSpawnType.pmcUSEC)
                return EspKind.Pmc;
            return IsOrdinaryScavRole(role)
                ? EspKind.Scav
                : EspKind.Boss;
        }

        private static string RoleGroupName(WildSpawnType role)
        {
            string name = role.ToString();
            if (role == WildSpawnType.pmcBEAR ||
                role == WildSpawnType.pmcUSEC)
                return "PMC";
            if (IsOrdinaryScavRole(role))
                return "Scav";
            if (string.Equals(name, "pmcBot",
                    StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("arenaFighter",
                    StringComparison.OrdinalIgnoreCase))
                return "Raider";
            if (string.Equals(name, "exUsec",
                    StringComparison.OrdinalIgnoreCase))
                return "Rogue";
            if (name.StartsWith("blackDiv",
                    StringComparison.OrdinalIgnoreCase))
                return "Black Division";
            if (string.Equals(name, "skier",
                    StringComparison.OrdinalIgnoreCase))
                return "RUAF";
            if (string.Equals(name, "peacemaker",
                    StringComparison.OrdinalIgnoreCase))
                return "UNTAR";
            if (string.Equals(name, "tagillaHelperAgro",
                    StringComparison.OrdinalIgnoreCase) ||
                IsFollowerRole(role))
                return "Boss Guard";
            if (name.StartsWith("infected",
                    StringComparison.OrdinalIgnoreCase))
            {
                return string.Equals(name, "infectedTagilla",
                        StringComparison.OrdinalIgnoreCase)
                    ? "Boss"
                    : "Infected";
            }
            if (name.StartsWith("boss", StringComparison.OrdinalIgnoreCase))
                return "Boss";
            if (name.StartsWith("sect", StringComparison.OrdinalIgnoreCase))
                return "Cultist";
            if (string.Equals(name, "gifter",
                    StringComparison.OrdinalIgnoreCase) ||
                name.IndexOf("ZryachiyEvent",
                    StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.StartsWith("spirit",
                    StringComparison.OrdinalIgnoreCase))
                return "Boss";
            return "Special";
        }

        private static bool IsFollowerRole(
            WildSpawnType role)
        {
            return role.ToString().StartsWith(
                "follower",
                StringComparison.OrdinalIgnoreCase);
        }

        private static Color GetRoleDefaultColor(string key, EspKind kind)
        {
            if (string.Equals(key, "PMC-BEAR",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(key, "ROLE-pmcBEAR",
                    StringComparison.OrdinalIgnoreCase))
                return PaletteColor(0x34, 0x98, 0xDB);
            if (string.Equals(key, "PMC-USEC",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(key, "ROLE-pmcUSEC",
                    StringComparison.OrdinalIgnoreCase))
                return PaletteColor(0x2E, 0xCC, 0x71);

            string roleName = key.StartsWith(
                    "ROLE-", StringComparison.OrdinalIgnoreCase)
                ? key.Substring(5)
                : key;
            if (IsOrdinaryScavRoleName(roleName))
                return PaletteColor(0xFF, 0xD4, 0x3B);
            if (string.Equals(roleName, "pmcBot",
                    StringComparison.OrdinalIgnoreCase) ||
                roleName.StartsWith("arenaFighter",
                    StringComparison.OrdinalIgnoreCase))
                return PaletteColor(0x9B, 0x59, 0xB6);
            if (string.Equals(roleName, "exUsec",
                    StringComparison.OrdinalIgnoreCase))
                return PaletteColor(0x00, 0xBC, 0xD4);
            if (roleName.StartsWith("blackDiv",
                    StringComparison.OrdinalIgnoreCase))
                return PaletteColor(0xF2, 0xF2, 0xF2);
            if (string.Equals(roleName, "skier",
                    StringComparison.OrdinalIgnoreCase))
                return PaletteColor(0xA0, 0x52, 0x52);
            if (string.Equals(roleName, "peacemaker",
                    StringComparison.OrdinalIgnoreCase))
                return PaletteColor(0x3F, 0x51, 0xB5);
            if (roleName.StartsWith("sect",
                    StringComparison.OrdinalIgnoreCase))
                return PaletteColor(0x61, 0x61, 0x61);
            if (roleName.StartsWith("infected",
                    StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(roleName, "infectedTagilla",
                    StringComparison.OrdinalIgnoreCase))
                return PaletteColor(0x8B, 0xC3, 0x4A);
            if (roleName.StartsWith("follower",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(roleName, "tagillaHelperAgro",
                    StringComparison.OrdinalIgnoreCase))
                return PaletteColor(0xFF, 0x8C, 0x00);
            if (roleName.StartsWith("boss",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(roleName, "gifter",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(roleName, "infectedTagilla",
                    StringComparison.OrdinalIgnoreCase) ||
                roleName.IndexOf("ZryachiyEvent",
                    StringComparison.OrdinalIgnoreCase) >= 0 ||
                roleName.StartsWith("spirit",
                    StringComparison.OrdinalIgnoreCase))
                return PaletteColor(0xF4, 0x43, 0x36);

            // Reserved for technical/test roles and any role introduced by a
            // future SPT update that does not yet have a semantic category.
            return PaletteColor(0xE9, 0x1E, 0x63);
        }

        private static bool IsOrdinaryScavRoleName(string roleName)
        {
            return string.Equals(roleName, "assault",
                       StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(roleName, "marksman",
                       StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(roleName, "cursedAssault",
                       StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(roleName, "assaultGroup",
                       StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(roleName, "crazyAssaultEvent",
                       StringComparison.OrdinalIgnoreCase);
        }

        private static Color PaletteColor(byte red, byte green, byte blue)
        {
            return new Color32(red, green, blue, 0xFF);
        }

        private static Color GetHiddenRoleDefaultColor(Color visible)
        {
            Color hidden = new Color(
                visible.r * 0.45f,
                visible.g * 0.45f,
                visible.b * 0.45f,
                0.75f);
            float brightest = Mathf.Max(
                hidden.r, Mathf.Max(hidden.g, hidden.b));
            if (brightest > 0f && brightest < 0.18f)
            {
                float lift = 0.18f / brightest;
                hidden.r = Mathf.Clamp01(hidden.r * lift);
                hidden.g = Mathf.Clamp01(hidden.g * lift);
                hidden.b = Mathf.Clamp01(hidden.b * lift);
            }
            return hidden;
        }

        private static bool IsExcludedEspRole(WildSpawnType role)
        {
            return string.Equals(
                role.ToString(),
                "shooterBTR",
                StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsExcludedEspRoleKey(string roleKey)
        {
            return string.Equals(
                roleKey,
                "ROLE-shooterBTR",
                StringComparison.OrdinalIgnoreCase);
        }

        private void ApplyEspPaletteMigration()
        {
            if (_espPaletteVersion.Value >= CurrentEspPaletteVersion)
                return;

            for (int i = 0; i < _espRoles.Count; i++)
            {
                EspRoleSettings role = _espRoles[i];
                role.VisibleColor.Value =
                    "#" + ColorUtility.ToHtmlStringRGBA(
                        role.DefaultVisible);
                role.HiddenColor.Value =
                    "#" + ColorUtility.ToHtmlStringRGBA(
                        role.DefaultHidden);
            }

            _espPaletteVersion.Value = CurrentEspPaletteVersion;
            Config.Save();
        }

        private string GetRoleKey(Player player)
        {
            try
            {
                if (player.AIData != null &&
                    player.AIData.IsAI)
                {
                    WildSpawnType role =
                        player.Profile.Info.Settings.Role;
                    return GetConfiguredRoleKey(role);
                }
            }
            catch { }
            if (player.Side == EPlayerSide.Bear)
                return "PMC-BEAR";
            if (player.Side == EPlayerSide.Usec)
                return "PMC-USEC";
            try
            {
                return GetConfiguredRoleKey(
                    player.Profile.Info.Settings.Role);
            }
            catch { return "ROLE-assault"; }
        }

        private static string GetConfiguredRoleKey(
            WildSpawnType role)
        {
            if (role == WildSpawnType.pmcBEAR)
                return "PMC-BEAR";
            if (role == WildSpawnType.pmcUSEC)
                return "PMC-USEC";
            return "ROLE-" + role;
        }

        private static bool IsRuntimeBoss(BotOwner owner)
        {
            return owner != null &&
                   owner.Boss != null &&
                   owner.Boss.IamBoss;
        }

        private static bool IsRuntimeFollower(BotOwner owner)
        {
            if (owner == null || IsRuntimeBoss(owner))
                return false;
            try
            {
                return owner.IsFollower() ||
                       (owner.BotFollower != null &&
                        owner.BotFollower.HaveBoss);
            }
            catch
            {
                return false;
            }
        }

        private EspRoleSettings GetRoleSettings(string key)
        {
            EspRoleSettings settings;
            return key != null && _espRolesByKey.TryGetValue(key, out settings)
                ? settings
                : null;
        }

        private bool ShouldShow(Target target)
        {
            if (target != null &&
                IsExcludedEspRoleKey(target.RoleKey))
                return false;

            EspRoleSettings settings = GetRoleSettings(target.RoleKey);
            return settings != null
                ? settings.Enabled.Value
                : ShouldShow(target.Kind);
        }

        private Color GetRoleColor(Target target, bool hidden)
        {
            EspRoleSettings settings = GetRoleSettings(target.RoleKey);
            if (settings == null)
                return GetVisualColor(target.Kind, hidden);
            return ParseVisualColor(
                hidden
                    ? settings.HiddenColor.Value
                    : settings.VisibleColor.Value,
                hidden
                    ? settings.DefaultHidden
                    : settings.DefaultVisible);
        }

        private sealed class EspRoleSettings
        {
            public string Key;
            public string Label;
            public string Group;
            public EspKind Kind;
            public bool FollowerSubcategory;
            public ConfigEntry<bool> Enabled;
            public ConfigEntry<string> VisibleColor;
            public ConfigEntry<string> HiddenColor;
            public Color DefaultVisible;
            public Color DefaultHidden;
        }

        private sealed class EspRoleGroup
        {
            public string Name;
            public bool Expanded;
            public readonly List<EspRoleSettings> Roles =
                new List<EspRoleSettings>();
        }
    }
}
