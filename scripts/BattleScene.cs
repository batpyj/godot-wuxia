using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Godot;

public partial class BattleScene : Node2D
{
    private const int ViewportWidth = 1600;
    private const int ViewportHeight = 900;
    private static readonly Vector2 HeroFeetPosition = new(430, 790);
    private static readonly Vector2 EnemyFeetPosition = new(1170, 790);
    private const string PreviewImportSaveTextEnv = "WUXIA_GODOT_PREVIEW_IMPORT_SAVE_TEXT";
    private const string PreviewImportSavePathEnv = "WUXIA_GODOT_PREVIEW_IMPORT_SAVE_PATH";
    private const string PreviewClearSaveEnv = "WUXIA_GODOT_PREVIEW_CLEAR_SAVE";
    private const string PreviewCommandsEnv = "WUXIA_GODOT_PREVIEW_COMMANDS";
    private const string StateDumpPathEnv = "WUXIA_GODOT_STATE_DUMP_PATH";
    private const string TimelineDumpPathEnv = "WUXIA_GODOT_TIMELINE_DUMP_PATH";
    private const double AutoCombatIntervalSeconds = 1.25;

    private HeroRig? _heroRig;
    private EnemyRig? _enemyRig;
    private GameState? _lastFeedbackState;
    private bool _suppressStateFeedback;
    private double _autoCombatElapsed;
    private readonly CombatPresentationTimeline _combatTimeline = new();

    public override void _Ready()
    {
        Name = "BattleScene";
        _combatTimeline.StepStarted += PlayTimelinePhaseFeedback;
        SaveManager.StateChanged += OnGameStateChanged;
        BuildStaticBattleView();
        CaptureScreenshotIfRequested();
        CaptureSequenceIfRequested();
    }

    public override void _Process(double delta)
    {
        AdvanceCombatTimeline(delta);

        var state = SaveManager.CurrentState;
        if (!state.Commands.AutoExplore || state.Commands.IsPaused)
        {
            _autoCombatElapsed = 0;
            return;
        }

        if (_combatTimeline.IsActive)
        {
            _autoCombatElapsed = 0;
            return;
        }

        _autoCombatElapsed += delta;
        if (_autoCombatElapsed < AutoCombatIntervalSeconds)
        {
            return;
        }

        _autoCombatElapsed = 0;
        var result = GameCommands.Execute("combat_tick");
        if (!result.Ok)
        {
            GD.PushWarning($"Auto combat tick failed: {result.Message}");
        }
    }

    public override void _ExitTree()
    {
        _combatTimeline.StepStarted -= PlayTimelinePhaseFeedback;
        SaveManager.StateChanged -= OnGameStateChanged;
    }

    private void BuildStaticBattleView()
    {
        AddBackdrop();

        AddHeroRig();
        var useBoss = ShouldPreviewBoss();
        AddEnemyRig(useBoss);
        AddPixelFxPreview(useBoss);
        AddCombatStatusBar();
        AddCharacterInfoPanel();
        AddRightInfoPanel();
        AddBottomQuickBar();
        AddSaveActionBar();
        AddGameModalLayer();
        UpdateHeroWeapon(SaveManager.CurrentState);
        _lastFeedbackState = SaveManager.CurrentState;
        DumpTimelineIfRequested();
        CallDeferred(nameof(ApplySavePreviewAction));
    }

    public void ApplySavePreviewAction()
    {
        if (ReadTruthyEnvironment(PreviewClearSaveEnv))
        {
            _suppressStateFeedback = true;
            try
            {
                SaveManager.ClearLocalSave();
            }
            finally
            {
                _suppressStateFeedback = false;
                _lastFeedbackState = SaveManager.CurrentState;
            }

            GD.Print("Preview save action: cleared local save.");
            DumpStateIfRequested();
            return;
        }

        var importText = ReadPreviewImportText();
        if (string.IsNullOrWhiteSpace(importText))
        {
            ApplyPreviewCommands();
            DumpStateIfRequested();
            return;
        }

        _suppressStateFeedback = true;
        var result = SaveManager.TryImportSaveText(importText);
        _suppressStateFeedback = false;
        _lastFeedbackState = SaveManager.CurrentState;
        if (result.Ok)
        {
            GD.Print($"Preview save action: imported {SaveManager.DescribeCurrentState()}.");
            UpdateHeroWeapon(SaveManager.CurrentState);
            ApplyPreviewCommands();
        }
        else
        {
            GD.PushWarning($"Preview save action import failed: {result.Message}");
        }

        DumpStateIfRequested();
    }

