using System;
using System.IO;
using System.Text.Json;
using Godot;

public partial class BottomQuickBar : Control
{
    private const string DefaultSamplePath = "res://data/combat-sample.json";
    private const float LeftReserve = 286.0f;
    private const float RightReserve = 286.0f;
    private const float BottomPadding = 16.0f;
    private const float BarHeight = 78.0f;

    private QuickCardData[] _cards = QuickCardData.FallbackSet;

    public override void _Ready()
    {
        Name = "BottomQuickBar";
        MouseFilter = MouseFilterEnum.Ignore;
        ZIndex = 152;

        var viewportSize = GetViewportRect().Size;
        SetAnchorsPreset(LayoutPreset.TopLeft);
        Position = new Vector2(LeftReserve, viewportSize.Y - BottomPadding - BarHeight);
        Size = new Vector2(Math.Max(520.0f, viewportSize.X - LeftReserve - RightReserve), BarHeight);

        _cards = LoadCards(DefaultSamplePath);
        BuildUi();
        SaveManager.StateChanged += OnGameStateChanged;
    }

    public override void _ExitTree()
    {
        SaveManager.StateChanged -= OnGameStateChanged;
    }

    private void OnGameStateChanged(GameState state)
    {
        _cards = LoadCards(DefaultSamplePath);
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
            Name = "QuickBarPanel",
            MouseFilter = MouseFilterEnum.Ignore
        };
        panel.SetAnchorsPreset(LayoutPreset.FullRect);
        panel.AddThemeStyleboxOverride("panel", MakePanelStyle(new Color(0.09f, 0.065f, 0.045f, 0.9f), new Color(0.82f, 0.62f, 0.28f, 0.72f), 2));
        AddChild(panel);

        var margin = new MarginContainer
        {
            Name = "ContentMargin",
            MouseFilter = MouseFilterEnum.Ignore
        };
        margin.AddThemeConstantOverride("margin_left", 10);
        margin.AddThemeConstantOverride("margin_right", 10);
        margin.AddThemeConstantOverride("margin_top", 8);
        margin.AddThemeConstantOverride("margin_bottom", 8);
        panel.AddChild(margin);

        var row = new HBoxContainer
        {
            Name = "Cards",
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        row.AddThemeConstantOverride("separation", 8);
        margin.AddChild(row);

        foreach (var card in _cards)
        {
            row.AddChild(BuildCard(card));
        }
    }

    private Control BuildCard(QuickCardData data)
    {
        var card = new PanelContainer
        {
            Name = $"{data.Title}Card",
            MouseFilter = MouseFilterEnum.Stop,
            CustomMinimumSize = new Vector2(178, 0),
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        card.AddThemeStyleboxOverride("panel", MakePanelStyle(PickBackground(data.Icon), PickBorder(data.Icon), 1));
        card.GuiInput += @event => HandleCardInput(@event, data.Title);

        var margin = new MarginContainer
        {
            MouseFilter = MouseFilterEnum.Ignore
        };
        margin.AddThemeConstantOverride("margin_left", 8);
        margin.AddThemeConstantOverride("margin_right", 8);
        margin.AddThemeConstantOverride("margin_top", 5);
        margin.AddThemeConstantOverride("margin_bottom", 5);
        card.AddChild(margin);

        var row = new HBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        row.AddThemeConstantOverride("separation", 8);
        margin.AddChild(row);

        var icon = MakeIcon(data.Icon);
        row.AddChild(icon);

        var textStack = new VBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        textStack.AddThemeConstantOverride("separation", 2);
        row.AddChild(textStack);

        textStack.AddChild(MakeHeaderRow(data.Title, data.Badge));
        textStack.AddChild(MakeMainLabel(data.Main));
        textStack.AddChild(MakeSubLabel(data.Sub));
        return card;
    }

    private void HandleCardInput(InputEvent @event, string title)
    {
        if (@event is not InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true })
        {
            return;
        }

        var commandKey = title switch
        {
            "江湖" => "toggle_auto_explore",
            "功法" => "next_martial_art",
            "情缘" => "trigger_romance_assist",
            "武器" => "cycle_weapon",
            _ => string.Empty
        };

        if (string.IsNullOrWhiteSpace(commandKey))
        {
            return;
        }

        var result = GameCommands.Execute(commandKey);
        if (!result.Ok)
        {
            GD.PushWarning(result.Message);
        }
    }

