using System;
using System.Collections.Generic;
using System.Linq;

public sealed class CombatPresentationTimeline
{
    private readonly Queue<CombatTimelineStep> _pendingSteps = new();
    private readonly List<CombatTimelineTraceEntry> _trace = new();
    private CombatTimelineStep? _activeStep;
    private double _activeRemainingSeconds;

    public event Action<CombatTimelineStep>? StepStarted;

    public bool IsActive => _activeStep != null;

    public void Begin(params CombatTimelineStep[] steps)
    {
        _pendingSteps.Clear();
        foreach (var step in steps.Where(step => step.HoldSeconds > 0))
        {
            _pendingSteps.Enqueue(step);
        }

        StartNextStep();
    }

    public void Advance(double delta)
    {
        if (_activeStep == null)
        {
            return;
        }

        _activeRemainingSeconds -= Math.Max(0, delta);
        while (_activeRemainingSeconds <= 0 && _activeStep != null)
        {
            StartNextStep();
        }
    }

    public CombatTimelineDump CreateDump(
        GameState state,
        double autoCombatIntervalSeconds,
        EnemyVisualProfile enemyProfile)
    {
        return new CombatTimelineDump(
            _activeStep?.Phase ?? "idle",
            Math.Round(Math.Max(0, _activeRemainingSeconds), 3),
            _pendingSteps.Select(step => step.Phase).ToArray(),
            _trace.ToArray(),
            autoCombatIntervalSeconds,
            state.Combat.Turn,
            state.Stage.Floor,
            state.Combat.LastHitKind,
            state.Enemy.Name,
            enemyProfile.Id,
            enemyProfile.BodyTexturePath,
            enemyProfile.WeaponTexturePath);
    }

    private void StartNextStep()
    {
        if (_pendingSteps.Count == 0)
        {
            _activeStep = null;
            _activeRemainingSeconds = 0;
            return;
        }

        var step = _pendingSteps.Dequeue();
        _activeStep = step;
        _activeRemainingSeconds = step.HoldSeconds;
        _trace.Add(new CombatTimelineTraceEntry(step.Phase, step.HoldSeconds, step.Reason));

        if (_trace.Count > 24)
        {
            _trace.RemoveRange(0, _trace.Count - 24);
        }

        StepStarted?.Invoke(step);
    }
}

public readonly record struct CombatTimelineStep(string Phase, double HoldSeconds, string Reason);

public sealed record CombatTimelineTraceEntry(string Phase, double HoldSeconds, string Reason);

public sealed record CombatTimelineDump(
    string ActivePhase,
    double RemainingSeconds,
    string[] PendingPhases,
    CombatTimelineTraceEntry[] Trace,
    double AutoCombatIntervalSeconds,
    int Turn,
    int Floor,
    string LastHitKind,
    string EnemyName,
    string EnemyProfileId,
    string EnemyBodyTexturePath,
    string EnemyWeaponTexturePath);