    private void AddHeroRig()
    {
        var heroScene = LoadScene("res://scenes/actors/HeroRig.tscn");
        var hero = heroScene.Instantiate<HeroRig>();
        hero.Name = "Hero";
        hero.Position = HeroFeetPosition;
        hero.Scale = new Vector2(0.23f, 0.23f);
        hero.ZIndex = 10;
        AddChild(hero);
        _heroRig = hero;
    }

    private void AddEnemyRig(bool useBoss)
    {
        var enemyScene = LoadScene("res://scenes/actors/EnemyRig.tscn");
        var enemy = enemyScene.Instantiate<EnemyRig>();
        var profile = CombatPresentationCatalog.GetEnemyVisualProfile(SaveManager.CurrentState, useBoss);
        enemy.Name = useBoss ? "Boss" : "Enemy";
        enemy.ActorRole = useBoss ? "boss" : "enemy";
        enemy.BodyTexturePath = profile.BodyTexturePath;
        enemy.WeaponTexturePath = profile.WeaponTexturePath;
        enemy.FeetAnchorPixels = profile.FeetAnchorPixels;
        enemy.WeaponSocketLocalFromFeet = profile.WeaponSocketLocalFromFeet;
        enemy.WeaponScale = profile.WeaponScale;
        enemy.IsBoss = profile.IsBoss;
        enemy.Position = EnemyFeetPosition;
        enemy.Scale = new Vector2(-profile.ActorScale, profile.ActorScale);
        enemy.ZIndex = profile.IsBoss ? 11 : 10;
        AddChild(enemy);
        _enemyRig = enemy;
    }

    private void UpdateEnemyProfile(GameState state, bool playSpawn)
    {
        if (_enemyRig == null)
        {
            return;
        }

        var profile = CombatPresentationCatalog.GetEnemyVisualProfile(state, _enemyRig.ActorRole == "boss");
        _enemyRig.Scale = new Vector2(-profile.ActorScale, profile.ActorScale);
        _enemyRig.ZIndex = profile.IsBoss ? 11 : 10;
        _enemyRig.SetProfile(
            profile.BodyTexturePath,
            profile.WeaponTexturePath,
            profile.FeetAnchorPixels,
            profile.WeaponSocketLocalFromFeet,
            profile.WeaponScale,
            profile.IsBoss);

        if (playSpawn)
        {
            _enemyRig.PlaySpawn();
        }
        else
        {
            _enemyRig.PlayIdle();
        }
    }

    private void AddBackdrop()
    {
        var fallback = new ColorRect
        {
            Name = "FallbackBackground",
            Color = new Color(0.08f, 0.07f, 0.055f, 1.0f),
            Size = new Vector2(ViewportWidth, ViewportHeight)
        };
        AddChild(fallback);

        var background = new Sprite2D
        {
            Name = "CombatBackground",
            Texture = LoadTexture("res://assets/backgrounds/bg_jianghu_combat_grounded_v7.png"),
            Centered = true,
            Position = new Vector2(ViewportWidth / 2.0f, ViewportHeight / 2.0f),
            Scale = new Vector2(0.9621f, 0.9621f),
            ZIndex = 0,
            TextureFilter = TextureFilterEnum.Nearest
        };
        AddChild(background);
    }

