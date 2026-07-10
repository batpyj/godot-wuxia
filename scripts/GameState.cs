using System;
using System.Linq;
using System.Text.Json;

public sealed record GameState(
    string SaveSchemaVersion,
    string SourceVersion,
    StageSnapshot Stage,
    HeroSnapshot Hero,
    EnemySnapshot Enemy,
    string[] SectBonuses,
    MartialArtSnapshot[] MartialArts,
    RomanceSnapshot Romance,
    WeaponSnapshot WeaponInfo,
    CommandSnapshot Commands,
    CombatSnapshot Combat)
{
    public const string CurrentSaveSchemaVersion = "v15g.5";
    public const string LegacySaveSchemaVersion = "v15g.0";
    public const string TransitionalSaveSchemaVersion = "v15g.1";
    public const string PreviousSaveSchemaVersion = "v15g.2";
    public const string CombatSaveSchemaVersion = "v15g.3";
    public const string AutoCombatSaveSchemaVersion = "v15g.4";

    public static GameState FromCombatSampleJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var rightPanel = ReadObject(root, "rightPanel");
        var sectPanel = ReadObject(rightPanel, "sect");
        var weaponPanel = ReadObject(rightPanel, "weapon");
        var martialArts = ReadMartialArts(rightPanel);

        return new GameState(
            CurrentSaveSchemaVersion,
            ReadString(root, "version", "unknown"),
            StageSnapshot.FromJson(ReadObject(root, "stage")),
            HeroSnapshot.FromJson(ReadObject(root, "hero")),
            EnemySnapshot.FromJson(ReadObject(root, "enemy")),
            SaveJson.ReadStringArray(sectPanel, "bonuses", DefaultSectBonuses()),
            martialArts,
            RomanceSnapshot.FromJson(ReadObject(root, "romance")),
            WeaponSnapshot.FromJson(weaponPanel),
            CommandSnapshot.FromJson(ReadObject(root, "commands"), ReadObject(rightPanel, "adventure"), martialArts.Length),
            CombatSnapshot.FromJson(ReadObject(root, "combat")));
    }

    public string ToStableJson()
    {
        var payload = new
        {
            saveSchemaVersion = SaveSchemaVersion,
            sourceVersion = SourceVersion,
            stage = new
            {
                floor = Stage.Floor,
                chapter = Stage.Chapter,
                martialRealm = Stage.MartialRealm,
                year = Stage.Year,
                direction = Stage.Direction,
                combatPower = Stage.CombatPower
            },
            hero = new
            {
                name = Hero.Name,
                title = Hero.Title,
                sect = Hero.Sect,
                realm = Hero.Realm,
                weapon = Hero.Weapon,
                hp = Hero.Hp,
                maxHp = Hero.MaxHp,
                qi = Hero.Qi,
                comprehensivePower = Hero.ComprehensivePower,
                cultivation = Hero.Cultivation,
                cultivationPerSecond = Hero.CultivationPerSecond,
                effects = Hero.Effects,
                battleStats = new
                {
                    outerPower = Hero.BattleStats.OuterPower,
                    innerPower = Hero.BattleStats.InnerPower,
                    outerDefense = Hero.BattleStats.OuterDefense,
                    innerDefense = Hero.BattleStats.InnerDefense,
                    critRate = Hero.BattleStats.CritRate,
                    dodgeRate = Hero.BattleStats.DodgeRate
                },
                resources = new
                {
                    silver = Hero.Resources.Silver,
                    essence = Hero.Resources.Essence,
                    food = Hero.Resources.Food
                },
                jianghuQualities = new
                {
                    reputation = Hero.JianghuQualities.Reputation,
                    comprehension = Hero.JianghuQualities.Comprehension,
                    luck = Hero.JianghuQualities.Luck,
                    charm = Hero.JianghuQualities.Charm,
                    root = Hero.JianghuQualities.Root,
                    resolve = Hero.JianghuQualities.Resolve
                }
            },
            enemy = new
            {
                name = Enemy.Name,
                rank = Enemy.Rank,
                tag = Enemy.Tag,
                hp = Enemy.Hp,
                maxHp = Enemy.MaxHp,
                combatPower = Enemy.CombatPower
            },
            sectBonuses = SectBonuses,
            martialArts = MartialArts.Select(art => new
            {
                name = art.Name,
                type = art.Type,
                level = art.Level,
                effect = art.Effect
            }).ToArray(),
            romance = new
            {
                name = Romance.Name,
                favor = Romance.Favor,
                status = Romance.Status,
                badge = Romance.Badge
            },
            weaponInfo = new
            {
                title = WeaponInfo.Title,
                name = WeaponInfo.Name,
                type = WeaponInfo.Type,
                score = WeaponInfo.Score,
                socket = WeaponInfo.Socket,
                effects = WeaponInfo.Effects
            },
            commands = new
            {
                isPaused = Commands.IsPaused,
                autoExplore = Commands.AutoExplore,
                activeMartialArtIndex = Commands.ActiveMartialArtIndex,
                lastCommand = Commands.LastCommand
            },
            combat = new
            {
                turn = Combat.Turn,
                lastAction = Combat.LastAction,
                lastTarget = Combat.LastTarget,
                lastDamage = Combat.LastDamage,
                lastDamageRatio = Combat.LastDamageRatio,
                lastHitKind = Combat.LastHitKind,
                enemyDefeated = Combat.EnemyDefeated,
                message = Combat.Message,
                eventLog = Combat.EventLog,
                events = Combat.Events.Select(item => new
                {
                    turn = item.Turn,
                    kind = item.Kind,
                    actor = item.Actor,
                    target = item.Target,
                    amount = item.Amount,
                    message = item.Message
                }).ToArray(),
                migrationWarnings = Combat.MigrationWarnings
            }
        };

        return JsonSerializer.Serialize(payload, SaveJson.Options);
    }

    public static GameState FromStableJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var savedSchema = ReadString(root, "saveSchemaVersion", string.Empty);
        var schema = savedSchema is LegacySaveSchemaVersion or TransitionalSaveSchemaVersion or PreviousSaveSchemaVersion or CombatSaveSchemaVersion or AutoCombatSaveSchemaVersion
            ? CurrentSaveSchemaVersion
            : savedSchema;
        var martialArts = ReadMartialArts(root);

        return new GameState(
            schema,
            ReadString(root, "sourceVersion", string.Empty),
            StageSnapshot.FromJson(ReadObject(root, "stage")),
            HeroSnapshot.FromJson(ReadObject(root, "hero")),
            EnemySnapshot.FromJson(ReadObject(root, "enemy")),
            SaveJson.ReadStringArray(root, "sectBonuses", DefaultSectBonuses()),
            martialArts,
            RomanceSnapshot.FromJson(ReadObject(root, "romance")),
            WeaponSnapshot.FromJson(ReadObject(root, "weaponInfo")),
            CommandSnapshot.FromJson(ReadObject(root, "commands"), default, martialArts.Length),
            CombatSnapshot.FromJson(ReadObject(root, "combat")));
    }

    public bool IsStructurallyValid(out string error)
    {
        if (SaveSchemaVersion != CurrentSaveSchemaVersion)
        {
            error = $"存档版本不匹配：{SaveSchemaVersion}";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Hero.Name) || Hero.MaxHp <= 0 || Hero.Hp < 0)
        {
            error = "主角数据不完整";
            return false;
        }

        if (Stage.Floor <= 0 || string.IsNullOrWhiteSpace(Stage.Chapter))
        {
            error = "江湖进度数据不完整";
            return false;
        }

        if (MartialArts.Length <= 0)
        {
            error = "功法数据不完整";
            return false;
        }

        if (Commands.ActiveMartialArtIndex < 0 || Commands.ActiveMartialArtIndex >= MartialArts.Length)
        {
            error = "当前功法索引不完整";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static MartialArtSnapshot[] ReadMartialArts(JsonElement json)
    {
        if (json.ValueKind != JsonValueKind.Object
            || !json.TryGetProperty("martialArts", out var property)
            || property.ValueKind != JsonValueKind.Array)
        {
            return DefaultMartialArts();
        }

        var values = property
            .EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.Object)
            .Select(MartialArtSnapshot.FromJson)
            .ToArray();

        return values.Length > 0 ? values : DefaultMartialArts();
    }

    private static string[] DefaultSectBonuses()
    {
        return new[] { "气血回复 +30%", "外功 +12%", "内功 +12%" };
    }

    private static MartialArtSnapshot[] DefaultMartialArts()
    {
        return new[]
        {
            new MartialArtSnapshot("无忌心经", "内功常驻", "突破 8/10 重", "内功 +108 / 防御 +35 / 气血 +24")
        };
    }

    private static JsonElement ReadObject(JsonElement json, string propertyName)
    {
        return json.ValueKind == JsonValueKind.Object
            && json.TryGetProperty(propertyName, out var property)
            && property.ValueKind == JsonValueKind.Object
            ? property
            : default;
    }

    private static string ReadString(JsonElement json, string propertyName, string fallback)
    {
        return json.ValueKind == JsonValueKind.Object
            && json.TryGetProperty(propertyName, out var property)
            && property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? fallback
            : fallback;
    }
}

