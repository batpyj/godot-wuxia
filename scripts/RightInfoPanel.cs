using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Godot;

public partial class RightInfoPanel : Control
{
    private const string DefaultSamplePath = "res://data/combat-sample.json";
    private const float PanelRightPadding = 16.0f;
    private const float PanelTop = 140.0f;
    private const float PanelWidth = 270.0f;
    private const float PanelBottomPadding = 16.0f;

    private RightPanelData _data = RightPanelData.Fallback;

    public override void _Ready()
    {
        Name = "RightInfoPanel";
        MouseFilter = MouseFilterEnum.Ignore;
        ZIndex = 150;

        var viewportSize = GetViewportRect().Size;
        SetAnchorsPreset(LayoutPreset.TopLeft);
        Position = new Vector2(viewportSize.X - PanelRightPadding - PanelWidth, PanelTop);
        Size = new Vector2(PanelWidth, Math.Max(360.0f, viewportSize.Y - PanelTop - PanelBottomPadding));

        _data = LoadData(DefaultSamplePath);
        BuildUi();
        SaveManager.StateChanged += OnGameStateChanged;
    }

    public override void _ExitTree()
    {
        SaveManager.StateChanged -= OnGameStateChanged;
    }

    private void OnGameStateChanged(GameState state)
    {
        _data = LoadData(DefaultSamplePath);
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
            Name = "RightPanel",
            MouseFilter = MouseFilterEnum.Ignore
        };
        panel.SetAnchorsPreset(LayoutPreset.FullRect);
        panel.AddThemeStyleboxOverride("panel", MakePanelStyle(new Color(0.09f, 0.075f, 0.05f, 0.88f), new Color(0.77f, 0.57f, 0.27f, 0.66f), 2));
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

