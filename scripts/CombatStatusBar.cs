using System;
using System.IO;
using System.Text.Json;
using Godot;

public partial class CombatStatusBar : Control
{
    private const string DefaultSamplePath = "res://data/combat-sample.json";
    private CombatSample _sample = CombatSample.Fallback;

    public override void _Ready()
    {
        Name = "CombatStatusBar";
        MouseFilter = MouseFilterEnum.Ignore;
        ZIndex = 160;

        SetAnchorsPreset(LayoutPreset.TopWide);
        OffsetLeft = 32;
        OffsetTop = 16;
        OffsetRight = -32;
        OffsetBottom = 124;

        _sample = LoadSample(DefaultSamplePath);
        BuildUi();
        SaveManager.StateChanged += OnGameStateChanged;
    }

    public override void _ExitTree()
    {
        SaveManager.StateChanged -= OnGameStateChanged;
    }

    private void OnGameStateChanged(GameState state)
    {
        _sample = LoadSample(DefaultSamplePath);
        BuildUi();
    }

    private void BuildUi()
    {
        foreach (var child in GetChildren())
        {
            child.QueueFree();
        }

        var panel = new PanelContainer
        {
            Name = "StatusPanel",
            MouseFilter = MouseFilterEnum.Ignore
        };
        panel.SetAnchorsPreset(LayoutPreset.FullRect);
        panel.AddThemeStyleboxOverride("panel", MakePanelStyle(new Color(0.09f, 0.07f, 0.045f, 0.86f), new Color(0.86f, 0.66f, 0.28f, 0.7f), 2));
        AddChild(panel);

        var margin = new MarginContainer
        {
            Name = "ContentMargin",
            MouseFilter = MouseFilterEnum.Ignore
        };
        margin.AddThemeConstantOverride("margin_left", 14);
        margin.AddThemeConstantOverride("margin_right", 14);
        margin.AddThemeConstantOverride("margin_top", 10);
        margin.AddThemeConstantOverride("margin_bottom", 10);
        panel.AddChild(margin);

        var row = new HBoxContainer
        {
            Name = "StatusRow",
            MouseFilter = MouseFilterEnum.Ignore
        };
        row.AddThemeConstantOverride("separation", 12);
        margin.AddChild(row);

        row.AddChild(BuildStageBlock());
        row.AddChild(BuildRealmBlock());
        row.AddChild(BuildHeroBlock());
        row.AddChild(BuildPowerBlock());
        row.AddChild(BuildEnemyBlock());
        row.AddChild(BuildPreviewBlock());
    }

    private Control BuildStageBlock()
    {
        var block = MakeBlock("StageBlock", 190);
        block.AddChild(MakeSmallLabel(_sample.Stage.Chapter));
        block.AddChild(MakeTitleLabel($"第 {_sample.Stage.Floor} 层"));
        block.AddChild(MakeSmallLabel($"{_sample.Stage.Year} 年"));
        return block;
    }

    private Control BuildRealmBlock()
    {
        var block = MakeBlock("RealmBlock", 260);
        block.AddChild(MakeSmallLabel("武学境界"));
        block.AddChild(MakeTitleLabel(_sample.Stage.MartialRealm));
        block.AddChild(MakeSmallLabel(_sample.Stage.Direction));
        return block;
    }

    private Control BuildHeroBlock()
    {
        var block = MakeBlock("HeroBlock", 370);
        block.AddChild(MakeInlineHeader(_sample.Hero.Name, $"{_sample.Hero.Sect} / {_sample.Hero.Realm}"));
        block.AddChild(MakeProgressLine("我方气血", _sample.Hero.Hp, _sample.Hero.MaxHp, new Color(0.38f, 0.86f, 0.48f, 0.95f)));
        block.AddChild(MakeSmallLabel($"气血稳健 {_sample.Hero.Qi}%"));
        block.AddChild(MakeChipRow(_sample.Hero.Effects));
        return block;
    }