    private void AddPixelFxPreview(bool useBoss)
    {
        var fxName = OS.GetEnvironment("WUXIA_GODOT_PREVIEW_FX").ToLowerInvariant();
        if (fxName == "none")
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(fxName))
        {
            fxName = InferPreviewFx(OS.GetEnvironment("WUXIA_GODOT_PREVIEW_ACTION"));
        }

        if (string.IsNullOrWhiteSpace(fxName))
        {
            return;
        }

        if (fxName == "all")
        {
            AddPixelFxPlayer("WeaponSlashFx").PlayWeaponSlash(new Vector2(780, 565), facingRight: true);
            AddPixelFxPlayer("HitSparkFx").PlayHitSpark(EnemyFeetPosition + new Vector2(useBoss ? -18 : -22, -250));
            AddPixelFxPlayer("HeavyImpactFx").PlayHeavyImpact(EnemyFeetPosition + new Vector2(useBoss ? -18 : -22, -245));
            AddPixelFxPlayer("InnerGuardFx").PlayInnerGuard(HeroFeetPosition + new Vector2(0, -300));
            return;
        }

        if (fxName is "themes" or "theme_all")
        {
            AddPixelFxPlayer("ShaolinGoldBodyFx").PlayShaolinGoldBody(HeroFeetPosition + new Vector2(-35, -300));
            AddPixelFxPlayer("TaijiSpiralFx").PlayTaijiSpiral(HeroFeetPosition + new Vector2(70, -295));
            AddPixelFxPlayer("SixMeridianSwordFx").PlaySixMeridianSword(new Vector2(780, 540), facingRight: true);
            AddPixelFxPlayer("AncientTombMoonFx").PlayAncientTombMoon(EnemyFeetPosition + new Vector2(useBoss ? -24 : -34, -330));
            AddPixelFxPlayer("RomanceAssistFx").PlayRomanceAssist(HeroFeetPosition + new Vector2(160, -285));
            return;
        }