public sealed record StageSnapshot(int Floor, string Chapter, string MartialRealm, int Year, string Direction, string CombatPower)
{
    public static StageSnapshot FromJson(JsonElement json)
    {
        return new StageSnapshot(
            SaveJson.ReadInt(json, "floor", 1),
            SaveJson.ReadString(json, "chapter", "江湖风云"),
            SaveJson.ReadString(json, "martialRealm", "初入江湖"),
            SaveJson.ReadInt(json, "year", 0),
            SaveJson.ReadString(json, "direction", "养伤潜修 / 练功闭门"),
            SaveJson.ReadString(json, "combatPower", "0"));
    }
}

public sealed record HeroSnapshot(
    string Name,
    string Title,
    string Sect,
    string Realm,
    string Weapon,
    int Hp,
    int MaxHp,
    int Qi,
    int ComprehensivePower,
    int Cultivation,
    double CultivationPerSecond,
    string[] Effects,
    BattleStatsSnapshot BattleStats,
    ResourceSnapshot Resources,
    QualitySnapshot JianghuQualities)
{
    public static HeroSnapshot FromJson(JsonElement json)
    {
        return new HeroSnapshot(
            SaveJson.ReadString(json, "name", "江湖风云"),
            SaveJson.ReadString(json, "title", "无名侠客"),
            SaveJson.ReadString(json, "sect", "无门无派"),
            SaveJson.ReadString(json, "realm", "初入江湖"),
            SaveJson.ReadString(json, "weapon", "sword"),
            SaveJson.ReadInt(json, "hp", 1),
            SaveJson.ReadInt(json, "maxHp", 1),
            SaveJson.ReadInt(json, "qi", 0),
            SaveJson.ReadInt(json, "comprehensivePower", 0),
            SaveJson.ReadInt(json, "cultivation", 0),
            SaveJson.ReadDouble(json, "cultivationPerSecond", 0),
            SaveJson.ReadStringArray(json, "effects", new[] { "内功增伤 +13%", "破防 +1%", "伤害减免 -5%" }),
            BattleStatsSnapshot.FromJson(SaveJson.ReadObject(json, "battleStats")),
            ResourceSnapshot.FromJson(SaveJson.ReadObject(json, "resources")),
            QualitySnapshot.FromJson(SaveJson.ReadObject(json, "jianghuQualities")));
    }
}

