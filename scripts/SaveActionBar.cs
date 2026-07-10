using System;
using System.IO;
using System.Text.Json;
using Godot;

public partial class SaveActionBar : Control
{
    private const string DefaultSamplePath = "res://data/combat-sample.json";
    private static readonly Vector2 BarPosition = new(904, 130);
    private static readonly Vector2 BarSize = new(392, 42);

    private SaveActionData[] _actions = SaveActionData.FallbackSet;

    public override void _Ready()
    {
        Name = "SaveActionBar";
        MouseFilter = MouseFilterEnum.Ignore;
        ZIndex = 170;
        SetAnchorsPreset(LayoutPreset.TopLeft);
        Position = BarPosition;
        Size = BarSize;

        _actions = LoadActions(DefaultSamplePath);
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
            Name = "SaveActionPanel",
            MouseFilter = MouseFilterEnum.Ignore
        };
        panel.SetAnchorsPreset(LayoutPreset.FullRect);
        panel.AddThemeStyleboxOverride("panel", MakePanelStyle(new Color(0.08f, 0.055f, 0.035f, 0.72f), new Color(0.72f, 0.52f, 0.24f, 0.54f), 1));
        AddChild(panel);

        var margin = new MarginContainer
        {
            MouseFilter = MouseFilterEnum.Ignore
        };
        margin.AddThemeConstantOverride("margin_left", 7);
        margin.AddThemeConstantOverride("margin_right", 7);
        margin.AddThemeConstantOverride("margin_top", 5);
        margin.AddThemeConstantOverride("margin_bottom", 5);
        panel.AddChild(margin);

        var row = new HBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        row.AddThemeConstantOverride("separation", 7);
        margin.AddChild(row);

        foreach (var action in _actions)
        {
            row.AddChild(MakeActionButton(action));
        }
    }

    private Button MakeActionButton(SaveActionData action)
    {
        var button = new Button
        {
            Text = action.Label,
            TooltipText = action.Hint,
            CustomMinimumSize = new Vector2(118, 30),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Stop
        };
        button.AddThemeFontSizeOverride("font_size", 14);
        button.AddThemeColorOverride("font_color", action.Key == "clear" ? new Color(1.0f, 0.82f, 0.62f, 1.0f) : new Color(0.2f, 0.12f, 0.04f, 1.0f));
        button.AddThemeStyleboxOverride("normal", action.Key == "clear"
            ? MakePanelStyle(new Color(0.44f, 0.14f, 0.09f, 0.9f), new Color(0.95f, 0.5f, 0.28f, 0.74f), 1)
            : MakePanelStyle(new Color(0.9f, 0.66f, 0.28f, 0.92f), new Color(1.0f, 0.84f, 0.42f, 0.78f), 1));
        button.AddThemeStyleboxOverride("hover", action.Key == "clear"
            ? MakePanelStyle(new Color(0.56f, 0.18f, 0.1f, 0.96f), new Color(1.0f, 0.62f, 0.36f, 0.9f), 1)
            : MakePanelStyle(new Color(1.0f, 0.76f, 0.36f, 0.98f), new Color(1.0f, 0.9f, 0.52f, 0.92f), 1));
        button.Pressed += () => ShowSaveDialog(action.Key);
        return button;
    }

    private void ShowSaveDialog(string key)
    {
        if (GetParent()?.GetNodeOrNull<GameModalLayer>("GameModalLayer") is { } modal)
        {
            modal.ShowPreset(key);
        }
    }

    private static SaveActionData[] LoadActions(string resourcePath)
    {
        try
        {
            var absolutePath = ProjectSettings.GlobalizePath(resourcePath);
            if (!File.Exists(absolutePath))
            {
                return SaveActionData.FallbackSet;
            }

            using var document = JsonDocument.Parse(File.ReadAllText(absolutePath));
            if (!document.RootElement.TryGetProperty("saveActions", out var actions) || actions.ValueKind != JsonValueKind.Array)
            {
                return SaveActionData.FallbackSet;
            }

            var values = new SaveActionData[actions.GetArrayLength()];
            var index = 0;
            foreach (var item in actions.EnumerateArray())
            {
                values[index++] = item.ValueKind == JsonValueKind.Object ? SaveActionData.FromJson(item) : SaveActionData.FallbackSet[Math.Min(index, SaveActionData.FallbackSet.Length - 1)];
            }

            return values.Length > 0 ? values : SaveActionData.FallbackSet;
        }
        catch (Exception ex)
        {
            GD.PushWarning($"SaveActionBar sample load failed: {ex.Message}");
            return SaveActionData.FallbackSet;
        }
    }

    private static StyleBoxFlat MakePanelStyle(Color background, Color border, int borderWidth)
    {
        return new StyleBoxFlat
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
    }

    private readonly record struct SaveActionData(string Key, string Label, string Hint)
    {
        public static SaveActionData[] FallbackSet => new[]
        {
            new SaveActionData("import", "导入存档", "粘贴文本"),
            new SaveActionData("export", "导出存档", "复制文本"),
            new SaveActionData("clear", "清空存档", "二次确认")
        };

        public static SaveActionData FromJson(JsonElement json)
        {
            return new SaveActionData(
                ReadString(json, "key", "import"),
                ReadString(json, "label", "导入存档"),
                ReadString(json, "hint", "游戏内弹窗"));
        }
    }

    private static string ReadString(JsonElement json, string propertyName, string fallback)
    {
        return json.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? fallback
            : fallback;
    }
}