    private Control MakeHeaderRow(string title, string badge)
    {
        var row = new HBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        row.AddThemeConstantOverride("separation", 6);

        var titleLabel = MakeTitleLabel(title);
        titleLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(titleLabel);
        row.AddChild(MakeBadge(badge));
        return row;
    }

    private static Label MakeIcon(string text)
    {
        var label = new Label
        {
            Text = text,
            MouseFilter = MouseFilterEnum.Ignore,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            CustomMinimumSize = new Vector2(34, 34)
        };
        label.AddThemeFontSizeOverride("font_size", 19);
        label.AddThemeColorOverride("font_color", new Color(0.16f, 0.1f, 0.04f, 1.0f));
        label.AddThemeStyleboxOverride("normal", MakePanelStyle(new Color(1.0f, 0.83f, 0.42f, 0.92f), new Color(0.48f, 0.32f, 0.12f, 0.85f), 1));
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
        label.AddThemeFontSizeOverride("font_size", 16);
        label.AddThemeColorOverride("font_color", new Color(1.0f, 0.86f, 0.46f, 1.0f));
        return label;
    }

    private static Label MakeMainLabel(string text)
    {
        var label = new Label
        {
            Text = text,
            MouseFilter = MouseFilterEnum.Ignore,
            VerticalAlignment = VerticalAlignment.Center,
            ClipText = true
        };
        label.AddThemeFontSizeOverride("font_size", 13);
        label.AddThemeColorOverride("font_color", new Color(0.98f, 0.92f, 0.74f, 1.0f));
        return label;
    }

    private static Label MakeSubLabel(string text)
    {
        var label = new Label
        {
            Text = text,
            MouseFilter = MouseFilterEnum.Ignore,
            VerticalAlignment = VerticalAlignment.Center,
            ClipText = true
        };
        label.AddThemeFontSizeOverride("font_size", 12);
        label.AddThemeColorOverride("font_color", new Color(0.84f, 0.78f, 0.62f, 1.0f));
        return label;
    }

    private static Label MakeBadge(string text)
    {
        var label = MakeSubLabel(text);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.VerticalAlignment = VerticalAlignment.Center;
        label.CustomMinimumSize = new Vector2(28, 20);
        label.AddThemeStyleboxOverride("normal", MakePanelStyle(new Color(0.85f, 0.66f, 0.28f, 0.78f), new Color(1.0f, 0.84f, 0.42f, 0.56f), 1));
        return label;
    }

    private static Color PickBackground(string icon)
    {
        return icon switch
        {
            "江" => new Color(0.12f, 0.15f, 0.10f, 0.75f),
            "门" => new Color(0.14f, 0.13f, 0.10f, 0.75f),
            "缘" => new Color(0.22f, 0.09f, 0.15f, 0.76f),
            "功" => new Color(0.16f, 0.10f, 0.22f, 0.76f),
            "兵" => new Color(0.23f, 0.14f, 0.06f, 0.76f),
            _ => new Color(0.14f, 0.10f, 0.07f, 0.76f)
        };
    }