public sealed record BattleStatsSnapshot(int OuterPower, int InnerPower, int OuterDefense, int InnerDefense, int CritRate, int DodgeRate)
{
    public static BattleStatsSnapshot FromJson(JsonElement json)
    {
        return new BattleStatsSnapshot(
            SaveJson.ReadInt(json, "outerPower", 1410),
            SaveJson.ReadInt(json, "innerPower", 1217),
            SaveJson.ReadInt(json, "outerDefense", 832),
            SaveJson.ReadInt(json, "innerDefense", 1010),
            SaveJson.ReadInt(json, "critRate", 82),
            SaveJson.ReadInt(json, "dodgeRate", 46));
    }
}

public sealed record ResourceSnapshot(int Silver, int Essence, int Food)
{
    public static ResourceSnapshot FromJson(JsonElement json)
    {
        return new ResourceSnapshot(
            SaveJson.ReadInt(json, "silver", 3589),
            SaveJson.ReadInt(json, "essence", 62),
            SaveJson.ReadInt(json, "food", 18));
    }
}

public sealed record QualitySnapshot(int Reputation, int Comprehension, int Luck, int Charm, int Root, int Resolve)
{
    public static QualitySnapshot FromJson(JsonElement json)
    {
        return new QualitySnapshot(
            SaveJson.ReadInt(json, "reputation", 47),
            SaveJson.ReadInt(json, "comprehension", 55),
            SaveJson.ReadInt(json, "luck", 16),
            SaveJson.ReadInt(json, "charm", 24),
            SaveJson.ReadInt(json, "root", 51),
            SaveJson.ReadInt(json, "resolve", 19));
    }
}

