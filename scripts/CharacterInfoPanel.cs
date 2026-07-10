using System;
using System.IO;
using System.Text.Json;
using Godot;

public partial class CharacterInfoPanel : Control
{
    private const string DefaultSamplePath = "res://data/combat-sample.json";
    private const float PanelLeft = 16.0f;
    private const float PanelTop = 140.0f;
    private const float PanelWidth = 250.0f;
    private const float PanelBottomPadding = 16.0f;
    private HeroPanelData _hero = HeroPanelData.Fallback;

    public override void _Ready()
    {
        Name = "CharacterInfoPanel";
        MouseFilter = MouseFilterEnum.Ignore;
        ZIndex = 150;

        SetAnchorsPreset(LayoutPreset.TopLeft);
        Position = new Vector2(PanelLeft, PanelTop);
        Size = new Vector2(PanelWidth, Math.Max(360.0f, GetViewportRect().Size.Y - PanelTop - PanelBottomPadding));

        _hero = LoadHero(DefaultSamplePath);
        BuildUi();
        SaveManager.StateChanged += OnGameStateChanged;
    }

    public override void _ExitTree()
    {
        SaveManager.StateChanged -= OnGameStateChanged;
    }

    private void OnGameStateChanged(GameState state)
    {
        _hero = LoadHero(DefaultSamplePath);
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
            Name = "CharacterPanel",
            MouseFilter = MouseFilterEnum.Ignore
        };
        panel.SetAnchorsPreset(LayoutPreset.FullRect);
        panel.AddThemeStyleboxOverride("panel", MakePanelStyle(new Color(0.11f, 0.085f, 0.055f, 0.88f), new Color(0.78f, 0.58f, 0.26f, 0.68f), 2));
        AddChild(panel);

        var margin = new MarginContainer
        {
            Name = "ContentMargin",
            MouseFilter = MouseFilterEnum.Ignore
        };
        margin.AddThemeConstantOverride("margin_left", 12);
        margin.AddThemeConstantOverride("margin_right", 12);
        margin.AddThemeConstantOverride("margin_top", 12);
        margin.AddThemeConstantOverride("margin_bottom", 12);
        panel.AddChild(margin);

        var scroll = new ScrollContainer
        {
            Name = "Scroll",
            MouseFilter = MouseFilterEnum.Ignore,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto
        };
        margin.AddChild(scroll);

        var stack = new VBoxContainer
        {
            Name = "Stack",
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        stack.AddThemeConstantOverride("separation", 10);
        scroll.AddChild(stack);

        stack.AddChild(BuildIdentityBlock());
        stack.AddChild(BuildSection("综合状态", new[]
        {
            ("综合战力", FormatNumber(_hero.ComprehensivePower)),
            ("气血", $"{FormatNumber(_hero.Hp)} / {FormatNumber(_hero.MaxHp)}"),
            ("修为", $"{FormatNumber(_hero.Cultivation)}"),
            ("修为/秒", _hero.CultivationPerSecond.ToString("0.0"))
        }));
        stack.AddChild(BuildSection("战斗数值", new[]
        {
            ("外功", _hero.BattleStats.OuterPower.ToString()),
            ("内功", _hero.BattleStats.InnerPower.ToString()),
            ("物防", _hero.BattleStats.OuterDefense.ToString()),
            ("内防", _hero.BattleStats.InnerDefense.ToString()),
            ("暴击率", $"{_hero.BattleStats.CritRate}%"),
            ("闪避率", $"{_hero.BattleStats.DodgeRate}%")
        }));
        stack.AddChild(BuildSection("资源材料", new[]
        {
            ("银两", FormatNumber(_hero.Resources.Silver)),
            ("精铁", FormatNumber(_hero.Resources.Essence)),
            ("寒玉", FormatNumber(_hero.Resources.Food))
        }));
        stack.AddChild(BuildSection("江湖资质", new[]
        {
            ("声望", _hero.Qualities.Reputation.ToString()),
            ("悟性", _hero.Qualities.Comprehension.ToString()),
            ("机缘", _hero.Qualities.Luck.ToString()),
            ("魅力", _hero.Qualities.Charm.ToString()),
            ("根骨", _hero.Qualities.Root.ToString()),
            ("定力", _hero.Qualities.Resolve.ToString())
        }));
    }

