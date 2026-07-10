using System;
using System.IO;
using System.Text.Json;
using Godot;

public partial class GameModalLayer : Control
{
    private const string DefaultSamplePath = "res://data/combat-sample.json";
    private const float StageLeft = 286.0f;
    private const float StageTop = 124.0f;
    private const float StageRight = 1314.0f;
    private const float StageBottom = 806.0f;
    private const float DialogWidth = 620.0f;
    private const float DialogHeight = 360.0f;
    private const float DialogHeightWithInput = 460.0f;

    private ModalData _data = ModalData.Fallback;
    private string _activePresetKey = "detail";
    private TextEdit? _activeInput;

    public override void _Ready()
    {
        Name = "GameModalLayer";
        ZIndex = 240;
        MouseFilter = MouseFilterEnum.Ignore;

        SetAnchorsPreset(LayoutPreset.TopLeft);
        Position = new Vector2(StageLeft, StageTop);
        Size = new Vector2(StageRight - StageLeft, StageBottom - StageTop);
        Visible = false;

        _data = LoadModal(DefaultSamplePath, "detail");
        var previewKey = ReadPreviewKey();
        if (!string.IsNullOrWhiteSpace(previewKey))
        {
            ShowPreset(previewKey);
        }
    }

    public void ShowPreset(string key)
    {
        _activePresetKey = key;
        ShowModal(LoadModal(DefaultSamplePath, key));
    }

    public void ShowModal(ModalData data)
    {
        _data = data;
        Visible = true;
        MouseFilter = MouseFilterEnum.Stop;
        BuildUi();
    }

    public void HideModal()
    {
        Visible = false;
        MouseFilter = MouseFilterEnum.Ignore;
    }