public sealed record EnemySnapshot(string Name, string Rank, string Tag, int Hp, int MaxHp, string CombatPower)
{
    public static EnemySnapshot FromJson(JsonElement json)
    {
        return new EnemySnapshot(
            SaveJson.ReadString(json, "name", "对手"),
            SaveJson.ReadString(json, "rank", "第1层"),
            SaveJson.ReadString(json, "tag", "普通"),
            SaveJson.ReadInt(json, "hp", 1),
            SaveJson.ReadInt(json, "maxHp", 1),
            SaveJson.ReadString(json, "combatPower", "0"));
    }
}

public sealed record MartialArtSnapshot(string Name, string Type, string Level, string Effect)
{
    public static MartialArtSnapshot FromJson(JsonElement json)
    {
        return new MartialArtSnapshot(
            SaveJson.ReadString(json, "name", "无忌心经"),
            SaveJson.ReadString(json, "type", "内功常驻"),
            SaveJson.ReadString(json, "level", "突破 8/10 重"),
            SaveJson.ReadString(json, "effect", "内功 +108 / 防御 +35 / 气血 +24"));
    }
}

public sealed record RomanceSnapshot(string Name, int Favor, string Status, string Badge)
{
    public static RomanceSnapshot FromJson(JsonElement json)
    {
        return new RomanceSnapshot(
            SaveJson.ReadString(json, "name", "沈青萝"),
            SaveJson.ReadInt(json, "favor", 15),
            SaveJson.ReadString(json, "status", "援手待部署"),
            SaveJson.ReadString(json, "badge", "待"));
    }
}

public sealed record WeaponSnapshot(string Title, string Name, string Type, int Score, string Socket, string[] Effects)
{
    public static WeaponSnapshot FromJson(JsonElement json)
    {
        return new WeaponSnapshot(
            SaveJson.ReadString(json, "title", "武器"),
            SaveJson.ReadString(json, "name", "王府纹剑"),
            SaveJson.ReadString(json, "type", "剑"),
            SaveJson.ReadInt(json, "score", 197),
            SaveJson.ReadString(json, "socket", "独立挂点 · 手部跟随"),
            SaveJson.ReadStringArray(json, "effects", new[] { "剑气拖尾", "攻击评分 +197" }));
    }
}

public sealed record CombatSnapshot(
    int Turn,
    string LastAction,
    string LastTarget,
    int LastDamage,
    double LastDamageRatio,
    string LastHitKind,
    bool EnemyDefeated,
    string Message,
    string[] EventLog,
    CombatEventSnapshot[] Events,
    string[] MigrationWarnings)
{
    public static CombatSnapshot FromJson(JsonElement json)
    {
        var eventLog = SaveJson.ReadStringArray(json, "eventLog", Array.Empty<string>());
        var events = ReadEvents(json, eventLog);

        return new CombatSnapshot(
            SaveJson.ReadInt(json, "turn", 0),
            SaveJson.ReadString(json, "lastAction", "idle"),
            SaveJson.ReadString(json, "lastTarget", "enemy"),
            SaveJson.ReadInt(json, "lastDamage", 0),
            SaveJson.ReadDouble(json, "lastDamageRatio", 0),
            SaveJson.ReadString(json, "lastHitKind", "none"),
            SaveJson.ReadBool(json, "enemyDefeated", false),
            SaveJson.ReadString(json, "message", "Combat ready"),
            eventLog,
            events,
            SaveJson.ReadStringArray(json, "migrationWarnings", Array.Empty<string>()));
    }

    private static CombatEventSnapshot[] ReadEvents(JsonElement json, string[] eventLog)
    {
        if (json.ValueKind == JsonValueKind.Object
            && json.TryGetProperty("events", out var property)
            && property.ValueKind == JsonValueKind.Array)
        {
            var values = property
                .EnumerateArray()
                .Where(item => item.ValueKind == JsonValueKind.Object)
                .Select(CombatEventSnapshot.FromJson)
                .ToArray();

            if (values.Length > 0)
            {
                return values;
            }
        }

        return eventLog
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select((message, index) => new CombatEventSnapshot(0, "log", "system", "combat", 0, message))
            .ToArray();
    }
}