    private static Color PickBorder(string icon)
    {
        return icon switch
        {
            "江" => new Color(0.58f, 0.7f, 0.46f, 0.5f),
            "门" => new Color(0.76f, 0.62f, 0.34f, 0.5f),
            "缘" => new Color(0.88f, 0.45f, 0.66f, 0.5f),
            "功" => new Color(0.62f, 0.48f, 0.9f, 0.5f),
            "兵" => new Color(0.9f, 0.62f, 0.26f, 0.5f),
            _ => new Color(0.72f, 0.54f, 0.28f, 0.5f)
        };
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

    private static QuickCardData[] LoadCards(string resourcePath)
    {
        try
        {
            return ApplyGameState(LoadStaticCards(resourcePath), SaveManager.CurrentState);
        }
        catch (Exception ex)
        {
            GD.PushWarning($"BottomQuickBar state load failed: {ex.Message}");
            return QuickCardData.FallbackSet;
        }
    }

    private static QuickCardData[] LoadStaticCards(string resourcePath)
    {
        try
        {
            var absolutePath = ProjectSettings.GlobalizePath(resourcePath);
            if (!File.Exists(absolutePath))
            {
                return QuickCardData.FallbackSet;
            }

            using var document = JsonDocument.Parse(File.ReadAllText(absolutePath));
            if (!document.RootElement.TryGetProperty("bottomBar", out var bar) || bar.ValueKind != JsonValueKind.Array)
            {
                return QuickCardData.FallbackSet;
            }

            var cards = new QuickCardData[bar.GetArrayLength()];
            var index = 0;
            foreach (var item in bar.EnumerateArray())
            {
                cards[index++] = item.ValueKind == JsonValueKind.Object ? QuickCardData.FromJson(item) : QuickCardData.FallbackSet[Math.Min(index, QuickCardData.FallbackSet.Length - 1)];
            }

            return cards.Length > 0 ? cards : QuickCardData.FallbackSet;
        }
        catch (Exception ex)
        {
            GD.PushWarning($"BottomQuickBar sample load failed: {ex.Message}");
            return QuickCardData.FallbackSet;
        }
    }

    private static QuickCardData[] ApplyGameState(QuickCardData[] cards, GameState state)
    {
        var result = new QuickCardData[cards.Length];
        Array.Copy(cards, result, cards.Length);
        var activeMartialArtIndex = Math.Clamp(state.Commands.ActiveMartialArtIndex, 0, Math.Max(0, state.MartialArts.Length - 1));
        var primaryMartialArt = state.MartialArts.Length > 0
            ? state.MartialArts[activeMartialArtIndex]
            : new MartialArtSnapshot("无忌心经", "内功常驻", "突破 8/10 重", "内功 +108 / 防御 +35 / 气血 +24");
        var weaponEffect = state.WeaponInfo.Effects.Length > 0 ? state.WeaponInfo.Effects[0] : "武器挂点随角色刷新";

        for (var index = 0; index < result.Length; index++)
        {
            var card = result[index];
            result[index] = card.Title switch
            {
                "江湖" => card with
                {
                    Main = $"闯荡 · 第{state.Stage.Floor}层",
                    Sub = state.Commands.AutoExplore ? "自动闯荡中" : state.Commands.IsPaused ? "已暂停" : state.Stage.Direction,
                    Badge = state.Commands.AutoExplore ? "自" : state.Commands.IsPaused ? "停" : "行"
                },
                "门派" => card with
                {
                    Main = $"{state.Hero.Sect} · {state.Hero.Title}",
                    Sub = state.Hero.Realm
                },
                "情缘" => card with
                {
                    Main = $"{state.Romance.Name} · 好感{state.Romance.Favor}",
                    Sub = state.Romance.Status,
                    Badge = state.Romance.Badge
                },
                "功法" => card with
                {
                    Main = $"已接入功法 {state.MartialArts.Length}门",
                    Sub = $"{primaryMartialArt.Name} · {primaryMartialArt.Level}",
                    Badge = "查"
                },
                "武器" => card with
                {
                    Main = $"{state.WeaponInfo.Name} · 评分{state.WeaponInfo.Score}",
                    Sub = weaponEffect
                },
                _ => card
            };
        }

        return result.Length > 0 ? result : QuickCardData.FallbackSet;
    }

    private readonly record struct QuickCardData(string Icon, string Title, string Main, string Sub, string Badge)
    {
        public static QuickCardData[] FallbackSet => new[]
        {
            new QuickCardData("江", "江湖", "闯荡 · 第20层", "养伤潜修中", "中"),
            new QuickCardData("门", "门派", "少林 · 俗家弟子", "气血回复 +30%", "开"),
            new QuickCardData("缘", "情缘", "沈青萝 · 好感15", "援手待部署", "待"),
            new QuickCardData("功", "功法", "已学功法 10/13", "无忌心经突破 8重", "开"),
            new QuickCardData("兵", "武器", "王府纹剑 · 评分197", "剑气拖尾", "开")
        };

        public static QuickCardData FromJson(JsonElement json)
        {
            return new QuickCardData(
                ReadString(json, "icon", "江"),
                ReadString(json, "title", "江湖"),
                ReadString(json, "main", "闯荡 · 第20层"),
                ReadString(json, "sub", "养伤潜修中"),
                ReadString(json, "badge", "中"));
        }
    }

    private static string ReadString(JsonElement json, string propertyName, string fallback)
    {
        return json.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? fallback
            : fallback;
    }

    private static string FormatNumber(int value)
    {
        return value >= 10000 && value % 10000 == 0 ? $"{value / 10000}万" : value.ToString();
    }
}