    private Control BuildPowerBlock()
    {
        var block = MakeBlock("PowerBlock", 150);
        block.Alignment = BoxContainer.AlignmentMode.Center;
        block.AddChild(MakeSmallLabel("综合战力"));
        var power = MakeTitleLabel(_sample.Stage.CombatPower);
        power.HorizontalAlignment = HorizontalAlignment.Center;
        block.AddChild(power);
        return block;
    }

    private Control BuildEnemyBlock()
    {
        var block = MakeBlock("EnemyBlock", 360);
        block.AddChild(MakeInlineHeader(_sample.Enemy.Name, $"{_sample.Enemy.Rank} / {_sample.Enemy.Tag}"));
        block.AddChild(MakeProgressLine("敌方气血", _sample.Enemy.Hp, _sample.Enemy.MaxHp, new Color(0.94f, 0.32f, 0.22f, 0.94f)));
        block.AddChild(MakeSmallLabel($"对方战力 {_sample.Enemy.CombatPower}"));
        block.AddChild(MakeChipRow(_sample.Enemy.Effects));
        return block;
    }

    private Control BuildPreviewBlock()
    {
        var target = CleanPreviewValue(OS.GetEnvironment("WUXIA_GODOT_PREVIEW_TARGET"), "hero");
        var action = CleanPreviewValue(OS.GetEnvironment("WUXIA_GODOT_PREVIEW_ACTION"), "idle");
        var fx = CleanPreviewValue(OS.GetEnvironment("WUXIA_GODOT_PREVIEW_FX"), "auto");

        var block = MakeBlock("PreviewBlock", 220);
        block.AddChild(MakeSmallLabel("预览状态"));
        block.AddChild(MakeTitleLabel($"{target}:{action}"));
        block.AddChild(MakeSmallLabel($"FX {fx}"));
        return block;
    }

    private VBoxContainer MakeBlock(string name, float minWidth)
    {
        var block = new VBoxContainer
        {
            Name = name,
            MouseFilter = MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(minWidth, 0),
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        block.AddThemeConstantOverride("separation", 4);
        return block;
    }

    private Control MakeInlineHeader(string title, string subtitle)
    {
        var row = new HBoxContainer
        {
            Name = "Header",
            MouseFilter = MouseFilterEnum.Ignore
        };
        row.AddThemeConstantOverride("separation", 8);

        var titleLabel = MakeTitleLabel(title);
        titleLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(titleLabel);

        var subtitleLabel = MakeSmallLabel(subtitle);
        subtitleLabel.HorizontalAlignment = HorizontalAlignment.Right;
        row.AddChild(subtitleLabel);
        return row;
    }

    private Control MakeProgressLine(string label, int value, int maxValue, Color fillColor)
    {
        var root = new VBoxContainer
        {
            Name = $"{label}Line",
            MouseFilter = MouseFilterEnum.Ignore
        };
        root.AddThemeConstantOverride("separation", 2);

        var header = new HBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore
        };
        var left = MakeSmallLabel(label);
        left.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        header.AddChild(left);
        var right = MakeSmallLabel($"{FormatNumber(value)} / {FormatNumber(maxValue)}");
        right.HorizontalAlignment = HorizontalAlignment.Right;
        header.AddChild(right);
        root.AddChild(header);

        var bar = new ProgressBar
        {
            MouseFilter = MouseFilterEnum.Ignore,
            MinValue = 0,
            MaxValue = Math.Max(1, maxValue),
            Value = Math.Clamp(value, 0, Math.Max(1, maxValue)),
            ShowPercentage = false,
            CustomMinimumSize = new Vector2(0, 12)
        };
        bar.AddThemeStyleboxOverride("background", MakePanelStyle(new Color(0.05f, 0.045f, 0.035f, 0.82f), new Color(0.2f, 0.16f, 0.1f, 0.8f), 1));
        bar.AddThemeStyleboxOverride("fill", MakePanelStyle(fillColor, fillColor, 0));
        root.AddChild(bar);

        return root;
    }