    private void BuildUi()
    {
        foreach (var child in GetChildren())
        {
            child.QueueFree();
        }
        _activeInput = null;

        var overlay = new ColorRect
        {
            Name = "StageDimmer",
            Color = new Color(0.03f, 0.022f, 0.015f, 0.62f),
            Size = Size,
            MouseFilter = MouseFilterEnum.Stop
        };
        AddChild(overlay);

        var dialogHeight = HasInput(_data) ? DialogHeightWithInput : DialogHeight;
        var dialog = new PanelContainer
        {
            Name = "Dialog",
            Position = new Vector2((Size.X - DialogWidth) / 2.0f, (Size.Y - dialogHeight) / 2.0f),
            Size = new Vector2(DialogWidth, dialogHeight),
            MouseFilter = MouseFilterEnum.Stop
        };
        dialog.AddThemeStyleboxOverride("panel", _data.Danger
            ? MakePanelStyle(new Color(0.2f, 0.045f, 0.035f, 0.97f), new Color(1.0f, 0.42f, 0.24f, 0.94f), 3)
            : MakePanelStyle(new Color(0.18f, 0.075f, 0.045f, 0.96f), new Color(0.96f, 0.68f, 0.28f, 0.94f), 3));
        AddChild(dialog);

        var margin = new MarginContainer
        {
            Name = "ContentMargin",
            MouseFilter = MouseFilterEnum.Ignore
        };
        margin.AddThemeConstantOverride("margin_left", 20);
        margin.AddThemeConstantOverride("margin_right", 20);
        margin.AddThemeConstantOverride("margin_top", 16);
        margin.AddThemeConstantOverride("margin_bottom", 16);
        dialog.AddChild(margin);

        var stack = new VBoxContainer
        {
            Name = "Stack",
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        stack.AddThemeConstantOverride("separation", 10);
        margin.AddChild(stack);

        stack.AddChild(MakeEyebrow(_data.Eyebrow));
        stack.AddChild(MakeTitle(_data.Title));
        stack.AddChild(MakeBody(_data.Body));
        stack.AddChild(MakeDetailBox(_data.Details));
        if (HasInput(_data))
        {
            stack.AddChild(MakeInputBox(_data));
        }

        var spacer = new Control
        {
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        stack.AddChild(spacer);
        stack.AddChild(MakeButtonRow(_data));
    }

    private static Control MakeDetailBox(string[] details)
    {
        var panel = new PanelContainer
        {
            Name = "DetailBox",
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        panel.AddThemeStyleboxOverride("panel", MakePanelStyle(new Color(0.11f, 0.07f, 0.045f, 0.82f), new Color(0.8f, 0.55f, 0.24f, 0.52f), 1));

        var stack = new VBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        stack.AddThemeConstantOverride("separation", 5);
        panel.AddChild(stack);

        foreach (var detail in details)
        {
            stack.AddChild(MakeSmallLabel(detail));
        }

        return panel;
    }

    private Control MakeInputBox(ModalData data)
    {
        var panel = new PanelContainer
        {
            Name = "InputBox",
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        panel.AddThemeStyleboxOverride("panel", MakePanelStyle(new Color(0.06f, 0.045f, 0.032f, 0.9f), new Color(0.76f, 0.54f, 0.26f, 0.5f), 1));

        var stack = new VBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        stack.AddThemeConstantOverride("separation", 4);
        panel.AddChild(stack);

        stack.AddChild(MakeSmallLabel(data.InputLabel));

        var text = new TextEdit
        {
            Text = data.InputText,
            Editable = !data.InputReadOnly,
            CustomMinimumSize = new Vector2(0, 92),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Stop
        };
        text.AddThemeFontSizeOverride("font_size", 13);
        text.AddThemeColorOverride("font_color", new Color(0.98f, 0.9f, 0.72f, 1.0f));
        text.AddThemeStyleboxOverride("normal", MakePanelStyle(new Color(0.025f, 0.022f, 0.018f, 0.96f), new Color(0.52f, 0.36f, 0.18f, 0.64f), 1));
        text.AddThemeStyleboxOverride("read_only", MakePanelStyle(new Color(0.03f, 0.028f, 0.024f, 0.96f), new Color(0.52f, 0.36f, 0.18f, 0.64f), 1));
        _activeInput = text;
        stack.AddChild(text);

        return panel;
    }

    private Control MakeButtonRow(ModalData data)
    {
        var row = new HBoxContainer
        {
            Name = "Buttons",
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        row.AddThemeConstantOverride("separation", 12);

        var spacer = new Control
        {
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        row.AddChild(spacer);

        var cancel = MakeButton(data.Cancel, false);
        cancel.Pressed += HideModal;
        row.AddChild(cancel);

        var confirm = MakeButton(data.Confirm, true);
        confirm.Pressed += HandleConfirm;
        row.AddChild(confirm);
        return row;
    }

    private void HandleConfirm()
    {
        switch (_activePresetKey)
        {
            case "import":
                ShowImportResult(SaveManager.TryImportSaveText(_activeInput?.Text ?? string.Empty));
                break;
            case "export":
                DisplayServer.ClipboardSet(_activeInput?.Text ?? SaveManager.ExportCurrentSaveText());
                ShowModal(new ModalData(
                    "导出存档",
                    "已复制存档文本",
                    "当前存档文本已经写入系统剪贴板。你也可以从导出弹窗里手动选择复制。",
                    new[] { SaveManager.DescribeCurrentState(), "格式：" + SaveManager.SaveTextPrefix },
                    string.Empty,
                    string.Empty,
                    false,
                    false,
                    "确认",
                    "关闭"));
                break;
            case "clear":
                SaveManager.ClearLocalSave();
                ShowModal(new ModalData(
                    "清空存档",
                    "本地存档已清空",
                    "当前已删除 Godot 本地存档槽，并回退到样例初始状态。真实游戏绑定后这里会刷新全部 UI 状态。",
                    new[] { SaveManager.DescribeCurrentState() },
                    string.Empty,
                    string.Empty,
                    false,
                    false,
                    "确认",
                    "关闭"));
                break;
            default:
                HideModal();
                break;
        }
    }

    private void ShowImportResult(ImportResult result)
    {
        if (result.Ok)
        {
            ShowModal(new ModalData(
                "导入存档",
                "导入成功",
                "存档文本已通过版本、结构和校验码检查，并写入 Godot 本地存档槽。",
                new[] { SaveManager.DescribeCurrentState() },
                string.Empty,
                string.Empty,
                false,
                false,
                "确认",
                "关闭"));
            return;
        }

        ShowModal(new ModalData(
            "导入存档",
            "导入失败",
            "没有覆盖当前存档。请确认文本完整、前缀正确，并且没有被额外空格或换行破坏。",
            new[] { result.Message },
            string.Empty,
            string.Empty,
            false,
            true,
            "确认",
            "关闭"));
    }

    private static Button MakeButton(string text, bool primary)
    {
        var button = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(112, 34),
            MouseFilter = MouseFilterEnum.Stop
        };
        button.AddThemeFontSizeOverride("font_size", 15);
        button.AddThemeColorOverride("font_color", primary ? new Color(0.14f, 0.08f, 0.025f, 1.0f) : new Color(0.98f, 0.88f, 0.62f, 1.0f));
        button.AddThemeStyleboxOverride("normal", primary
            ? MakePanelStyle(new Color(0.96f, 0.72f, 0.32f, 0.96f), new Color(1.0f, 0.88f, 0.46f, 0.9f), 1)
            : MakePanelStyle(new Color(0.19f, 0.12f, 0.075f, 0.92f), new Color(0.74f, 0.52f, 0.24f, 0.7f), 1));
        button.AddThemeStyleboxOverride("hover", primary
            ? MakePanelStyle(new Color(1.0f, 0.81f, 0.42f, 1.0f), new Color(1.0f, 0.94f, 0.56f, 1.0f), 1)
            : MakePanelStyle(new Color(0.26f, 0.16f, 0.09f, 0.96f), new Color(0.9f, 0.66f, 0.3f, 0.86f), 1));
        button.AddThemeStyleboxOverride("pressed", MakePanelStyle(new Color(0.56f, 0.36f, 0.16f, 0.98f), new Color(1.0f, 0.82f, 0.38f, 0.94f), 1));
        return button;
    }

    private static Label MakeEyebrow(string text)
    {
        var label = MakeSmallLabel(text);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.CustomMinimumSize = new Vector2(0, 24);
        label.AddThemeStyleboxOverride("normal", MakePanelStyle(new Color(0.76f, 0.56f, 0.22f, 0.96f), new Color(1.0f, 0.82f, 0.38f, 0.82f), 1));
        return label;
    }

    private static Label MakeTitle(string text)
    {
        var label = new Label
        {
            Text = text,
            MouseFilter = MouseFilterEnum.Ignore,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            ClipText = true
        };
        label.AddThemeFontSizeOverride("font_size", 24);
        label.AddThemeColorOverride("font_color", new Color(1.0f, 0.88f, 0.54f, 1.0f));
        label.AddThemeColorOverride("font_shadow_color", new Color(0.12f, 0.04f, 0.02f, 0.9f));
        label.AddThemeConstantOverride("shadow_offset_x", 1);
        label.AddThemeConstantOverride("shadow_offset_y", 2);
        return label;
    }

    private static Label MakeBody(string text)
    {
        var label = MakeSmallLabel(text);
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        label.ClipText = false;
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
        label.AddThemeColorOverride("font_color", new Color(0.98f, 0.9f, 0.72f, 1.0f));
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

    private static bool HasInput(ModalData data)
    {
        return !string.IsNullOrWhiteSpace(data.InputLabel) || !string.IsNullOrWhiteSpace(data.InputText);
    }

    private static string ReadPreviewKey()
    {
        var value = OS.GetEnvironment("WUXIA_GODOT_PREVIEW_MODAL").ToLowerInvariant();
        return value switch
        {
            "detail" or "confirm" or "on" or "true" => "detail",
            "import" or "export" or "clear" => value,
            _ => string.Empty
        };
    }

    private static ModalData LoadModal(string resourcePath, string key)
    {
        try
        {
            var absolutePath = ProjectSettings.GlobalizePath(resourcePath);
            if (!File.Exists(absolutePath))
            {
                return ModalData.Fallback;
            }

            using var document = JsonDocument.Parse(File.ReadAllText(absolutePath));
            var root = document.RootElement;
            if (key is "import" or "export" or "clear"
                && root.TryGetProperty("saveDialogs", out var saveDialogs)
                && saveDialogs.ValueKind == JsonValueKind.Object
                && saveDialogs.TryGetProperty(key, out var saveModal)
                && saveModal.ValueKind == JsonValueKind.Object)
            {
                var data = ModalData.FromJson(saveModal);
                return key == "export" ? data with { InputText = SaveManager.ExportCurrentSaveText() } : data;
            }

            return root.TryGetProperty("modalPreview", out var modal) && modal.ValueKind == JsonValueKind.Object
                ? ModalData.FromJson(modal)
                : ModalData.Fallback;
        }
        catch (Exception ex)
        {
            GD.PushWarning($"GameModalLayer sample load failed: {ex.Message}");
            return ModalData.Fallback;
        }
    }

    public readonly record struct ModalData(
        string Eyebrow,
        string Title,
        string Body,
        string[] Details,
        string InputLabel,
        string InputText,
        bool InputReadOnly,
        bool Danger,
        string Confirm,
        string Cancel)
    {
        public static ModalData Fallback => new(
            "功法详情",
            "无忌心经 · 内功常驻",
            "以内息温养经脉，战斗中持续提升内功、防御和气血上限。",
            new[] { "突破进度：8 / 10 重", "当前加成：内功 +108，防御 +35，气血 +24" },
            string.Empty,
            string.Empty,
            false,
            false,
            "确认",
            "关闭");

        public static ModalData FromJson(JsonElement json)
        {
            return new ModalData(
                ReadString(json, "eyebrow", Fallback.Eyebrow),
                ReadString(json, "title", Fallback.Title),
                ReadString(json, "body", Fallback.Body),
                ReadStringArray(json, "details", Fallback.Details),
                ReadString(json, "inputLabel", string.Empty),
                ReadString(json, "inputText", string.Empty),
                ReadBool(json, "inputReadOnly", false),
                ReadBool(json, "danger", false),
                ReadString(json, "confirm", Fallback.Confirm),
                ReadString(json, "cancel", Fallback.Cancel));
        }
    }

    private static string ReadString(JsonElement json, string propertyName, string fallback)
    {
        return json.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? fallback
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

    private static bool ReadBool(JsonElement json, string propertyName, bool fallback)
    {
        return json.TryGetProperty(propertyName, out var property) && property.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? property.GetBoolean()
            : fallback;
    }
}