    private Control BuildIdentityBlock()
    {
        var block = new VBoxContainer
        {
            Name = "Identity",
            MouseFilter = MouseFilterEnum.Ignore
        };
        block.AddThemeConstantOverride("separation", 8);

        var row = new HBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore
        };
        row.AddThemeConstantOverride("separation", 10);
        block.AddChild(row);

        var portraitFrame = new PanelContainer
        {
            CustomMinimumSize = new Vector2(70, 74),
            MouseFilter = MouseFilterEnum.Ignore
        };
        portraitFrame.AddThemeStyleboxOverride("panel", MakePanelStyle(new Color(0.08f, 0.065f, 0.045f, 0.92f), new Color(0.84f, 0.66f, 0.34f, 0.72f), 2));
        row.AddChild(portraitFrame);

        var portrait = new TextureRect
        {
            Texture = LoadTexture(_hero.PortraitPath),
            CustomMinimumSize = new Vector2(66, 70),
            MouseFilter = MouseFilterEnum.Ignore,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered
        };
        portraitFrame.AddChild(portrait);

        var textStack = new VBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        textStack.AddThemeConstantOverride("separation", 4);
        row.AddChild(textStack);

        textStack.AddChild(MakeSmallLabel("侠客档案"));
        textStack.AddChild(MakeTitleLabel(_hero.Name));
        textStack.AddChild(MakeSmallLabel(_hero.Title));
        textStack.AddChild(MakeChip($"{_hero.Sect} / {_hero.Realm}"));

        var hpBar = MakeProgressLine("气血", _hero.Hp, _hero.MaxHp, new Color(0.38f, 0.86f, 0.48f, 0.95f));
        block.AddChild(hpBar);
        block.AddChild(MakeChipRow(_hero.Effects));

