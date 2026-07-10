using System;
using System.Linq;

public static class GameCommands
{
    public static CommandResult Execute(string key)
    {
        return key.Trim().ToLowerInvariant() switch
        {
            "toggle_pause" or "pause" => TogglePause(),
            "toggle_auto_explore" or "auto" => ToggleAutoExplore(),
            "next_martial_art" or "martial" => SelectNextMartialArt(),
            "trigger_romance_assist" or "romance" => TriggerRomanceAssist(),
            "cycle_weapon" or "weapon" => CycleWeapon(),
            "resolve_combat_tick" or "combat_tick" or "tick" => ResolveCombatTick(),
            _ => CommandResult.Fail($"未知命令：{key}")
        };
    }

    public static CommandResult TogglePause()
    {
        var state = SaveManager.CurrentState;
        var paused = !state.Commands.IsPaused;
        var next = state with
        {
            Commands = state.Commands with
            {
                IsPaused = paused,
                AutoExplore = paused ? false : state.Commands.AutoExplore,
                LastCommand = paused ? "已暂停闯荡" : "已继续闯荡"
            }
        };

        SaveManager.SaveState(next);
        return CommandResult.Success(next.Commands.LastCommand, next);
    }

    public static CommandResult ToggleAutoExplore()
    {
        var state = SaveManager.CurrentState;
        var autoExplore = !state.Commands.AutoExplore;
        var next = state with
        {
            Commands = state.Commands with
            {
                IsPaused = !autoExplore,
                AutoExplore = autoExplore,
                LastCommand = autoExplore ? "自动闯荡已开启" : "自动闯荡已关闭"
            }
        };

        SaveManager.SaveState(next);
        return CommandResult.Success(next.Commands.LastCommand, next);
    }

    public static CommandResult SelectNextMartialArt()
    {
        var state = SaveManager.CurrentState;
        if (state.MartialArts.Length <= 0)
        {
            return CommandResult.Fail("没有可查看的功法");
        }

        var activeIndex = (state.Commands.ActiveMartialArtIndex + 1) % state.MartialArts.Length;
        var activeArt = state.MartialArts[activeIndex];
        var next = state with
        {
            Commands = state.Commands with
            {
                ActiveMartialArtIndex = activeIndex,
                LastCommand = $"查看功法：{activeArt.Name}"
            }
        };

        SaveManager.SaveState(next);
        return CommandResult.Success(next.Commands.LastCommand, next);
    }

    public static CommandResult TriggerRomanceAssist()
    {
        var state = SaveManager.CurrentState;
        var nextRomance = state.Romance with
        {
            Favor = Math.Min(99, state.Romance.Favor + 1),
            Status = "同心援手已触发",
            Badge = "援"
        };
        var next = state with
        {
            Romance = nextRomance,
            Commands = state.Commands with
            {
                LastCommand = $"情缘援手：{nextRomance.Name}"
            }
        };

        SaveManager.SaveState(next);
        return CommandResult.Success(next.Commands.LastCommand, next);
    }

    public static CommandResult CycleWeapon()
    {
        var state = SaveManager.CurrentState;
        var nextWeapon = PickNextWeapon(state.WeaponInfo.Name);
        var next = state with
        {
            Hero = state.Hero with
            {
                Weapon = nextWeapon.WeaponKey,
                ComprehensivePower = Math.Max(state.Hero.ComprehensivePower, 17000 + nextWeapon.Score)
            },
            WeaponInfo = new WeaponSnapshot(
                "武器",
                nextWeapon.Name,
                nextWeapon.Type,
                nextWeapon.Score,
                "独立挂点 · 手部跟随",
                nextWeapon.Effects),
            Commands = state.Commands with
            {
                LastCommand = $"切换武器：{nextWeapon.Name}"
            }
        };

        SaveManager.SaveState(next);
        return CommandResult.Success(next.Commands.LastCommand, next);
    }