    private Control MakeChipRow(string[] chips)
    {
        var row = new HBoxContainer
        {
            Name = "Chips",
            MouseFilter = MouseFilterEnum.Ignore
        };
        row.AddThemeConstantOverride("separation", 5);

        foreach (var chip in chips)
        {
            var label = MakeSmallLabel(chip);
            label.CustomMinimumSize = new Vector2(86, 22);
            label.HorizontalAlignment = HorizontalAlignment.Center;
            label.VerticalAlignment = VerticalAlignment.Center;
            label.AddThemeStyleboxOverride("normal", MakePanelStyle(new Color(0.18f, 0.12f, 0.07f, 0.68f), new Color(0.76f, 0.55f, 0.24f, 0.45f), 1));
            row.AddChild(label);
        }

        return row;
    }

    private static Label MakeTitleLabel(string text)
    {
        var label = new Label
        {
            Text = text,
            MouseFilter = MouseFilterEnum.Ignore,
            VerticalAlignment = VerticalAlignment.Center,
            ClipText = true
        };
        label.AddThemeFontSizeOverride("font_size", 24);
        label.AddThemeColorOverride("font_color", new Color(1.0f, 0.88f, 0.48f, 1.0f));
        label.AddThemeColorOverride("font_shadow_color", new Color(0.12f, 0.07f, 0.03f, 0.8f));
        label.AddThemeConstantOverride("shadow_offset_x", 1);
        label.AddThemeConstantOverride("shadow_offset_y", 2);
        return label;
    }

    private static Label MakeSmallLabel(string text)
    {
        var label = new Label
        {
            Text = text,
            MouseFilter = MouseFilterEnum.Ignore,
            VerticalAlignment = VerticalAlignment.Center,
            ClipText = true
        };
        label.AddThemeFontSizeOverride("font_size", 14);
        label.AddThemeColorOverride("font_color", new Color(0.96f, 0.9f, 0.76f, 1.0f));
        return label;
    }

    private static StyleBoxFlat MakePanelStyle(Color background, Color border, int borderWidth)
    {
        var style = new StyleBoxFlat
        {
            BgColor = background,
            BorderColor = border,
            BorderWidthLeft = borderWidth,
            BorderWidthRight = borderWidth,
            BorderWidthTop = borderWidth,
            BorderWidthBottom = borderWidth,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8
        };
        return style;
    }

    private static CombatSample LoadSample(string resourcePath)
    {
        try
        {
            var state = SaveManager.CurrentState;
            var sample = LoadStaticSample(resourcePath);
            return new CombatSample(
                new StageInfo(state.Stage.Floor, state.Stage.Chapter, state.Stage.MartialRealm, state.Stage.Year, state.Stage.Direction, state.Stage.CombatPower),
                new HeroInfo(
                    state.Hero.Name,
                    state.Hero.Sect,
                    state.Hero.Realm,
                    state.Hero.Hp,
                    state.Hero.MaxHp,
                    state.Hero.Qi,
                    sample.Hero.Effects),
                new EnemyInfo(
                    state.Enemy.Name,
                    state.Enemy.Rank,
                    state.Enemy.Tag,
                    state.Enemy.Hp,
                    state.Enemy.MaxHp,
                    state.Enemy.CombatPower,
                    sample.Enemy.Effects));
        }
        catch (Exception ex)
        {
            GD.PushWarning($"CombatStatusBar sample load failed: {ex.Message}");
            return CombatSample.Fallback;
        }
    }

