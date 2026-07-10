using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Godot;

public static class RewardRuleCatalog
{
    private const string RulePath = "res://data/reward-rules.json";
    private static RewardRuleConfig? _cachedConfig;

    public static RewardSettlementData Calculate(GameState state, int turn, int damage)
    {
        var config = Load();
        var floor = Math.Max(1, state.Stage.Floor);
        var silver = config.Formula.SilverBase
            + floor * config.Formula.SilverPerFloor
            + Math.Max(1, state.Enemy.MaxHp / Math.Max(1, config.Formula.SilverEnemyHpDivisor));
        var cultivation = config.Formula.CultivationBase
            + floor * config.Formula.CultivationPerFloor
            + Math.Max(1, damage / Math.Max(1, config.Formula.CultivationDamageDivisor));
        var essence = config.Formula.EssenceBase
            + (config.Formula.EssenceBonusEveryFloors > 0 && floor % config.Formula.EssenceBonusEveryFloors == 0
                ? config.Formula.EssenceBonus
                : 0);
        var food = config.Formula.FoodBonusEveryFloors > 0 && floor % config.Formula.FoodBonusEveryFloors == 0
            ? config.Formula.FoodBonus
            : 0;
        var sectReputation = Math.Max(
            config.Formula.SectReputationMin,
            floor / Math.Max(1, config.Formula.SectReputationFloorDivisor));
        var dropName = config.DropByTag.TryGetValue(state.Enemy.Tag, out var mappedDrop)
            ? mappedDrop
            : config.DropByTag.GetValueOrDefault("default", "江湖杂物");
        var rewardMessage = ReplacePlaceholders(config.Messages.RewardTemplate, new Dictionary<string, string>
        {
            ["silver"] = silver.ToString(),
            ["cultivation"] = cultivation.ToString()
        });
        if (food > 0)
        {
            rewardMessage += ReplacePlaceholders(config.Messages.FoodAppendTemplate, new Dictionary<string, string>
            {
                ["food"] = food.ToString()
            });
        }

        return new RewardSettlementData(
            silver,
            cultivation,
            essence,
            food,
            sectReputation,
            dropName,
            rewardMessage,
            ReplacePlaceholders(config.Messages.DropTemplate, new Dictionary<string, string>
            {
                ["dropName"] = dropName,
                ["essence"] = essence.ToString()
            }),
            ReplacePlaceholders(config.Messages.SectNoticeTemplate, new Dictionary<string, string>
            {
                ["sect"] = state.Hero.Sect,
                ["sectReputation"] = sectReputation.ToString(),
                ["floor"] = floor.ToString()
            }),
            ReplacePlaceholders(config.Messages.AdventureNoticeTemplate, new Dictionary<string, string>
            {
                ["floor"] = floor.ToString(),
                ["turn"] = turn.ToString()
            }));
    }