    public static CommandResult ResolveCombatTick()
    {
        var state = SaveManager.CurrentState;
        if (state.Enemy.Hp <= 0 || state.Combat.EnemyDefeated)
        {
            return AdvanceToNextEnemy(state);
        }

        var activeArtIndex = Math.Clamp(state.Commands.ActiveMartialArtIndex, 0, Math.Max(0, state.MartialArts.Length - 1));
        var activeArt = state.MartialArts.Length > 0
            ? state.MartialArts[activeArtIndex]
            : new MartialArtSnapshot("基础拳脚", "拳脚", "入门", "基础攻击");

        var damage = CalculateHeroDamage(state, activeArtIndex);
        var enemyHp = Math.Max(0, state.Enemy.Hp - damage);
        var damageRatio = state.Enemy.MaxHp <= 0 ? 0 : Math.Round((double)damage / state.Enemy.MaxHp, 3);
        var enemyDefeated = enemyHp <= 0;
        var hitKind = enemyDefeated
            ? "defeat"
            : damageRatio >= 0.15
                ? "hit"
                : "graze";
        var hitText = hitKind switch
        {
            "defeat" => "击败",
            "hit" => "重击",
            _ => "擦伤"
        };
        var turn = state.Combat.Turn + 1;
        var message = enemyDefeated
            ? $"第{turn}回合 · {activeArt.Name} 造成 {damage} 点伤害 · 击败敌人"
            : $"第{turn}回合 · {activeArt.Name} 造成 {damage} 点伤害 · {hitText}";
        var events = AppendCombatEvents(
            state.Combat.Events,
            new CombatEventSnapshot(turn, hitKind, state.Hero.Name, state.Enemy.Name, damage, message));
        var eventLog = AppendCombatEvent(state.Combat.EventLog, message);
        var nextHero = state.Hero;
        var commandMessage = message;

        if (enemyDefeated)
        {
            var reward = RewardRuleCatalog.Calculate(state, turn, damage);
            var defeatMessage = $"第{turn}回合 · {state.Enemy.Name} 倒下";
            events = AppendCombatEvents(
                events,
                new CombatEventSnapshot(turn, "defeat", state.Enemy.Name, "stage", 0, defeatMessage),
                new CombatEventSnapshot(turn, "reward", "system", state.Hero.Name, reward.Silver, reward.RewardMessage),
                new CombatEventSnapshot(turn, "drop", state.Enemy.Name, state.Hero.Name, reward.Essence, reward.DropMessage),
                new CombatEventSnapshot(turn, "sect_notice", state.Hero.Sect, state.Hero.Name, reward.SectReputation, reward.SectNotice),
                new CombatEventSnapshot(turn, "adventure_notice", "江湖", state.Stage.Chapter, state.Stage.Floor, reward.AdventureNotice));
            eventLog = AppendCombatEvent(eventLog, defeatMessage);
            eventLog = AppendCombatEvent(eventLog, reward.RewardMessage);
            eventLog = AppendCombatEvent(eventLog, reward.DropMessage);
            nextHero = state.Hero with
            {
                Cultivation = state.Hero.Cultivation + reward.Cultivation,
                Resources = state.Hero.Resources with
                {
                    Silver = state.Hero.Resources.Silver + reward.Silver,
                    Essence = state.Hero.Resources.Essence + reward.Essence,
                    Food = state.Hero.Resources.Food + reward.Food
                },
                JianghuQualities = state.Hero.JianghuQualities with
                {
                    Reputation = state.Hero.JianghuQualities.Reputation + reward.SectReputation
                }
            };
            commandMessage = reward.AdventureNotice;
        }

        var next = state with
        {
            Hero = nextHero,
            Enemy = state.Enemy with
            {
                Hp = enemyHp
            },
            Combat = new CombatSnapshot(
                turn,
                "hero_attack",
                "enemy",
                damage,
                damageRatio,
                hitKind,
                enemyDefeated,
                message,
                eventLog,
                events,
                state.Combat.MigrationWarnings),
            Commands = state.Commands with
            {
                IsPaused = enemyDefeated && !state.Commands.AutoExplore,
                AutoExplore = state.Commands.AutoExplore,
                LastCommand = commandMessage
            }
        };

        SaveManager.SaveState(next);
        return CommandResult.Success(message, next);
    }