        AddPixelFxPlayer("PixelFxPreview").PlayPreview(fxName, HeroFeetPosition, EnemyFeetPosition, useBoss);
    }

    private PixelFxPlayer AddPixelFxPlayer(string name)
    {
        var fxScene = LoadScene("res://scenes/fx/PixelFxPlayer.tscn");
        var fx = fxScene.Instantiate<PixelFxPlayer>();
        fx.Name = name;
        fx.ZIndex = 80;
        AddChild(fx);
        return fx;
    }

    private void AddCombatStatusBar()
    {
        var statusScene = LoadScene("res://scenes/ui/CombatStatusBar.tscn");
        var statusBar = statusScene.Instantiate<CombatStatusBar>();
        AddChild(statusBar);
    }

    private void AddCharacterInfoPanel()
    {
        var panelScene = LoadScene("res://scenes/ui/CharacterInfoPanel.tscn");
        var panel = panelScene.Instantiate<CharacterInfoPanel>();
        AddChild(panel);
    }

    private void AddRightInfoPanel()
    {
        var panelScene = LoadScene("res://scenes/ui/RightInfoPanel.tscn");
        var panel = panelScene.Instantiate<RightInfoPanel>();
        AddChild(panel);
    }

    private void AddBottomQuickBar()
    {
        var panelScene = LoadScene("res://scenes/ui/BottomQuickBar.tscn");
        var panel = panelScene.Instantiate<BottomQuickBar>();
        AddChild(panel);
    }

    private void AddSaveActionBar()
    {
        var panelScene = LoadScene("res://scenes/ui/SaveActionBar.tscn");
        var panel = panelScene.Instantiate<SaveActionBar>();
        AddChild(panel);
    }

    private void AddGameModalLayer()
    {
        var modalScene = LoadScene("res://scenes/ui/GameModalLayer.tscn");
        var modal = modalScene.Instantiate<GameModalLayer>();
        AddChild(modal);
    }

    private static bool ShouldPreviewBoss()
    {
        return OS.GetEnvironment("WUXIA_GODOT_PREVIEW_TARGET").ToLowerInvariant() == "boss";
    }

    private static string InferPreviewFx(string action)
    {
        return action.ToLowerInvariant() switch
        {
            "attack" => "weapon_slash",
            "graze" => "hit_spark",
            "hit" => "heavy_impact",
            "meditate" => "inner_guard",
            _ => string.Empty
        };
    }

    private static Texture2D LoadTexture(string path)
    {
        var texture = GD.Load<Texture2D>(path);
        if (texture == null)
        {
            throw new InvalidOperationException($"Texture not found: {path}");
        }

        return texture;
    }

    private static PackedScene LoadScene(string path)
    {
        var scene = GD.Load<PackedScene>(path);
        if (scene == null)
        {
            throw new InvalidOperationException($"Scene not found: {path}");
        }

        return scene;
    }

    private async void CaptureScreenshotIfRequested()
    {
        if (!OS.HasEnvironment("WUXIA_GODOT_CAPTURE_PATH"))
        {
            return;
        }

        var captureAfterFrames = ReadPositiveIntEnvironment("WUXIA_GODOT_CAPTURE_AFTER_FRAMES", 2);
        for (var frame = 0; frame < captureAfterFrames; frame++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }

        var outputPath = OS.GetEnvironment("WUXIA_GODOT_CAPTURE_PATH");
        var absolutePath = ToAbsoluteOutputPath(outputPath);

        var outputDirectory = Path.GetDirectoryName(absolutePath);
        if (!string.IsNullOrEmpty(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        if (!TrySaveViewportPng(absolutePath))
        {
            GetTree().Quit(1);
            return;
        }

        DumpTimelineIfRequested();
        GD.Print($"Saved screenshot: {absolutePath}");
        GetTree().Quit(0);
    }

    private async void CaptureSequenceIfRequested()
    {
        if (!OS.HasEnvironment("WUXIA_GODOT_CAPTURE_SEQUENCE_DIR"))
        {
            return;
        }

        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        var outputDirectory = ToAbsoluteOutputPath(OS.GetEnvironment("WUXIA_GODOT_CAPTURE_SEQUENCE_DIR"));
        Directory.CreateDirectory(outputDirectory);

        var frameCount = ReadPositiveIntEnvironment("WUXIA_GODOT_CAPTURE_FRAMES", 48);
        var frameStep = ReadPositiveIntEnvironment("WUXIA_GODOT_CAPTURE_EVERY", 1);

        for (var frame = 0; frame < frameCount; frame++)
        {
            for (var step = 0; step < frameStep; step++)
            {
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }

            var outputPath = Path.Combine(outputDirectory, $"frame_{frame:0000}.png");
            if (!TrySaveViewportPng(outputPath))
            {
                GetTree().Quit(1);
                return;
            }
        }

        GD.Print($"Saved frame sequence: {outputDirectory}");
        GetTree().Quit(0);
    }

    private bool TrySaveViewportPng(string absolutePath)
    {
        var viewportTexture = GetViewport().GetTexture();
        if (viewportTexture == null)
        {
            GD.PushWarning("Screenshot capture skipped: the active display driver did not provide a viewport texture.");
            return false;
        }

        var image = viewportTexture.GetImage();
        if (image == null)
        {
            GD.PushWarning("Screenshot capture skipped: viewport texture did not produce an image.");
            return false;
        }

        var result = image.SavePng(absolutePath);
        if (result != Error.Ok)
        {
            GD.PushError($"Failed to save screenshot {absolutePath}: {result}");
            return false;
        }

        return true;
    }

    private static string ToAbsoluteOutputPath(string outputPath)
    {
        return outputPath.StartsWith("res://", StringComparison.Ordinal)
            || outputPath.StartsWith("user://", StringComparison.Ordinal)
            ? ProjectSettings.GlobalizePath(outputPath)
            : outputPath;
    }

    private static int ReadPositiveIntEnvironment(string name, int fallback)
    {
        if (!OS.HasEnvironment(name))
        {
            return fallback;
        }

        return int.TryParse(OS.GetEnvironment(name), out var value) && value > 0
            ? value
            : fallback;
    }

    private static string ReadPreviewImportText()
    {
        if (OS.HasEnvironment(PreviewImportSaveTextEnv))
        {
            return OS.GetEnvironment(PreviewImportSaveTextEnv);
        }

        if (!OS.HasEnvironment(PreviewImportSavePathEnv))
        {
            return string.Empty;
        }

        var path = ToAbsoluteOutputPath(OS.GetEnvironment(PreviewImportSavePathEnv));
        return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
    }

    private static bool ReadTruthyEnvironment(string name)
    {
        if (!OS.HasEnvironment(name))
        {
            return false;
        }

        var value = OS.GetEnvironment(name).Trim().ToLowerInvariant();
        return value is "1" or "true" or "yes" or "on";
    }

    private void ApplyPreviewCommands()
    {
        if (!OS.HasEnvironment(PreviewCommandsEnv))
        {
            return;
        }

        var commands = OS.GetEnvironment(PreviewCommandsEnv)
            .Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var command in commands)
        {
            var result = GameCommands.Execute(command);
            if (result.Ok)
            {
                GD.Print($"Preview command: {result.Message}");
            }
            else
            {
                GD.PushWarning($"Preview command failed: {result.Message}");
            }
        }
    }

    private void OnGameStateChanged(GameState state)
    {
        var previous = _lastFeedbackState;
        _lastFeedbackState = state;
        DumpStateIfRequested();

        if (_suppressStateFeedback || previous == null)
        {
            return;
        }

        if (IsEnemyAdvanceEvent(state, previous))
        {
            PlayEnemyAdvanceFeedback(state);
            return;
        }

        if (state.Combat.Turn != previous.Combat.Turn)
        {
            PlayCombatSettlementFeedback(state.Combat);
            return;
        }

        if (state.WeaponInfo.Score != previous.WeaponInfo.Score
            || !string.Equals(state.WeaponInfo.Name, previous.WeaponInfo.Name, StringComparison.Ordinal)
            || !string.Equals(state.Hero.Weapon, previous.Hero.Weapon, StringComparison.Ordinal))
        {
            PlayWeaponCommandFeedback(state);
            return;
        }

        if (state.Romance.Favor != previous.Romance.Favor
            || !string.Equals(state.Romance.Status, previous.Romance.Status, StringComparison.Ordinal))
        {
            PlayRomanceCommandFeedback();
            return;
        }

        if (state.Commands.ActiveMartialArtIndex != previous.Commands.ActiveMartialArtIndex)
        {
            PlayMartialCommandFeedback();
            return;
        }

        if (state.Commands.AutoExplore != previous.Commands.AutoExplore
            || state.Commands.IsPaused != previous.Commands.IsPaused)
        {
            PlayAdventureCommandFeedback(state.Commands.AutoExplore);
        }
    }

    private static bool IsEnemyAdvanceEvent(GameState state, GameState previous)
    {
        return string.Equals(state.Combat.LastHitKind, "advance", StringComparison.Ordinal)
            && (state.Stage.Floor != previous.Stage.Floor
                || !string.Equals(state.Enemy.Name, previous.Enemy.Name, StringComparison.Ordinal));
    }

    private void PlayCombatSettlementFeedback(CombatSnapshot combat)
    {
        var timing = CombatPresentationCatalog.GetTiming(_enemyRig?.ActorRole == "boss");
        _heroRig?.PlayAttack();
        AddPixelFxPlayer("CombatWeaponSlashFx").PlayWeaponSlash(new Vector2(790, 560), facingRight: true);

        switch (combat.LastHitKind)
        {
            case "defeat":
                _enemyRig?.PlayDefeat();
                AddPixelFxPlayer("CombatHeavyDefeatFx").PlayHeavyImpact(EnemyFeetPosition + new Vector2(-24, -245));
                BeginCombatTimeline(new CombatTimelineStep("defeat_pause", timing.DefeatPause, combat.Message), new CombatTimelineStep("reward_settlement", timing.RewardSettlement, "reward settlement"));
                break;
            case "hit":
                _enemyRig?.PlayHit();
                AddPixelFxPlayer("CombatHeavyHitFx").PlayHeavyImpact(EnemyFeetPosition + new Vector2(-24, -245));
                BeginCombatTimeline(new CombatTimelineStep("hit_recover", timing.HitRecover, combat.Message), new CombatTimelineStep("next_beat_ready", timing.HitNextBeat, "next combat beat"));
                break;
            case "graze":
                _enemyRig?.PlayGraze();
                AddPixelFxPlayer("CombatGrazeFx").PlayHitSpark(EnemyFeetPosition + new Vector2(-24, -245));
                BeginCombatTimeline(new CombatTimelineStep("graze_recover", timing.GrazeRecover, combat.Message), new CombatTimelineStep("next_beat_ready", timing.GrazeNextBeat, "next combat beat"));
                break;
            default:
                _enemyRig?.PlayGraze();
                AddPixelFxPlayer("CombatFallbackSparkFx").PlayHitSpark(EnemyFeetPosition + new Vector2(-24, -245));
                BeginCombatTimeline(new CombatTimelineStep("combat_recover", timing.CombatRecover, combat.Message), new CombatTimelineStep("next_beat_ready", timing.CombatNextBeat, "next combat beat"));
                break;
        }
    }

    private void PlayEnemyAdvanceFeedback(GameState state)
    {
        _heroRig?.PlayIdle();
        UpdateEnemyProfile(state, playSpawn: true);
        AddPixelFxPlayer("EnemyAdvanceSparkFx").PlayHitSpark(EnemyFeetPosition + new Vector2(-24, -250));
        var timing = CombatPresentationCatalog.GetTiming(_enemyRig?.ActorRole == "boss");
        BeginCombatTimeline(new CombatTimelineStep("enemy_spawn", timing.EnemySpawn, state.Combat.Message), new CombatTimelineStep("next_beat_ready", timing.EnemySpawnNextBeat, "next enemy beat"));
    }

    private void PlayWeaponCommandFeedback(GameState state)
    {
        UpdateHeroWeapon(state);
        _heroRig?.PlayAttack();
        _enemyRig?.PlayGraze();
        AddPixelFxPlayer("CommandWeaponSlashFx").PlayWeaponSlash(new Vector2(790, 560), facingRight: true);
    }

    private void PlayRomanceCommandFeedback()
    {
        _heroRig?.PlayAttack();
        AddPixelFxPlayer("CommandRomanceAssistFx").PlayRomanceAssist(HeroFeetPosition + new Vector2(170, -285));
    }

    private void PlayMartialCommandFeedback()
    {
        _heroRig?.PlayMeditate();
        AddPixelFxPlayer("CommandMartialFx").PlayTaijiSpiral(HeroFeetPosition + new Vector2(72, -295));
    }

    private void PlayAdventureCommandFeedback(bool autoExplore)
    {
        if (autoExplore)
        {
            _heroRig?.PlayAttack();
            _enemyRig?.PlayGraze();
            AddPixelFxPlayer("CommandAutoExploreFx").PlayHitSpark(EnemyFeetPosition + new Vector2(-24, -245));
        }
        else
        {
            _heroRig?.PlayIdle();
        }
    }

    private void UpdateHeroWeapon(GameState state)
    {
        _heroRig?.SetWeaponTexturePath(GetWeaponTexturePath(state));
    }

    private static string GetWeaponTexturePath(GameState state)
    {
        var key = state.Hero.Weapon.Trim().ToLowerInvariant();
        var pathKey = key switch
        {
            "axe" => "axe/axe_basic",
            "blade" => "blade/blade_basic",
            "cane" => "cane/cane_basic",
            "duster" or "whisk" => "duster/duster_basic",
            "fan" => "fan/fan_basic",
            "flute" => "flute/flute_basic",
            "hidden" => "hidden/hidden_basic",
            "spear" => "spear/spear_basic",
            "staff" => "staff/staff_basic",
            _ => "sword/sword_basic"
        };

        return $"res://assets/weapons/{pathKey}.png";
    }

    private static void DumpStateIfRequested()
    {
        if (!OS.HasEnvironment(StateDumpPathEnv))
        {
            return;
        }

        var path = ToAbsoluteOutputPath(OS.GetEnvironment(StateDumpPathEnv));
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, SaveManager.CurrentState.ToStableJson(), new UTF8Encoding(false));
        GD.Print($"Saved state dump: {path}");
    }

    private void BeginCombatTimeline(params CombatTimelineStep[] steps)
    {
        _combatTimeline.Begin(steps);
        DumpTimelineIfRequested();
    }

    private void AdvanceCombatTimeline(double delta)
    {
        _combatTimeline.Advance(delta);
    }

    private void PlayTimelinePhaseFeedback(CombatTimelineStep step)
    {
        if (step.Phase == "reward_settlement")
        {
            PlayRewardSettlementFeedback();
        }
    }

    private void PlayRewardSettlementFeedback()
    {
        var presentation = CombatPresentationCatalog.GetRewardNoticePresentation();
        var rewardEvents = SaveManager.CurrentState.Combat.Events
            .Where(item => item.Kind is "reward" or "drop" or "sect_notice" or "adventure_notice")
            .TakeLast(Math.Max(1, presentation.MaxEvents))
            .ToArray();

        if (rewardEvents.Length == 0)
        {
            return;
        }

        AddPixelFxPlayer("RewardSettlementFx").PlayHitSpark(new Vector2(800, 600));
        for (var index = 0; index < rewardEvents.Length; index++)
        {
            var combatEvent = rewardEvents[index];
            var color = presentation.ColorFor(combatEvent.Kind);
            var position = presentation.BasePosition + new Vector2(0, index * presentation.RowSpacing);
            AddFloatingRewardText(FormatRewardFloatText(combatEvent), position, color, index * 0.08);
        }
    }

    private void AddFloatingRewardText(string text, Vector2 position, Color color, double delay)
    {
        var label = new Label
        {
            Text = text,
            Position = position,
            Size = new Vector2(360, 30),
            ZIndex = 145,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Modulate = color
        };
        label.AddThemeFontSizeOverride("font_size", 18);
        label.AddThemeColorOverride("font_shadow_color", new Color(0.06f, 0.035f, 0.02f, 0.9f));
        label.AddThemeConstantOverride("shadow_offset_x", 2);
        label.AddThemeConstantOverride("shadow_offset_y", 2);
        AddChild(label);

        var tween = CreateTween();
        if (delay > 0)
        {
            tween.TweenInterval(delay);
        }

        var fadeColor = new Color(color.R, color.G, color.B, 0.0f);
        tween.TweenProperty(label, "position", position + new Vector2(0, -46), 0.85)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        tween.Parallel().TweenProperty(label, "modulate", fadeColor, 0.85);
        tween.TweenCallback(Callable.From(label.QueueFree));
    }

    private static string FormatRewardFloatText(CombatEventSnapshot combatEvent)
    {
        return combatEvent.Kind switch
        {
            "reward" => combatEvent.Message,
            "drop" => combatEvent.Message,
            "sect_notice" => combatEvent.Message,
            "adventure_notice" => combatEvent.Message,
            _ => combatEvent.Message
        };
    }

    private void DumpTimelineIfRequested()
    {
        if (!OS.HasEnvironment(TimelineDumpPathEnv))
        {
            return;
        }

        var state = SaveManager.CurrentState;
        var path = ToAbsoluteOutputPath(OS.GetEnvironment(TimelineDumpPathEnv));
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var profile = CombatPresentationCatalog.GetEnemyVisualProfile(state, _enemyRig?.ActorRole == "boss");
        var dump = _combatTimeline.CreateDump(state, AutoCombatIntervalSeconds, profile);
        var json = JsonSerializer.Serialize(dump, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });
        File.WriteAllText(path, json, new UTF8Encoding(false));
    }
}