    private static RewardRuleConfig Load()
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
                _cachedConfig = RewardRuleConfig.Fallback;
                return _cachedConfig;
            }

            using var document = JsonDocument.Parse(File.ReadAllText(absolutePath));
            _cachedConfig = RewardRuleConfig.FromJson(document.RootElement);
            return _cachedConfig;
        }
        catch (Exception ex)
        {
            GD.PushWarning($"Reward rules load failed: {ex.Message}");
            _cachedConfig = RewardRuleConfig.Fallback;
            return _cachedConfig;
        }
    }

    private static string ReplacePlaceholders(string template, IReadOnlyDictionary<string, string> values)
    {
        var result = template;
        foreach (var (key, value) in values)
        {
            result = result.Replace("{" + key + "}", value, StringComparison.Ordinal);
        }

        return result;
    }

    private sealed record RewardRuleConfig(
        RewardFormulaConfig Formula,
        Dictionary<string, string> DropByTag,
        RewardMessageConfig Messages)
    {
        public static RewardRuleConfig Fallback => new(
            RewardFormulaConfig.Fallback,
            new Dictionary<string, string>
            {
                ["外功"] = "练骨砂",
                ["身法"] = "轻身残页",
                ["防反"] = "护心铁片",
                ["内功"] = "温玉丹砂",
                ["破招"] = "断招残谱",
                ["default"] = "江湖杂物"
            },
            RewardMessageConfig.Fallback);

        public static RewardRuleConfig FromJson(JsonElement json)
        {
            return new RewardRuleConfig(
                RewardFormulaConfig.FromJson(SaveJson.ReadObject(json, "rewardFormula")),
                ReadDropMap(SaveJson.ReadObject(json, "dropByTag")),
                RewardMessageConfig.FromJson(SaveJson.ReadObject(json, "messages")));
        }

        private static Dictionary<string, string> ReadDropMap(JsonElement json)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            if (json.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in json.EnumerateObject())
                {
                    if (property.Value.ValueKind == JsonValueKind.String)
                    {
                        result[property.Name] = property.Value.GetString() ?? string.Empty;
                    }
                }
            }

            foreach (var (key, value) in Fallback.DropByTag)
            {
                if (string.IsNullOrWhiteSpace(result.GetValueOrDefault(key)))
                {
                    result[key] = value;
                }
            }

            return result;
        }
    }

    private sealed record RewardFormulaConfig(
        int SilverBase,
        int SilverPerFloor,
        int SilverEnemyHpDivisor,
        int CultivationBase,
        int CultivationPerFloor,
        int CultivationDamageDivisor,
        int EssenceBase,
        int EssenceBonusEveryFloors,
        int EssenceBonus,
        int FoodBonusEveryFloors,
        int FoodBonus,
        int SectReputationFloorDivisor,
        int SectReputationMin)
    {
        public static RewardFormulaConfig Fallback => new(48, 3, 180, 18, 1, 160, 1, 5, 1, 4, 1, 10, 1);

        public static RewardFormulaConfig FromJson(JsonElement json)
        {
            return new RewardFormulaConfig(
                SaveJson.ReadInt(json, "silverBase", Fallback.SilverBase),
                SaveJson.ReadInt(json, "silverPerFloor", Fallback.SilverPerFloor),
                SaveJson.ReadInt(json, "silverEnemyHpDivisor", Fallback.SilverEnemyHpDivisor),
                SaveJson.ReadInt(json, "cultivationBase", Fallback.CultivationBase),
                SaveJson.ReadInt(json, "cultivationPerFloor", Fallback.CultivationPerFloor),
                SaveJson.ReadInt(json, "cultivationDamageDivisor", Fallback.CultivationDamageDivisor),
                SaveJson.ReadInt(json, "essenceBase", Fallback.EssenceBase),
                SaveJson.ReadInt(json, "essenceBonusEveryFloors", Fallback.EssenceBonusEveryFloors),
                SaveJson.ReadInt(json, "essenceBonus", Fallback.EssenceBonus),
                SaveJson.ReadInt(json, "foodBonusEveryFloors", Fallback.FoodBonusEveryFloors),
                SaveJson.ReadInt(json, "foodBonus", Fallback.FoodBonus),
                SaveJson.ReadInt(json, "sectReputationFloorDivisor", Fallback.SectReputationFloorDivisor),
                SaveJson.ReadInt(json, "sectReputationMin", Fallback.SectReputationMin));
        }
    }

    private sealed record RewardMessageConfig(
        string RewardTemplate,
        string FoodAppendTemplate,
        string DropTemplate,
        string SectNoticeTemplate,
        string AdventureNoticeTemplate)
    {
        public static RewardMessageConfig Fallback => new(
            "+{silver} 银两 · +{cultivation} 修为",
            " · +{food} 寒玉粮",
            "战利品 · {dropName} x{essence}",
            "{sect}战绩 +{sectReputation} · 传功堂记录第{floor}层胜绩",
            "第{floor}层已清理 · 第{turn}回合结算完成");

        public static RewardMessageConfig FromJson(JsonElement json)
        {
            return new RewardMessageConfig(
                SaveJson.ReadString(json, "rewardTemplate", Fallback.RewardTemplate),
                SaveJson.ReadString(json, "foodAppendTemplate", Fallback.FoodAppendTemplate),
                SaveJson.ReadString(json, "dropTemplate", Fallback.DropTemplate),
                SaveJson.ReadString(json, "sectNoticeTemplate", Fallback.SectNoticeTemplate),
                SaveJson.ReadString(json, "adventureNoticeTemplate", Fallback.AdventureNoticeTemplate));
        }
    }
}

public readonly record struct RewardSettlementData(
    int Silver,
    int Cultivation,
    int Essence,
    int Food,
    int SectReputation,
    string DropName,
    string RewardMessage,
    string DropMessage,
    string SectNotice,
    string AdventureNotice);
