# Wuxia Godot Migration

This repo is the Godogen/Codex Godot migration workspace for the wuxia idle game.

## Current Setup

- Godogen runtime: installed.
- Codex skill: `.agents/skills/asset-gen`.
- Engine guide: `godot.md`.
- Godot executable: configured through `WUXIA_GODOT_ROOT`.
- Current Godot version: `4.7.stable.mono.official.5b4e0cb0f`.
- .NET SDK: `9.0.315`.
- ffmpeg: `8.1.2`.

## Local Tools

```powershell
powershell -ExecutionPolicy Bypass -File tools\verify-godot.ps1
```

This verifies Godot, `.NET SDK`, and `ffmpeg`.

Asset-generation API keys are optional for this project. To check them explicitly:

```powershell
powershell -ExecutionPolicy Bypass -File tools\verify-godot.ps1 -CheckAssetKeys
```

To open the Godot editor after a `project.godot` exists:

```powershell
. .\tools\godot-env.ps1
Open-WuxiaGodotEditor
```

## Current Notes

- `GOOGLE_API_KEY`, `XAI_API_KEY`, and `TRIPO3D_API_KEY` are not configured by choice. Paid `$asset-gen` calls are not part of the current workflow.
- The migration can start with local assets from the existing web game and Godot runtime code.
- Text files are standardized as UTF-8 with LF line endings through `.editorconfig` and `.gitattributes`.
- Encoding check: `powershell -ExecutionPolicy Bypass -File tools\verify-utf8.ps1`

## Migration Plan