        return block;
    }

    private Control BuildSection(string title, (string Label, string Value)[] values)
    {
        var section = new VBoxContainer
        {
            Name = title,
            MouseFilter = MouseFilterEnum.Ignore
        };
        section.AddThemeConstantOverride("separation", 6);
        section.AddChild(MakeSectionTitle(title));

        var grid = new GridContainer
        {
            Columns = 2,
            MouseFilter = MouseFilterEnum.Ignore
        };
        grid.AddThemeConstantOverride("h_separation", 6);
        grid.AddThemeConstantOverride("v_separation", 6);
        section.AddChild(grid);

        foreach (var (label, value) in values)
        {
            grid.AddChild(MakeStatCell(label, value));
        }

        return section;
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
            CustomMinimumSize = new Vector2(0, 11)
        };
        bar.AddThemeStyleboxOverride("background", MakePanelStyle(new Color(0.04f, 0.035f, 0.026f, 0.84f), new Color(0.2f, 0.16f, 0.1f, 0.8f), 1));
        bar.AddThemeStyleboxOverride("fill", MakePanelStyle(fillColor, fillColor, 0));
        root.AddChild(bar);

        return root;
    }

    private Control MakeChipRow(string[] chips)
    {
        var wrap = new GridContainer
        {
            Columns = 1,
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        wrap.AddThemeConstantOverride("v_separation", 4);

        foreach (var chip in chips)
        {
            wrap.AddChild(MakeChip(chip, 190));
        }

        return wrap;
    }

    private Label MakeChip(string text, float minWidth = 0.0f)
    {
        var chip = MakeSmallLabel(text);
        chip.CustomMinimumSize = new Vector2(minWidth, 24);
        chip.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        chip.HorizontalAlignment = HorizontalAlignment.Center;
        chip.VerticalAlignment = VerticalAlignment.Center;
        chip.AddThemeStyleboxOverride("normal", MakePanelStyle(new Color(0.19f, 0.13f, 0.08f, 0.72f), new Color(0.78f, 0.58f, 0.27f, 0.48f), 1));
        return chip;
    }

    private Control MakeStatCell(string label, string value)
    {
        var cell = new VBoxContainer
        {
            Name = label,
            MouseFilter = MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(106, 50),
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        cell.AddThemeConstantOverride("separation", 2);
        cell.AddThemeStyleboxOverride("panel", MakePanelStyle(new Color(0.09f, 0.07f, 0.05f, 0.74f), new Color(0.42f, 0.32f, 0.18f, 0.5f), 1));
        cell.AddChild(MakeTinyLabel(label));
        cell.AddChild(MakeValueLabel(value));
        return cell;
    }

    private static Label MakeSectionTitle(string text)
    {
        var label = new Label
        {
            Text = text,
            MouseFilter = MouseFilterEnum.Ignore,
            VerticalAlignment = VerticalAlignment.Center
        };
        label.AddThemeFontSizeOverride("font_size", 16);
        label.AddThemeColorOverride("font_color", new Color(1.0f, 0.83f, 0.42f, 1.0f));
        return label;
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
        label.AddThemeFontSizeOverride("font_size", 22);
        label.AddThemeColorOverride("font_color", new Color(1.0f, 0.88f, 0.48f, 1.0f));
        return label;
    }

    private static Label MakeValueLabel(string text)
    {
        var label = new Label
        {
            Text = text,
            MouseFilter = MouseFilterEnum.Ignore,
            VerticalAlignment = VerticalAlignment.Center,
            ClipText = true
        };
        label.AddThemeFontSizeOverride("font_size", 18);
        label.AddThemeColorOverride("font_color", new Color(0.98f, 0.92f, 0.74f, 1.0f));
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
        label.AddThemeFontSizeOverride("font_size", 13);
        label.AddThemeColorOverride("font_color", new Color(0.96f, 0.9f, 0.76f, 1.0f));
        return label;
    }

    private static Label MakeTinyLabel(string text)
    {
        var label = MakeSmallLabel(text);
        label.AddThemeFontSizeOverride("font_size", 12);
        label.AddThemeColorOverride("font_color", new Color(0.86f, 0.77f, 0.58f, 1.0f));
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
            CornerRadiusBottomRight = 8,
            ContentMarginLeft = 8,
            ContentMarginRight = 8,
            ContentMarginTop = 5,
            ContentMarginBottom = 5
        };
        return style;
    }

    private static HeroPanelData LoadHero(string resourcePath)
    {
        try
        {
            var staticHero = LoadStaticHero(resourcePath);
            var stateHero = SaveManager.CurrentState.Hero;
            return staticHero with
            {
                Name = stateHero.Name,
                Title = stateHero.Title,
                Sect = stateHero.Sect,
                Realm = stateHero.Realm,
                Hp = stateHero.Hp,
                MaxHp = stateHero.MaxHp,
                ComprehensivePower = stateHero.ComprehensivePower,
                Cultivation = stateHero.Cultivation,
                CultivationPerSecond = stateHero.CultivationPerSecond,
                Effects = stateHero.Effects,
                BattleStats = new BattleStats(
                    stateHero.BattleStats.OuterPower,
                    stateHero.BattleStats.InnerPower,
                    stateHero.BattleStats.OuterDefense,
                    stateHero.BattleStats.InnerDefense,
                    stateHero.BattleStats.CritRate,
                    stateHero.BattleStats.DodgeRate),
                Resources = new Resources(
                    stateHero.Resources.Silver,
                    stateHero.Resources.Essence,
                    stateHero.Resources.Food),
                Qualities = new Qualities(
                    stateHero.JianghuQualities.Reputation,
                    stateHero.JianghuQualities.Comprehension,
                    stateHero.JianghuQualities.Luck,
                    stateHero.JianghuQualities.Charm,
                    stateHero.JianghuQualities.Root,
                    stateHero.JianghuQualities.Resolve)
            };
        }
        catch (Exception ex)
        {
            GD.PushWarning($"CharacterInfoPanel state load failed: {ex.Message}");
            return HeroPanelData.Fallback;
        }
    }

    private static HeroPanelData LoadStaticHero(string resourcePath)
    {
        try
        {
            var absolutePath = ProjectSettings.GlobalizePath(resourcePath);
            if (!File.Exists(absolutePath))
            {
                return HeroPanelData.Fallback;
            }

            using var document = JsonDocument.Parse(File.ReadAllText(absolutePath));
            return HeroPanelData.FromJson(document.RootElement.GetProperty("hero"));
        }
        catch (Exception ex)
        {
            GD.PushWarning($"CharacterInfoPanel sample load failed: {ex.Message}");
            return HeroPanelData.Fallback;
        }
    }

    private static Texture2D LoadTexture(string path)
    {
        return GD.Load<Texture2D>(path) ?? GD.Load<Texture2D>("res://assets/actors/hero/hero_male_idle.webp")!;
    }

    private static string FormatNumber(int value)
    {
        return value >= 10000 && value % 10000 == 0 ? $"{value / 10000}万" : value.ToString();
    }

    private readonly record struct HeroPanelData(
        string Name,
        string Title,
        string Sect,
        string Realm,
        string PortraitPath,
        int Hp,
        int MaxHp,
        int ComprehensivePower,
        int Cultivation,
        double CultivationPerSecond,
        string[] Effects,
        BattleStats BattleStats,
        Resources Resources,
        Qualities Qualities)
    {
        public static HeroPanelData Fallback => new(
            "江湖风云",
            "少林俗家弟子",
            "少林",
            "一代宗师",
            "res://assets/actors/hero/hero_male_idle.webp",
            16000,
            17000,
            17000,
            4357,
            17.5,
            new[] { "内功增伤 +13%", "破防 +1%", "伤害减免 -5%" },
            new BattleStats(1410, 1217, 832, 1010, 82, 46),
            new Resources(3589, 62, 18),
            new Qualities(47, 55, 16, 24, 51, 19));

        public static HeroPanelData FromJson(JsonElement json)
        {
            return new HeroPanelData(
                ReadString(json, "name", Fallback.Name),
                ReadString(json, "title", Fallback.Title),
                ReadString(json, "sect", Fallback.Sect),
                ReadString(json, "realm", Fallback.Realm),
                ReadString(json, "portraitPath", Fallback.PortraitPath),
                ReadInt(json, "hp", Fallback.Hp),
                ReadInt(json, "maxHp", Fallback.MaxHp),
                ReadInt(json, "comprehensivePower", Fallback.ComprehensivePower),
                ReadInt(json, "cultivation", Fallback.Cultivation),
                ReadDouble(json, "cultivationPerSecond", Fallback.CultivationPerSecond),
                ReadStringArray(json, "effects", Fallback.Effects),
                BattleStats.FromJson(ReadObject(json, "battleStats")),
                Resources.FromJson(ReadObject(json, "resources")),
                Qualities.FromJson(ReadObject(json, "jianghuQualities")));
        }
    }

    private readonly record struct BattleStats(int OuterPower, int InnerPower, int OuterDefense, int InnerDefense, int CritRate, int DodgeRate)
    {
        public static BattleStats FromJson(JsonElement json)
        {
            return new BattleStats(
                ReadInt(json, "outerPower", 1410),
                ReadInt(json, "innerPower", 1217),
                ReadInt(json, "outerDefense", 832),
                ReadInt(json, "innerDefense", 1010),
                ReadInt(json, "critRate", 82),
                ReadInt(json, "dodgeRate", 46));
        }
    }

    private readonly record struct Resources(int Silver, int Essence, int Food)
    {
        public static Resources FromJson(JsonElement json)
        {
            return new Resources(
                ReadInt(json, "silver", 3589),
                ReadInt(json, "essence", 62),
                ReadInt(json, "food", 18));
        }
    }

    private readonly record struct Qualities(int Reputation, int Comprehension, int Luck, int Charm, int Root, int Resolve)
    {
        public static Qualities FromJson(JsonElement json)
        {
            return new Qualities(
                ReadInt(json, "reputation", 47),
                ReadInt(json, "comprehension", 55),
                ReadInt(json, "luck", 16),
                ReadInt(json, "charm", 24),
                ReadInt(json, "root", 51),
                ReadInt(json, "resolve", 19));
        }
    }

    private static JsonElement ReadObject(JsonElement json, string propertyName)
    {
        return json.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.Object
            ? property
            : JsonDocument.Parse("{}").RootElement;
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

    private static double ReadDouble(JsonElement json, string propertyName, double fallback)
    {
        return json.TryGetProperty(propertyName, out var property) && property.TryGetDouble(out var value)
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