    private static CombatSample LoadStaticSample(string resourcePath)
    {
        try
        {
            var absolutePath = ProjectSettings.GlobalizePath(resourcePath);
            if (!File.Exists(absolutePath))
            {
                return CombatSample.Fallback;
            }

            using var document = JsonDocument.Parse(File.ReadAllText(absolutePath));
            var root = document.RootElement;
            return new CombatSample(
                StageInfo.FromJson(root.GetProperty("stage")),
                HeroInfo.FromJson(root.GetProperty("hero")),
                EnemyInfo.FromJson(root.GetProperty("enemy")));
        }
        catch
        {
            return CombatSample.Fallback;
        }
    }

    private static string CleanPreviewValue(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    private static string FormatNumber(int value)
    {
        return value >= 10000 && value % 10000 == 0 ? $"{value / 10000}万" : value.ToString();
    }

    private readonly record struct CombatSample(StageInfo Stage, HeroInfo Hero, EnemyInfo Enemy)
    {
        public static CombatSample Fallback => new(
            new StageInfo(20, "江湖风云", "一代宗师", 8, "养伤潜修 / 练功闭门", "1.7万"),
            new HeroInfo("江湖风云", "少林", "一代宗师", 16000, 17000, 73, new[] { "内功增伤 +13%", "破防 +1%", "伤害减免 -5%" }),
            new EnemyInfo("黑衣剑客", "第19层", "身法", 5312, 5312, "2387", new[] { "破手", "身法斗" }));
    }

    private readonly record struct StageInfo(int Floor, string Chapter, string MartialRealm, int Year, string Direction, string CombatPower)
    {
        public static StageInfo FromJson(JsonElement json)
        {
            return new StageInfo(
                ReadInt(json, "floor", 20),
                ReadString(json, "chapter", "江湖风云"),
                ReadString(json, "martialRealm", "一代宗师"),
                ReadInt(json, "year", 8),
                ReadString(json, "direction", "养伤潜修 / 练功闭门"),
                ReadString(json, "combatPower", "1.7万"));
        }
    }

    private readonly record struct HeroInfo(string Name, string Sect, string Realm, int Hp, int MaxHp, int Qi, string[] Effects)
    {
        public static HeroInfo FromJson(JsonElement json)
        {
            return new HeroInfo(
                ReadString(json, "name", "江湖风云"),
                ReadString(json, "sect", "少林"),
                ReadString(json, "realm", "一代宗师"),
                ReadInt(json, "hp", 16000),
                ReadInt(json, "maxHp", 17000),
                ReadInt(json, "qi", 73),
                ReadStringArray(json, "effects", new[] { "内功增伤 +13%", "破防 +1%", "伤害减免 -5%" }));
        }
    }

    private readonly record struct EnemyInfo(string Name, string Rank, string Tag, int Hp, int MaxHp, string CombatPower, string[] Effects)
    {
        public static EnemyInfo FromJson(JsonElement json)
        {
            return new EnemyInfo(
                ReadString(json, "name", "黑衣剑客"),
                ReadString(json, "rank", "第19层"),
                ReadString(json, "tag", "身法"),
                ReadInt(json, "hp", 5312),
                ReadInt(json, "maxHp", 5312),
                ReadString(json, "combatPower", "2387"),
                ReadStringArray(json, "effects", new[] { "破手", "身法斗" }));
        }
    }

    private static string ReadString(JsonElement json, string propertyName, string fallback)
    {
        return json.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? fallback
            : fallback;
    }

    private static int ReadInt(JsonElement json, string propertyName, int fallback)
    {
        return json.TryGetProperty(propertyName, out var property) && property.TryGetInt32(out var value)
            ? value
            : fallback;
    }

    private static string[] ReadStringArray(JsonElement json, string propertyName, string[] fallback)
    {
        if (!json.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Array)
        {
            return fallback;
        }

        var values = new string[property.GetArrayLength()];
        var index = 0;
        foreach (var item in property.EnumerateArray())
        {
            values[index++] = item.ValueKind == JsonValueKind.String ? item.GetString() ?? string.Empty : string.Empty;
        }

        return values;
    }
}
