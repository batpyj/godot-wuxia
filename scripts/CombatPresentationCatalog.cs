using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Godot;

public static class CombatPresentationCatalog
{
    private const string RulePath = "res://data/combat-presentation-rules.json";
    private static CombatPresentationConfig? _cachedConfig;

    public static EnemyVisualProfile GetEnemyVisualProfile(GameState state, bool useBoss)
    {
        var config = Load();
        if (useBoss)
        {
            return config.EnemyVisualProfiles.Boss;
        }

        var floor = Math.Max(1, state.Stage.Floor);
        var band = config.EnemyVisualProfiles.FloorBands
            .Where(item => floor >= item.MinFloor)
            .OrderByDescending(item => item.MinFloor)
            .FirstOrDefault() ?? config.EnemyVisualProfiles.FloorBands[0];
        return band.ToProfile(config.EnemyVisualProfiles.GetWeaponTexturePath(floor));
    }

    public static CombatPresentationTiming GetTiming(bool useBoss)
    {
        var config = Load();
        return useBoss ? config.BossTiming : config.Timing;
    }

    public static RewardNoticePresentation GetRewardNoticePresentation()
    {
        return Load().RewardNoticePresentation;
    }

    private static CombatPresentationConfig Load()
    {
        if (_cachedConfig != null)
        {
            return _cachedConfig;
        }

        try
        {
            var absolutePath = ProjectSettings.GlobalizePath(RulePath);
            if (!File.Exists(absolutePath))
            {
                _cachedConfig = CombatPresentationConfig.Fallback;
                return _cachedConfig;
            }

            using var document = JsonDocument.Parse(File.ReadAllText(absolutePath));
            _cachedConfig = CombatPresentationConfig.FromJson(document.RootElement);
            return _cachedConfig;
        }
        catch (Exception ex)
        {
            GD.PushWarning($"Combat presentation rules load failed: {ex.Message}");
            _cachedConfig = CombatPresentationConfig.Fallback;
            return _cachedConfig;
        }
    }

    private sealed record CombatPresentationConfig(
        EnemyVisualProfileConfig EnemyVisualProfiles,
        CombatPresentationTiming Timing,
        CombatPresentationTiming BossTiming,
        RewardNoticePresentation RewardNoticePresentation)
    {
        public static CombatPresentationConfig Fallback
        {
            get
            {
                var timing = CombatPresentationTiming.Fallback;
                return new CombatPresentationConfig(
                    EnemyVisualProfileConfig.Fallback,
                    timing,
                    timing with
                    {
                        DefeatPause = 1.15,
                        RewardSettlement = 0.55,
                        EnemySpawn = 1.25,
                        EnemySpawnNextBeat = 0.45
                    },
                    RewardNoticePresentation.Fallback);
            }
        }

        public static CombatPresentationConfig FromJson(JsonElement json)
        {
            var fallback = Fallback;
            var timing = CombatPresentationTiming.FromJson(
                SaveJson.ReadObject(json, "timelineDurations"),
                fallback.Timing);
            var bossTiming = CombatPresentationTiming.FromJson(
                SaveJson.ReadObject(json, "bossTimelineDurations"),
                timing);

            return new CombatPresentationConfig(
                EnemyVisualProfileConfig.FromJson(SaveJson.ReadObject(json, "enemyVisualProfiles"), fallback.EnemyVisualProfiles),
                timing,
                bossTiming,
                RewardNoticePresentation.FromJson(SaveJson.ReadObject(json, "rewardNoticePresentation"), fallback.RewardNoticePresentation));
        }
    }