- Animation migration plan: `docs/godot-animation-migration-plan.md`
- V13G.1 source manifest: `data/asset-sources.json`
- V13G.1 art rig seed data: `data/art-rig.json`
- V13G.2 static battle scene: `scenes/BattleScene.tscn`
- V13G.3 hero rig: `scenes/actors/HeroRig.tscn`
- V13G.4 runtime idle animation: `scripts/HeroRig.cs`
- V13G.5 layered hero manifest: `assets/actors/hero/layers/v13g5/layers-manifest.json`
- V13G.6 layered idle preview: `screenshots/v13g6-layered-idle-preview.gif`
- V13G.7 action previews: `screenshots/v13g7-attack-preview.gif`, `screenshots/v13g7-graze-preview.gif`, `screenshots/v13g7-hit-preview.gif`
- V13G.8 action previews: `screenshots/v13g8-defeat-preview.gif`, `screenshots/v13g8-meditate-preview.gif`
- V13G.9 enemy rig: `scenes/actors/EnemyRig.tscn`, `scripts/EnemyRig.cs`
- V13G.9 enemy/Boss previews: `screenshots/v13g9-enemy-attack-preview.gif`, `screenshots/v13g9-enemy-hit-preview.gif`, `screenshots/v13g9-boss-defeat-preview.gif`
- V13G.10.1 pixel FX pipeline: `scenes/fx/PixelFxPlayer.tscn`, `scripts/PixelFxPlayer.cs`, `data/fx-catalog.json`
- V13G.10.1 FX previews: `screenshots/v13g10-fx-all-preview.gif`, `screenshots/v13g10-weapon-slash-preview.gif`, `screenshots/v13g10-inner-guard-preview.gif`
- V13G.10.2 theme FX: `shaolin_gold_body`, `taiji_spiral`, `six_meridian_sword`, `ancient_tomb_moon`, `romance_assist`
- V13G.10.2 theme previews: `screenshots/v13g10-2-theme-fx-all-preview.gif`, `screenshots/v13g10-2-six-meridian-preview.gif`, `screenshots/v13g10-2-ancient-tomb-moon-preview.gif`, `screenshots/v13g10-2-romance-assist-preview.gif`
- V14G.1 combat status bar: `scenes/ui/CombatStatusBar.tscn`, `scripts/CombatStatusBar.cs`
- V14G.1 UI previews: `screenshots/v14g1-combat-status-bar.png`, `screenshots/v14g1-combat-status-bar-preview.gif`
- V14G.2 character info panel: `scenes/ui/CharacterInfoPanel.tscn`, `scripts/CharacterInfoPanel.cs`
- V14G.2 UI preview: `screenshots/v14g2-character-info-panel.png`
- V14G.3 right info panel: `scenes/ui/RightInfoPanel.tscn`, `scripts/RightInfoPanel.cs`
- V14G.3 UI preview: `screenshots/v14g3-right-info-panel.png`
- V14G.4 bottom quick bar: `scenes/ui/BottomQuickBar.tscn`, `scripts/BottomQuickBar.cs`
- V14G.4 UI preview: `screenshots/v14g4-bottom-quick-bar.png`
- V14G.5 game modal layer: `scenes/ui/GameModalLayer.tscn`, `scripts/GameModalLayer.cs`
- V14G.5 UI preview: `screenshots/v14g5-game-modal-layer.png`
- V14G.6 save action bar: `scenes/ui/SaveActionBar.tscn`, `scripts/SaveActionBar.cs`
- V14G.6 save previews: `screenshots/v14g6-save-import-modal.png`, `screenshots/v14g6-save-export-modal.png`, `screenshots/v14g6-save-clear-modal.png`
- V15G.0 state/save manager: `scripts/GameState.cs`, `scripts/SaveManager.cs`
- V15G.0 save manager preview: `screenshots/v15g0-save-manager-export.png`
- V15G.1 GameState UI refresh: `scripts/CombatStatusBar.cs`, `scripts/CharacterInfoPanel.cs`, `scripts/RightInfoPanel.cs`, `scripts/BottomQuickBar.cs`
- V15G.1 UI preview: `screenshots/v15g1-game-state-panels.png`
- V15G.2 expanded save state and refresh verification: `scripts/GameState.cs`, `tools/verify-save-refresh.ps1`
- V15G.2 refresh previews: `screenshots/v15g2-import-refresh.png`, `screenshots/v15g2-legacy-import-refresh.png`
- V15G.3 state command layer: `scripts/GameCommands.cs`, `tools/verify-commands.ps1`
- V15G.3 command preview: `screenshots/v15g3-command-preview.png`
- V15G.4 state dump/assertions and web-save bridge: `tools/verify-state-dump.ps1`, `tools/convert-web-save.ps1`, `tools/verify-web-save-bridge.ps1`
- V15G.4 command animation/FX preview: `screenshots/v15g4-command-fx-preview.png`
- V15G.5 combat tick settlement and web-save field report: `scripts/GameCommands.cs`, `tools/analyze-web-save-fields.ps1`, `tools/verify-web-save-field-report.ps1`
- V15G.5 command preview: `screenshots/v15g5-combat-tick-preview.png`
- V15G.6 auto combat loop, combat event log, and migration warnings: `scripts/BattleScene.cs`, `scripts/RightInfoPanel.cs`, `tools/verify-auto-combat.ps1`
- V15G.6 command preview: `screenshots/v15g6-combat-event-preview.png`
- V15G.6 web migration warning preview: `screenshots/v15g6-web-migration-warning.png`
- V15G.7 structured combat event queue and floor advance: `scripts/GameState.cs`, `scripts/GameCommands.cs`, `tools/verify-floor-advance-preview.ps1`
- V15G.7 event queue preview: `screenshots/v15g7-combat-event-queue-preview.png`
- V15G.7 floor advance preview: `screenshots/v15g7-floor-advance-preview.png`
- V15G.8 enemy visual refresh and spawn timing: `scripts/BattleScene.cs`, `scripts/EnemyRig.cs`, `tools/verify-enemy-visual-refresh.ps1`
- V15G.8 enemy visual refresh preview: `screenshots/v15g8-enemy-visual-refresh-preview.png`
- V15G.9 combat timeline gating: `scripts/BattleScene.cs`, `tools/verify-combat-timeline.ps1`
- V15G.9 combat timeline preview: `screenshots/v15g9-combat-timeline-preview.png`
- V15G.10 reward settlement, drops, notices, and combat-event UI: `scripts/GameCommands.cs`, `scripts/BattleScene.cs`, `scripts/RightInfoPanel.cs`, `tools/verify-reward-settlement.ps1`
- V15G.10 reward settlement preview: `screenshots/v15g10-reward-settlement-preview.png`
- V15G.11 timeline service and reward rules table: `scripts/CombatPresentationTimeline.cs`, `scripts/RewardRuleCatalog.cs`, `data/reward-rules.json`, `tools/verify-reward-rules-config.ps1`
- V15G.11 reward rules preview: `screenshots/v15g11-reward-rules-preview.png`
- V15G.12 combat presentation rules table: `scripts/CombatPresentationCatalog.cs`, `data/combat-presentation-rules.json`, `tools/verify-combat-presentation-rules.ps1`
- V15G.12 combat presentation preview: `screenshots/v15g12-combat-presentation-rules-preview.png`

Preview targeting:

```powershell
$env:WUXIA_GODOT_PREVIEW_TARGET = "enemy" # hero, enemy, or boss
$env:WUXIA_GODOT_PREVIEW_ACTION = "attack" # attack, graze, hit, defeat, meditate for hero only
$env:WUXIA_GODOT_PREVIEW_FX = "weapon_slash" # weapon_slash, hit_spark, heavy_impact, inner_guard, shaolin_gold_body, taiji_spiral, six_meridian_sword, ancient_tomb_moon, romance_assist, all, themes, or none
$env:WUXIA_GODOT_PREVIEW_MODAL = "detail" # detail, import, export, clear, confirm, on, true, or unset for hidden
```

## Next Step

Continue with `V15G.13`: extract the temporary floating reward text into a reusable presentation layer and prepare boss/sect/adventure-specific combat presentation hooks.