        stack.AddChild(BuildAdventureCard(_data.Adventure));
        stack.AddChild(BuildCombatCard(SaveManager.CurrentState.Combat));
        stack.AddChild(BuildSectCard(_data.Sect));
        stack.AddChild(BuildMartialArtsCard(_data.MartialArts));
        stack.AddChild(BuildWeaponCard(_data.Weapon));
    }

    private Control BuildAdventureCard(AdventureData adventure)
    {
        var card = MakeCard("AdventureCard");
        card.AddChild(MakeCardHeader(adventure.Title, adventure.Status));
        card.AddChild(MakePairRow("路线", adventure.Route));
        card.AddChild(MakePairRow("进度", adventure.Progress));
        card.AddChild(MakeBodyLabel(adventure.Note));
        card.AddChild(MakeAdventureButtonRow(adventure));
        return card;
    }

    private Control BuildCombatCard(CombatSnapshot combat)
    {
        var card = MakeCard("CombatEventCard");
        card.AddChild(MakeCardHeader("战斗事件", $"第{combat.Turn}回合"));
        card.AddChild(MakePairRow("结果", DescribeHitKind(combat.LastHitKind)));
        card.AddChild(MakePairRow("伤害", combat.LastDamage <= 0 ? "待出手" : combat.LastDamage.ToString()));

        var combatEvents = combat.Events
            .Where(item => item.Kind is not ("reward" or "drop" or "sect_notice" or "adventure_notice"))
            .TakeLast(4)
            .Select(FormatCombatEvent)
            .ToArray();
        var events = combatEvents.Length > 0
            ? combatEvents
            : combat.EventLog.Length > 0
                ? combat.EventLog.TakeLast(4).ToArray()
                : new[] { combat.Message };
        card.AddChild(MakeInlineTitle("战斗过程", $"{events.Length}条"));
        foreach (var item in events)
        {
            card.AddChild(MakeBodyLabel(item));
        }

        var rewardEvents = combat.Events
            .Where(item => item.Kind is "reward" or "drop" or "sect_notice" or "adventure_notice")
            .TakeLast(4)
            .ToArray();
        if (rewardEvents.Length > 0)
        {
            card.AddChild(MakeInlineTitle("战利结算", $"{rewardEvents.Length}项"));
            foreach (var rewardEvent in rewardEvents)
            {
                card.AddChild(MakeBodyLabel(FormatCombatEvent(rewardEvent)));
            }
        }

        if (combat.MigrationWarnings.Length > 0)
        {
            card.AddChild(MakeInlineTitle("迁移提示", $"{combat.MigrationWarnings.Length}项"));
            foreach (var warning in combat.MigrationWarnings.Take(2))
            {
                card.AddChild(MakeBodyLabel(warning));
            }
        }

        return card;
    }

    private Control MakeAdventureButtonRow(AdventureData adventure)
    {
        var row = new HBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        row.AddThemeConstantOverride("separation", 6);
        row.AddChild(MakeCommandButton(adventure.IsPaused ? "继续" : "暂停", "toggle_pause"));
        row.AddChild(MakeCommandButton(adventure.AutoExplore ? "自动：开" : "自动：关", "toggle_auto_explore"));
        return row;
    }

    private Control BuildSectCard(SectData sect)
    {
        var card = MakeCard("SectCard");
        card.AddChild(MakeCardHeader(sect.Title, sect.Identity));
        card.AddChild(MakePairRow("门派", sect.Name));
        card.AddChild(MakeChipColumn(sect.Bonuses));
        return card;
    }

    private Control BuildMartialArtsCard(MartialArtData[] martialArts)
    {
        var card = MakeCard("MartialArtsCard");
        card.AddChild(MakeCardHeader("功法", $"{martialArts.Length} 门"));

        foreach (var martialArt in martialArts)
        {
            var artCard = new PanelContainer
            {
                Name = $"{martialArt.Name}Card",
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };
            artCard.AddThemeStyleboxOverride("panel", MakePanelStyle(new Color(0.13f, 0.085f, 0.16f, 0.62f), new Color(0.67f, 0.45f, 0.84f, 0.44f), 1));

            var stack = new VBoxContainer
            {
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };
            stack.AddThemeConstantOverride("separation", 3);
            artCard.AddChild(stack);

            stack.AddChild(MakeInlineTitle(martialArt.Name, martialArt.Type));
            stack.AddChild(MakeSmallLabel(martialArt.Level));
            stack.AddChild(MakeBodyLabel(martialArt.Effect));
            card.AddChild(artCard);
        }

        return card;
    }

    private Control BuildWeaponCard(WeaponPanelData weapon)
    {
        var card = MakeCard("WeaponCard");
        card.AddChild(MakeCardHeader(weapon.Title, weapon.Type));
        card.AddChild(MakePairRow("名称", weapon.Name));
        card.AddChild(MakePairRow("评分", weapon.Score.ToString()));
        card.AddChild(MakePairRow("挂点", weapon.Socket));
        card.AddChild(MakeChipColumn(weapon.Effects));
        return card;
    }

    private Button MakeCommandButton(string text, string commandKey)
    {
        var button = new Button
        {
            Text = text,
            MouseFilter = MouseFilterEnum.Stop,
            CustomMinimumSize = new Vector2(92, 26),
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        button.AddThemeFontSizeOverride("font_size", 12);
        button.AddThemeColorOverride("font_color", new Color(0.2f, 0.12f, 0.04f, 1.0f));
        button.AddThemeStyleboxOverride("normal", MakePanelStyle(new Color(0.9f, 0.68f, 0.34f, 0.92f), new Color(1.0f, 0.84f, 0.44f, 0.74f), 1));
        button.AddThemeStyleboxOverride("hover", MakePanelStyle(new Color(1.0f, 0.76f, 0.4f, 0.98f), new Color(1.0f, 0.9f, 0.58f, 0.9f), 1));
        button.Pressed += () => RunCommand(commandKey);
        return button;
    }

    private void RunCommand(string commandKey)
    {
        var result = GameCommands.Execute(commandKey);
        if (!result.Ok)
        {
            GD.PushWarning(result.Message);
        }
    }

    private static string DescribeHitKind(string hitKind)
    {
        return hitKind switch
        {
            "defeat" => "击败",
            "hit" => "重击",
            "graze" => "擦伤",
            "advance" => "推进",
            _ => "待命"
        };
    }

    private static string FormatCombatEvent(CombatEventSnapshot combatEvent)
    {
        return combatEvent.Kind switch
        {
            "advance" => $"推进 · {combatEvent.Message}",
            "defeat" => $"击败 · {combatEvent.Message}",
            "hit" => $"重击 · {combatEvent.Amount}",
            "graze" => $"擦伤 · {combatEvent.Amount}",
            "reward" => $"奖励 · {combatEvent.Message}",
            "drop" => $"掉落 · {combatEvent.Message}",
            "sect_notice" => $"门派 · {combatEvent.Message}",
            "adventure_notice" => $"江湖 · {combatEvent.Message}",
            "import" => combatEvent.Message,
            _ => combatEvent.Message
        };
    }

    private VBoxContainer MakeCard(string name)
    {
        var card = new VBoxContainer
        {
            Name = name,
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        card.AddThemeConstantOverride("separation", 6);
        card.AddThemeStyleboxOverride("panel", MakePanelStyle(new Color(0.12f, 0.09f, 0.055f, 0.68f), new Color(0.58f, 0.42f, 0.2f, 0.46f), 1));
        return card;
    }

    private Control MakeCardHeader(string title, string chip)
    {
        var row = new HBoxContainer
        {
            Name = "Header",
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        row.AddThemeConstantOverride("separation", 8);

        var titleLabel = MakeSectionTitle(title);
        titleLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(titleLabel);
        row.AddChild(MakeChip(chip, 82));
        return row;
    }

    private Control MakeInlineTitle(string title, string chip)
    {
        var row = new HBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        row.AddThemeConstantOverride("separation", 6);

        var titleLabel = MakeValueLabel(title);
        titleLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(titleLabel);
        row.AddChild(MakeChip(chip, 76));
        return row;
    }

    private Control MakePairRow(string label, string value)
    {
        var row = new HBoxContainer
        {
            Name = label,
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        row.AddThemeConstantOverride("separation", 8);

        var left = MakeTinyLabel(label);
        left.CustomMinimumSize = new Vector2(40, 0);
        row.AddChild(left);

        var right = MakeSmallLabel(value);
        right.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        right.HorizontalAlignment = HorizontalAlignment.Right;
        row.AddChild(right);
        return row;
    }

    private Control MakeChipColumn(string[] chips)
    {
        var column = new VBoxContainer
        {
            Name = "Chips",
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        column.AddThemeConstantOverride("separation", 4);

        foreach (var chip in chips)
        {
            column.AddChild(MakeChip(chip, 218));
        }

        return column;
    }

    private static Label MakeSectionTitle(string text)
    {
        var label = new Label
        {
            Text = text,
            MouseFilter = MouseFilterEnum.Ignore,
            VerticalAlignment = VerticalAlignment.Center,
            ClipText = true
        };
        label.AddThemeFontSizeOverride("font_size", 18);
        label.AddThemeColorOverride("font_color", new Color(1.0f, 0.83f, 0.42f, 1.0f));
        label.AddThemeColorOverride("font_shadow_color", new Color(0.12f, 0.07f, 0.03f, 0.8f));
        label.AddThemeConstantOverride("shadow_offset_x", 1);
        label.AddThemeConstantOverride("shadow_offset_y", 1);
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
        label.AddThemeFontSizeOverride("font_size", 16);
        label.AddThemeColorOverride("font_color", new Color(0.98f, 0.91f, 0.7f, 1.0f));
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
        label.AddThemeColorOverride("font_color", new Color(0.84f, 0.76f, 0.58f, 1.0f));
        return label;
    }

    private static Label MakeBodyLabel(string text)
    {
        var label = MakeSmallLabel(text);
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        label.ClipText = false;
        return label;
    }

    private static Label MakeChip(string text, float minWidth)
    {
        var chip = MakeSmallLabel(text);
        chip.CustomMinimumSize = new Vector2(minWidth, 24);
        chip.HorizontalAlignment = HorizontalAlignment.Center;
        chip.VerticalAlignment = VerticalAlignment.Center;
        chip.AddThemeStyleboxOverride("normal", MakePanelStyle(new Color(0.2f, 0.13f, 0.075f, 0.76f), new Color(0.8f, 0.58f, 0.27f, 0.5f), 1));
        return chip;
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

    private static RightPanelData LoadData(string resourcePath)
    {
        try
        {
            var staticData = LoadStaticData(resourcePath);
            var state = SaveManager.CurrentState;
            var weaponType = SaveManager.DescribeWeaponType(state.Hero.Weapon);

            return staticData with
            {
                Adventure = staticData.Adventure with
                {
                    Status = state.Commands.AutoExplore ? "自动闯荡" : state.Commands.IsPaused ? "暂停闯荡" : "手动闯荡",
                    Route = state.Stage.Direction,
                    Progress = $"第{state.Stage.Floor}层 · 当前战力 {state.Stage.CombatPower}",
                    Note = state.Commands.LastCommand,
                    IsPaused = state.Commands.IsPaused,
                    AutoExplore = state.Commands.AutoExplore
                },
                Sect = staticData.Sect with
                {
                    Name = state.Hero.Sect,
                    Identity = state.Hero.Title,
                    Bonuses = state.SectBonuses
                },
                MartialArts = state.MartialArts
                    .Select((art, index) => new MartialArtData(
                        art.Name,
                        art.Type,
                        index == state.Commands.ActiveMartialArtIndex ? $"{art.Level} · 当前查看" : art.Level,
                        art.Effect))
                    .ToArray(),
                Weapon = staticData.Weapon with
                {
                    Title = state.WeaponInfo.Title,
                    Name = state.WeaponInfo.Name,
                    Type = string.IsNullOrWhiteSpace(state.WeaponInfo.Type) ? weaponType : state.WeaponInfo.Type,
                    Score = state.WeaponInfo.Score,
                    Socket = state.WeaponInfo.Socket,
                    Effects = state.WeaponInfo.Effects
                }
            };
        }
        catch (Exception ex)
        {
            GD.PushWarning($"RightInfoPanel state load failed: {ex.Message}");
            return RightPanelData.Fallback;
        }
    }

    private static RightPanelData LoadStaticData(string resourcePath)
    {
        try
        {
            var absolutePath = ProjectSettings.GlobalizePath(resourcePath);
            if (!File.Exists(absolutePath))
            {
                return RightPanelData.Fallback;
            }

            using var document = JsonDocument.Parse(File.ReadAllText(absolutePath));
            return document.RootElement.TryGetProperty("rightPanel", out var panel)
                ? RightPanelData.FromJson(panel)
                : RightPanelData.Fallback;
        }
        catch (Exception ex)
        {
            GD.PushWarning($"RightInfoPanel sample load failed: {ex.Message}");
            return RightPanelData.Fallback;
        }
    }

    private readonly record struct RightPanelData(
        AdventureData Adventure,
        SectData Sect,
        MartialArtData[] MartialArts,
        WeaponPanelData Weapon)
    {
        public static RightPanelData Fallback => new(
            AdventureData.Fallback,
            SectData.Fallback,
            new[] { MartialArtData.Fallback },
            WeaponPanelData.Fallback);

        public static RightPanelData FromJson(JsonElement json)
        {
            return new RightPanelData(
                json.TryGetProperty("adventure", out var adventure) ? AdventureData.FromJson(adventure) : AdventureData.Fallback,
                json.TryGetProperty("sect", out var sect) ? SectData.FromJson(sect) : SectData.Fallback,
                ReadMartialArts(json),
                json.TryGetProperty("weapon", out var weapon) ? WeaponPanelData.FromJson(weapon) : WeaponPanelData.Fallback);
        }
    }

    private readonly record struct AdventureData(string Title, string Status, string Route, string Progress, string Note, bool IsPaused, bool AutoExplore)
    {
        public static AdventureData Fallback => new("江湖闯荡", "暂停闯荡", "养伤潜修 / 练功闭门", "第20层 · 即将挑战第21层", "当前以稳血和练功为主。", true, false);

        public static AdventureData FromJson(JsonElement json)
        {
            return new AdventureData(
                ReadString(json, "title", Fallback.Title),
                ReadString(json, "status", Fallback.Status),
                ReadString(json, "route", Fallback.Route),
                ReadString(json, "progress", Fallback.Progress),
                ReadString(json, "note", Fallback.Note),
                true,
                false);
        }
    }

    private readonly record struct SectData(string Title, string Name, string Identity, string[] Bonuses)
    {
        public static SectData Fallback => new("门派", "少林", "俗家弟子", new[] { "气血回复 +30%", "外功 +12%", "内功 +12%" });

        public static SectData FromJson(JsonElement json)
        {
            return new SectData(
                ReadString(json, "title", Fallback.Title),
                ReadString(json, "name", Fallback.Name),
                ReadString(json, "identity", Fallback.Identity),
                ReadStringArray(json, "bonuses", Fallback.Bonuses));
        }
    }

    private readonly record struct MartialArtData(string Name, string Type, string Level, string Effect)
    {
        public static MartialArtData Fallback => new("无忌心经", "内功常驻", "突破 8/10 重", "内功 +108 / 防御 +35 / 气血 +24");

        public static MartialArtData FromJson(JsonElement json)
        {
            return new MartialArtData(
                ReadString(json, "name", Fallback.Name),
                ReadString(json, "type", Fallback.Type),
                ReadString(json, "level", Fallback.Level),
                ReadString(json, "effect", Fallback.Effect));
        }
    }

    private readonly record struct WeaponPanelData(string Title, string Name, string Type, int Score, string Socket, string[] Effects)
    {
        public static WeaponPanelData Fallback => new("武器", "王府纹剑", "剑", 197, "独立挂点 · 手部跟随", new[] { "剑气拖尾", "攻击评分 +197" });

        public static WeaponPanelData FromJson(JsonElement json)
        {
            return new WeaponPanelData(
                ReadString(json, "title", Fallback.Title),
                ReadString(json, "name", Fallback.Name),
                ReadString(json, "type", Fallback.Type),
                ReadInt(json, "score", Fallback.Score),
                ReadString(json, "socket", Fallback.Socket),
                ReadStringArray(json, "effects", Fallback.Effects));
        }
    }

    private static MartialArtData[] ReadMartialArts(JsonElement json)
    {
        if (!json.TryGetProperty("martialArts", out var property) || property.ValueKind != JsonValueKind.Array)
        {
            return RightPanelData.Fallback.MartialArts;
        }

        var values = new MartialArtData[property.GetArrayLength()];
        var index = 0;
        foreach (var item in property.EnumerateArray())
        {
            values[index++] = item.ValueKind == JsonValueKind.Object ? MartialArtData.FromJson(item) : MartialArtData.Fallback;
        }

        return values.Length > 0 ? values : RightPanelData.Fallback.MartialArts;
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

        return values.Length > 0 ? values : fallback;
    }
}