    private static CommandResult AdvanceToNextEnemy(GameState state)
    {
        var nextFloor = state.Stage.Floor + 1;
        var nextEnemy = CreateEnemyForFloor(nextFloor, state.Enemy.MaxHp);
        var message = $"进入第{nextFloor}层 · {nextEnemy.Name} 登场";
        var eventLog = AppendCombatEvent(state.Combat.EventLog, message);
        var events = AppendCombatEvents(
            state.Combat.Events,
            new CombatEventSnapshot(state.Combat.Turn, "advance", "system", nextEnemy.Name, nextFloor, message));

        var next = state with
        {
            Stage = state.Stage with
            {
                Floor = nextFloor,
                Year = Math.Max(state.Stage.Year, Math.Max(0, nextFloor / 4)),
                Direction = state.Commands.AutoExplore ? "自动闯荡 / 逐层推进" : state.Stage.Direction
            },
            Enemy = nextEnemy,
            Combat = new CombatSnapshot(
                state.Combat.Turn,
                "advance",
                "enemy",
                0,
                0,
                "advance",
                false,
                message,
                eventLog,
                events,
                state.Combat.MigrationWarnings),
            Commands = state.Commands with
            {
                IsPaused = false,
                LastCommand = message
            }
        };

        SaveManager.SaveState(next);
        return CommandResult.Success(message, next);
    }

    private static EnemySnapshot CreateEnemyForFloor(int floor, int previousMaxHp)
    {
        var names = new[] { "巡山刀客", "青衣剑手", "游方棍僧", "铁掌护院", "山门执事" };
        var tags = new[] { "外功", "身法", "防反", "内功", "破招" };
        var name = names[Math.Abs(floor) % names.Length];
        var tag = tags[Math.Abs(floor + 2) % tags.Length];
        var maxHp = Math.Max(1, previousMaxHp + 420 + floor * 60);
        return new EnemySnapshot(
            name,
            $"第{floor}层",
            tag,
            maxHp,
            maxHp,
            Math.Max(1, maxHp / 2).ToString());
    }

    private static string[] AppendCombatEvent(string[] eventLog, string message)
    {
        const int maxEvents = 6;
        return eventLog
            .Concat(new[] { message })
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .TakeLast(maxEvents)
            .ToArray();
    }

    private static CombatEventSnapshot[] AppendCombatEvents(CombatEventSnapshot[] events, params CombatEventSnapshot[] newEvents)
    {
        const int maxEvents = 12;
        return events
            .Concat(newEvents)
            .Where(value => !string.IsNullOrWhiteSpace(value.Message))
            .TakeLast(maxEvents)
            .ToArray();
    }

    private static WeaponCommandData PickNextWeapon(string currentName)
    {
        var weapons = new[]
        {
            new WeaponCommandData("王府纹剑", "sword", "剑", 197, new[] { "剑气拖尾", "攻击评分 +197" }),
            new WeaponCommandData("玄铁重剑", "sword", "剑", 888, new[] { "重剑拖尾", "攻击评分 +888" }),
            new WeaponCommandData("断岳长刀", "blade", "刀", 520, new[] { "刀弧拖尾", "攻击评分 +520" })
        };

        var currentIndex = Array.FindIndex(weapons, weapon => string.Equals(weapon.Name, currentName, StringComparison.Ordinal));
        return weapons[(currentIndex + 1 + weapons.Length) % weapons.Length];
    }

    private static int CalculateHeroDamage(GameState state, int activeArtIndex)
    {
        var martialBonus = 120 + activeArtIndex * 180;
        var rawDamage = state.Hero.BattleStats.OuterPower
            + state.Hero.BattleStats.InnerPower / 2
            + state.WeaponInfo.Score
            + martialBonus;
        var enemyGuard = Math.Max(0, state.Enemy.MaxHp / 22);
        return Math.Clamp(rawDamage - enemyGuard, 1, Math.Max(1, state.Enemy.Hp));
    }

    private readonly record struct WeaponCommandData(string Name, string WeaponKey, string Type, int Score, string[] Effects);
}

public readonly record struct CommandResult(bool Ok, string Message, GameState? State)
{
    public static CommandResult Success(string message, GameState state)
    {
        return new CommandResult(true, message, state);
    }

    public static CommandResult Fail(string message)
    {
        return new CommandResult(false, message, null);
    }
}