public sealed record CombatEventSnapshot(int Turn, string Kind, string Actor, string Target, int Amount, string Message)
{
    public static CombatEventSnapshot FromJson(JsonElement json)
    {
        return new CombatEventSnapshot(
            SaveJson.ReadInt(json, "turn", 0),
            SaveJson.ReadString(json, "kind", "log"),
            SaveJson.ReadString(json, "actor", "system"),
            SaveJson.ReadString(json, "target", "combat"),
            SaveJson.ReadInt(json, "amount", 0),
            SaveJson.ReadString(json, "message", string.Empty));
    }
}

public sealed record CommandSnapshot(bool IsPaused, bool AutoExplore, int ActiveMartialArtIndex, string LastCommand)
{
    public static CommandSnapshot FromJson(JsonElement json, JsonElement adventure, int martialArtCount)
    {
        var fallbackPaused = SaveJson.ReadString(adventure, "status", "暂停闯荡").Contains("暂停", StringComparison.Ordinal);
        var activeIndex = SaveJson.ReadInt(json, "activeMartialArtIndex", 0);
        return new CommandSnapshot(
            SaveJson.ReadBool(json, "isPaused", fallbackPaused),
            SaveJson.ReadBool(json, "autoExplore", false),
            Math.Clamp(activeIndex, 0, Math.Max(0, martialArtCount - 1)),
            SaveJson.ReadString(json, "lastCommand", "初始状态"));
    }
}

public static class SaveJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = false
    };

    public static JsonElement ReadObject(JsonElement json, string propertyName)
    {
        return json.ValueKind == JsonValueKind.Object
            && json.TryGetProperty(propertyName, out var property)
            && property.ValueKind == JsonValueKind.Object
            ? property
            : default;
    }

    public static string ReadString(JsonElement json, string propertyName, string fallback)
    {
        return json.ValueKind == JsonValueKind.Object
            && json.TryGetProperty(propertyName, out var property)
            && property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? fallback
            : fallback;
    }

    public static int ReadInt(JsonElement json, string propertyName, int fallback)
    {
        return json.ValueKind == JsonValueKind.Object
            && json.TryGetProperty(propertyName, out var property)
            && property.TryGetInt32(out var value)
            ? value
            : fallback;
    }

    public static double ReadDouble(JsonElement json, string propertyName, double fallback)
    {
        return json.ValueKind == JsonValueKind.Object
            && json.TryGetProperty(propertyName, out var property)
            && property.TryGetDouble(out var value)
            ? value
            : fallback;
    }

    public static bool ReadBool(JsonElement json, string propertyName, bool fallback)
    {
        return json.ValueKind == JsonValueKind.Object
            && json.TryGetProperty(propertyName, out var property)
            && property.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? property.GetBoolean()
            : fallback;
    }

    public static string[] ReadStringArray(JsonElement json, string propertyName, string[] fallback)
    {
        if (json.ValueKind != JsonValueKind.Object
            || !json.TryGetProperty(propertyName, out var property)
            || property.ValueKind != JsonValueKind.Array)
        {
            return fallback;
        }

        var values = property
            .EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString() ?? string.Empty)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray();

        return values.Length > 0 ? values : fallback;
    }
}