    private sealed record EnemyVisualProfileConfig(
        EnemyVisualProfile Boss,
        List<EnemyVisualBand> FloorBands,
        Dictionary<int, string> WeaponCycle)
    {
        public static EnemyVisualProfileConfig Fallback => new(
            new EnemyVisualProfile(
                "boss_default",
                "res://assets/actors/enemies/boss_default_idle.webp",
                "res://assets/weapons/staff/staff_basic.png",
                new Vector2(512, 1490),
                new Vector2(-260, -980),
                2.35f,
                0.25f,
                true),
            new List<EnemyVisualBand>
            {
                new(
                    "enemy_early",
                    1,
                    "res://assets/actors/enemies/enemy_early_idle.webp",
                    new Vector2(469, 1475),
                    new Vector2(-230, -950),
                    2.1f,
                    0.22f),
                new(
                    "enemy_mid",
                    21,
                    "res://assets/actors/enemies/enemy_mid_idle.webp",
                    new Vector2(479, 1486),
                    new Vector2(-235, -965),
                    2.16f,
                    0.22f),
                new(
                    "enemy_late",
                    30,
                    "res://assets/actors/enemies/enemy_late_idle.webp",
                    new Vector2(512, 1490),
                    new Vector2(-250, -970),
                    2.22f,
                    0.23f)
            },
            new Dictionary<int, string>
            {
                [0] = "res://assets/weapons/blade/blade_basic.png",
                [1] = "res://assets/weapons/sword/sword_basic.png",
                [2] = "res://assets/weapons/staff/staff_basic.png",
                [3] = "res://assets/weapons/cane/cane_basic.png",
                [4] = "res://assets/weapons/spear/spear_basic.png"
            });

        public string GetWeaponTexturePath(int floor)
        {
            var modulo = Math.Abs(floor) % 5;
            return WeaponCycle.TryGetValue(modulo, out var path)
                ? path
                : Fallback.WeaponCycle[1];
        }

        public static EnemyVisualProfileConfig FromJson(JsonElement json, EnemyVisualProfileConfig fallback)
        {
            if (json.ValueKind != JsonValueKind.Object)
            {
                return fallback;
            }

            var boss = ReadProfile(SaveJson.ReadObject(json, "boss"), fallback.Boss);
            var floorBands = ReadFloorBands(json, fallback.FloorBands);
            var weaponCycle = ReadWeaponCycle(json, fallback.WeaponCycle);
            return new EnemyVisualProfileConfig(boss, floorBands, weaponCycle);
        }

        private static List<EnemyVisualBand> ReadFloorBands(JsonElement json, List<EnemyVisualBand> fallback)
        {
            if (!json.TryGetProperty("floorBands", out var bandsJson) || bandsJson.ValueKind != JsonValueKind.Array)
            {
                return fallback;
            }

            var bands = new List<EnemyVisualBand>();
            foreach (var bandJson in bandsJson.EnumerateArray())
            {
                if (bandJson.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var fallbackBand = fallback.FirstOrDefault(item =>
                    string.Equals(item.Id, SaveJson.ReadString(bandJson, "id", string.Empty), StringComparison.Ordinal))
                    ?? fallback[0];
                bands.Add(new EnemyVisualBand(
                    SaveJson.ReadString(bandJson, "id", fallbackBand.Id),
                    SaveJson.ReadInt(bandJson, "minFloor", fallbackBand.MinFloor),
                    SaveJson.ReadString(bandJson, "bodyTexturePath", fallbackBand.BodyTexturePath),
                    ReadVector2(bandJson, "feetAnchorPixels", fallbackBand.FeetAnchorPixels),
                    ReadVector2(bandJson, "weaponSocketLocalFromFeet", fallbackBand.WeaponSocketLocalFromFeet),
                    ReadFloat(bandJson, "weaponScale", fallbackBand.WeaponScale),
                    ReadFloat(bandJson, "actorScale", fallbackBand.ActorScale)));
            }

            return bands.Count > 0
                ? bands.OrderBy(item => item.MinFloor).ToList()
                : fallback;
        }

        private static Dictionary<int, string> ReadWeaponCycle(JsonElement json, Dictionary<int, string> fallback)
        {
            if (!json.TryGetProperty("weaponCycle", out var cycleJson) || cycleJson.ValueKind != JsonValueKind.Array)
            {
                return fallback;
            }

            var result = new Dictionary<int, string>(fallback);
            foreach (var item in cycleJson.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var modulo = SaveJson.ReadInt(item, "floorModulo", -1);
                var path = SaveJson.ReadString(item, "weaponTexturePath", string.Empty);
                if (modulo >= 0 && !string.IsNullOrWhiteSpace(path))
                {
                    result[modulo] = path;
                }
            }

            return result;
        }

        private static EnemyVisualProfile ReadProfile(JsonElement json, EnemyVisualProfile fallback)
        {
            if (json.ValueKind != JsonValueKind.Object)
            {
                return fallback;
            }

            return new EnemyVisualProfile(
                SaveJson.ReadString(json, "id", fallback.Id),
                SaveJson.ReadString(json, "bodyTexturePath", fallback.BodyTexturePath),
                SaveJson.ReadString(json, "weaponTexturePath", fallback.WeaponTexturePath),
                ReadVector2(json, "feetAnchorPixels", fallback.FeetAnchorPixels),
                ReadVector2(json, "weaponSocketLocalFromFeet", fallback.WeaponSocketLocalFromFeet),
                ReadFloat(json, "weaponScale", fallback.WeaponScale),
                ReadFloat(json, "actorScale", fallback.ActorScale),
                SaveJson.ReadBool(json, "isBoss", fallback.IsBoss));
        }
    }

    private sealed record EnemyVisualBand(
        string Id,
        int MinFloor,
        string BodyTexturePath,
        Vector2 FeetAnchorPixels,
        Vector2 WeaponSocketLocalFromFeet,
        float WeaponScale,
        float ActorScale)
    {
        public EnemyVisualProfile ToProfile(string weaponTexturePath)
        {
            return new EnemyVisualProfile(
                Id,
                BodyTexturePath,
                weaponTexturePath,
                FeetAnchorPixels,
                WeaponSocketLocalFromFeet,
                WeaponScale,
                ActorScale,
                false);
        }
    }

    private static Vector2 ReadVector2(JsonElement json, string propertyName, Vector2 fallback)
    {
        if (json.ValueKind != JsonValueKind.Object
            || !json.TryGetProperty(propertyName, out var property)
            || property.ValueKind != JsonValueKind.Array)
        {
            return fallback;
        }

        var values = property.EnumerateArray().Take(2).ToArray();
        if (values.Length < 2
            || !values[0].TryGetDouble(out var x)
            || !values[1].TryGetDouble(out var y))
        {
            return fallback;
        }

        return new Vector2((float)x, (float)y);
    }

    public static Vector2 ReadVector2ForPresentation(JsonElement json, string propertyName, Vector2 fallback)
    {
        return ReadVector2(json, propertyName, fallback);
    }

    private static float ReadFloat(JsonElement json, string propertyName, float fallback)
    {
        return (float)SaveJson.ReadDouble(json, propertyName, fallback);
    }

    private static Color ReadColor(JsonElement json, Color fallback)
    {
        if (json.ValueKind != JsonValueKind.Array)
        {
            return fallback;
        }

        var values = json.EnumerateArray().Take(4).ToArray();
        if (values.Length < 4
            || !values[0].TryGetDouble(out var r)
            || !values[1].TryGetDouble(out var g)
            || !values[2].TryGetDouble(out var b)
            || !values[3].TryGetDouble(out var a))
        {
            return fallback;
        }

        return new Color((float)r, (float)g, (float)b, (float)a);
    }

    public static Color ReadColorForPresentation(JsonElement json, Color fallback)
    {
        return ReadColor(json, fallback);
    }
}

public readonly record struct EnemyVisualProfile(
    string Id,
    string BodyTexturePath,
    string WeaponTexturePath,
    Vector2 FeetAnchorPixels,
    Vector2 WeaponSocketLocalFromFeet,
    float WeaponScale,
    float ActorScale,
    bool IsBoss);

public readonly record struct CombatPresentationTiming(
    double GrazeRecover,
    double GrazeNextBeat,
    double HitRecover,
    double HitNextBeat,
    double DefeatPause,
    double RewardSettlement,
    double CombatRecover,
    double CombatNextBeat,
    double EnemySpawn,
    double EnemySpawnNextBeat)
{
    public static CombatPresentationTiming Fallback => new(0.25, 0.1, 0.45, 0.15, 0.9, 0.45, 0.25, 0.1, 1.05, 0.35);

    public static CombatPresentationTiming FromJson(JsonElement json, CombatPresentationTiming fallback)
    {
        return new CombatPresentationTiming(
            SaveJson.ReadDouble(json, "grazeRecover", fallback.GrazeRecover),
            SaveJson.ReadDouble(json, "grazeNextBeat", fallback.GrazeNextBeat),
            SaveJson.ReadDouble(json, "hitRecover", fallback.HitRecover),
            SaveJson.ReadDouble(json, "hitNextBeat", fallback.HitNextBeat),
            SaveJson.ReadDouble(json, "defeatPause", fallback.DefeatPause),
            SaveJson.ReadDouble(json, "rewardSettlement", fallback.RewardSettlement),
            SaveJson.ReadDouble(json, "combatRecover", fallback.CombatRecover),
            SaveJson.ReadDouble(json, "combatNextBeat", fallback.CombatNextBeat),
            SaveJson.ReadDouble(json, "enemySpawn", fallback.EnemySpawn),
            SaveJson.ReadDouble(json, "enemySpawnNextBeat", fallback.EnemySpawnNextBeat));
    }
}

public sealed record RewardNoticePresentation(
    int MaxEvents,
    Vector2 BasePosition,
    float RowSpacing,
    Dictionary<string, Color> Colors)
{
    public static RewardNoticePresentation Fallback => new(
        4,
        new Vector2(640, 604),
        28,
        new Dictionary<string, Color>(StringComparer.Ordinal)
        {
            ["reward"] = new Color(1.0f, 0.82f, 0.38f, 1.0f),
            ["drop"] = new Color(0.52f, 0.9f, 1.0f, 1.0f),
            ["sect_notice"] = new Color(0.78f, 1.0f, 0.6f, 1.0f),
            ["adventure_notice"] = new Color(1.0f, 0.94f, 0.72f, 1.0f),
            ["default"] = new Color(1.0f, 0.9f, 0.65f, 1.0f)
        });

    public Color ColorFor(string kind)
    {
        if (Colors.TryGetValue(kind, out var color))
        {
            return color;
        }

        return Colors.TryGetValue("default", out var fallback)
            ? fallback
            : new Color(1.0f, 0.9f, 0.65f, 1.0f);
    }

    public static RewardNoticePresentation FromJson(JsonElement json, RewardNoticePresentation fallback)
    {
        if (json.ValueKind != JsonValueKind.Object)
        {
            return fallback;
        }

        return new RewardNoticePresentation(
            SaveJson.ReadInt(json, "maxEvents", fallback.MaxEvents),
            CombatPresentationCatalog.ReadVector2ForPresentation(json, "basePosition", fallback.BasePosition),
            (float)SaveJson.ReadDouble(json, "rowSpacing", fallback.RowSpacing),
            ReadColors(SaveJson.ReadObject(json, "colors"), fallback.Colors));
    }

    private static Dictionary<string, Color> ReadColors(JsonElement json, Dictionary<string, Color> fallback)
    {
        var result = new Dictionary<string, Color>(fallback, StringComparer.Ordinal);
        if (json.ValueKind != JsonValueKind.Object)
        {
            return result;
        }

        foreach (var property in json.EnumerateObject())
        {
            result[property.Name] = CombatPresentationCatalog.ReadColorForPresentation(
                property.Value,
                result.TryGetValue(property.Name, out var existing) ? existing : result["default"]);
        }

        return result;
    }
}
