using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Godot;

public static class SaveManager
{
    public const string SaveTextPrefix = "WUXIA_SAVE_V15G0";
    private const string SamplePath = "res://data/combat-sample.json";
    private const string LocalSavePath = "user://wuxia-save-v15g0.json";

    private static GameState? _currentState;

    public static event Action<GameState>? StateChanged;

    public static GameState CurrentState => _currentState ??= LoadCurrentState();

    public static GameState LoadCurrentState()
    {
        var localPath = ProjectSettings.GlobalizePath(LocalSavePath);
        if (File.Exists(localPath))
        {
            try
            {
                var localState = GameState.FromStableJson(File.ReadAllText(localPath, Encoding.UTF8));
                if (localState.IsStructurallyValid(out _))
                {
                    _currentState = localState;
                    return localState;
                }
            }
            catch (Exception ex)
            {
                GD.PushWarning($"Local save ignored: {ex.Message}");
            }
        }

        _currentState = LoadSampleState();
        return _currentState;
    }

    public static GameState LoadSampleState()
    {
        var samplePath = ProjectSettings.GlobalizePath(SamplePath);
        return GameState.FromCombatSampleJson(File.ReadAllText(samplePath, Encoding.UTF8));
    }

    public static string ExportCurrentSaveText()
    {
        return EncodeSaveText(CurrentState);
    }

    public static string EncodeSaveText(GameState state)
    {
        var json = state.ToStableJson();
        var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        var checksum = ComputeChecksum(payload);
        return $"{SaveTextPrefix}::{payload}::{checksum}";
    }

    public static ImportResult TryImportSaveText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return ImportResult.Fail("存档文本为空");
        }

        var parts = text.Trim().Split("::", StringSplitOptions.None);
        if (parts.Length != 3 || parts[0] != SaveTextPrefix)
        {
            return ImportResult.Fail("存档格式不正确");
        }

        var payload = parts[1];
        var checksum = parts[2];
        var expected = ComputeChecksum(payload);
        if (!string.Equals(checksum, expected, StringComparison.OrdinalIgnoreCase))
        {
            return ImportResult.Fail("存档校验失败");
        }

        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            var state = GameState.FromStableJson(json);
            if (!state.IsStructurallyValid(out var error))
            {
                return ImportResult.Fail(error);
            }

            SaveState(state);
            return ImportResult.Success(state);
        }
        catch (Exception ex)
        {
            return ImportResult.Fail($"存档解析失败：{ex.Message}");
        }
    }

    public static void SaveState(GameState state)
    {
        var localPath = ProjectSettings.GlobalizePath(LocalSavePath);
        var directory = Path.GetDirectoryName(localPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(localPath, state.ToStableJson(), new UTF8Encoding(false));
        _currentState = state;
        StateChanged?.Invoke(state);
    }

    public static void ClearLocalSave()
    {
        var localPath = ProjectSettings.GlobalizePath(LocalSavePath);
        if (File.Exists(localPath))
        {
            File.Delete(localPath);
        }

        _currentState = LoadSampleState();
        StateChanged?.Invoke(_currentState);
    }

    public static string DescribeCurrentState()
    {
        var state = CurrentState;
        return $"{state.Hero.Name} · {state.Hero.Sect} · 第{state.Stage.Floor}层 · 战力 {state.Stage.CombatPower}";
    }

    public static string DescribeWeaponType(string weapon)
    {
        if (string.IsNullOrWhiteSpace(weapon))
        {
            return "徒手";
        }

        return weapon.Trim().ToLowerInvariant() switch
        {
            "sword" => "剑",
            "blade" => "刀",
            "staff" => "棍",
            "spear" => "枪",
            "hidden" => "暗器",
            "whisk" => "拂尘",
            "cane" => "杖",
            "fan" => "扇",
            "flute" => "笛",
            "axe" => "斧",
            _ => weapon
        };
    }

    private static string ComputeChecksum(string payload)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{SaveTextPrefix}:{payload}"));
        return Convert.ToHexString(bytes)[..16].ToLowerInvariant();
    }
}

public readonly record struct ImportResult(bool Ok, string Message, GameState? State)
{
    public static ImportResult Success(GameState state)
    {
        return new ImportResult(true, "导入成功", state);
    }

    public static ImportResult Fail(string message)
    {
        return new ImportResult(false, message, null);
    }
}
