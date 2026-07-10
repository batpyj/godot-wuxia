using System;
using Godot;

public partial class EnemyRig : Node2D
{
    private static readonly StringName IdleAnimation = "idle";
    private static readonly StringName AttackAnimation = "attack";
    private static readonly StringName GrazeAnimation = "graze";
    private static readonly StringName HitAnimation = "hit";
    private static readonly StringName DefeatAnimation = "defeat";
    private static readonly StringName SpawnAnimation = "spawn";

    public string ActorRole { get; set; } = "enemy";
    public string BodyTexturePath { get; set; } = "res://assets/actors/enemies/enemy_early_idle.webp";
    public string WeaponTexturePath { get; set; } = "res://assets/weapons/blade/blade_basic.png";
    public Vector2 FeetAnchorPixels { get; set; } = new(469, 1475);
    public Vector2 WeaponSocketLocalFromFeet { get; set; } = new(-230, -950);
    public float WeaponScale { get; set; } = 2.1f;
    public bool IsBoss { get; set; }

    public Marker2D GroundAnchor { get; private set; } = null!;
    public Node2D BodyRoot { get; private set; } = null!;
    public Sprite2D FullBody { get; private set; } = null!;
    public Marker2D WeaponSocket { get; private set; } = null!;
    public Node2D WeaponRoot { get; private set; } = null!;
    public Sprite2D Weapon { get; private set; } = null!;
    public Polygon2D BossAura { get; private set; } = null!;
    public AnimationPlayer AnimationPlayer { get; private set; } = null!;

    public override void _Ready()
    {
        GroundAnchor = GetNode<Marker2D>("GroundAnchor");
        BodyRoot = GetNode<Node2D>("BodyRoot");
        FullBody = GetNode<Sprite2D>("BodyRoot/FullBody");
        WeaponSocket = GetNode<Marker2D>("BodyRoot/WeaponSocket");
        WeaponRoot = GetNode<Node2D>("BodyRoot/WeaponSocket/WeaponRoot");
        Weapon = GetNode<Sprite2D>("BodyRoot/WeaponSocket/WeaponRoot/Weapon");
        BossAura = GetNode<Polygon2D>("BodyRoot/BossAura");
        AnimationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");

        ApplyResources();
        SetNearestFilters(this);
        EnsureBattleAnimations();
        AnimationPlayer.AnimationFinished += OnAnimationFinished;
        PlayStartupAnimation();
    }

    public void PlayIdle()
    {
        AnimationPlayer.Play(IdleAnimation);
    }

    public void PlayAttack()
    {
        AnimationPlayer.Play(AttackAnimation);
    }

    public void PlayGraze()
    {
        AnimationPlayer.Play(GrazeAnimation);
    }

    public void PlayHit()
    {
        AnimationPlayer.Play(HitAnimation);
    }

    public void PlayDefeat()
    {
        AnimationPlayer.Play(DefeatAnimation);
    }

    public void PlaySpawn()
    {
        AnimationPlayer.Play(SpawnAnimation);
    }

    public void SetProfile(
        string bodyTexturePath,
        string weaponTexturePath,
        Vector2 feetAnchorPixels,
        Vector2 weaponSocketLocalFromFeet,
        float weaponScale,
        bool isBoss)
    {
        BodyTexturePath = bodyTexturePath;
        WeaponTexturePath = weaponTexturePath;
        FeetAnchorPixels = feetAnchorPixels;
        WeaponSocketLocalFromFeet = weaponSocketLocalFromFeet;
        WeaponScale = weaponScale;
        IsBoss = isBoss;
        ApplyResources();
    }

    public void SetWeaponTexturePath(string texturePath)
    {
        WeaponTexturePath = texturePath;
        Weapon.Texture = LoadTexture(WeaponTexturePath);
    }

    private void ApplyResources()
    {
        FullBody.Texture = LoadTexture(BodyTexturePath);
        FullBody.Position = -FeetAnchorPixels;
        BodyRoot.Position = Vector2.Zero;
        BodyRoot.RotationDegrees = 0.0f;
        BodyRoot.Scale = Vector2.One;
        BodyRoot.Modulate = Colors.White;

        Weapon.Texture = LoadTexture(WeaponTexturePath);
        WeaponRoot.Visible = true;
        WeaponRoot.Position = Vector2.Zero;
        WeaponRoot.RotationDegrees = 0.0f;
        Weapon.Position = new Vector2(-193.2f, -386.4f);
        Weapon.Scale = new Vector2(WeaponScale, WeaponScale);
        WeaponSocket.Position = WeaponSocketLocalFromFeet;

        BossAura.Visible = IsBoss;
        BossAura.Color = IsBoss
            ? new Color(0.52f, 0.18f, 0.72f, 0.22f)
            : Colors.Transparent;
    }

    private void EnsureBattleAnimations()
    {
        var library = new AnimationLibrary();
        AddAnimationIfMissing(library, IdleAnimation, BuildIdleAnimation());
        AddAnimationIfMissing(library, AttackAnimation, BuildAttackAnimation());
        AddAnimationIfMissing(library, GrazeAnimation, BuildGrazeAnimation());
        AddAnimationIfMissing(library, HitAnimation, BuildHitAnimation());
        AddAnimationIfMissing(library, DefeatAnimation, BuildDefeatAnimation());
        AddAnimationIfMissing(library, SpawnAnimation, BuildSpawnAnimation());
        AnimationPlayer.AddAnimationLibrary("", library);
    }

    private static void AddAnimationIfMissing(AnimationLibrary library, StringName name, Animation animation)
    {
        if (!library.HasAnimation(name))
        {
            library.AddAnimation(name, animation);
        }
    }

    private Animation BuildIdleAnimation()
    {
        var idle = new Animation
        {
            Length = IsBoss ? 3.2 : 2.6,
            LoopMode = Animation.LoopModeEnum.Linear
        };

        var bodyPosition = AddValueTrack(idle, "BodyRoot:position");
        idle.TrackInsertKey(bodyPosition, 0.0, Vector2.Zero);
        idle.TrackInsertKey(bodyPosition, idle.Length / 2.0, new Vector2(0, IsBoss ? -3 : -2));
        idle.TrackInsertKey(bodyPosition, idle.Length, Vector2.Zero);

        var bodyRotation = AddValueTrack(idle, "BodyRoot:rotation_degrees");
        idle.TrackInsertKey(bodyRotation, 0.0, 0.0f);
        idle.TrackInsertKey(bodyRotation, idle.Length / 2.0, IsBoss ? -0.5f : -0.25f);
        idle.TrackInsertKey(bodyRotation, idle.Length, 0.0f);

        var bodyScale = AddValueTrack(idle, "BodyRoot:scale");
        idle.TrackInsertKey(bodyScale, 0.0, Vector2.One);
        idle.TrackInsertKey(bodyScale, idle.Length / 2.0, IsBoss ? new Vector2(1.012f, 0.996f) : new Vector2(1.006f, 0.998f));
        idle.TrackInsertKey(bodyScale, idle.Length, Vector2.One);

        var bodyColor = AddValueTrack(idle, "BodyRoot:modulate");
        idle.TrackInsertKey(bodyColor, 0.0, Colors.White);
        idle.TrackInsertKey(bodyColor, idle.Length, Colors.White);

        var weaponVisible = AddValueTrack(idle, "BodyRoot/WeaponSocket/WeaponRoot:visible");
        idle.TrackInsertKey(weaponVisible, 0.0, true);
        idle.TrackInsertKey(weaponVisible, idle.Length, true);

        var weaponRotation = AddValueTrack(idle, "BodyRoot/WeaponSocket/WeaponRoot:rotation_degrees");
        idle.TrackInsertKey(weaponRotation, 0.0, 0.0f);
        idle.TrackInsertKey(weaponRotation, idle.Length / 2.0, IsBoss ? -1.0f : -0.7f);
        idle.TrackInsertKey(weaponRotation, idle.Length, 0.0f);

        var auraColor = AddValueTrack(idle, "BodyRoot/BossAura:color");
        idle.TrackInsertKey(auraColor, 0.0, BossAuraColor(0.16f));
        idle.TrackInsertKey(auraColor, idle.Length / 2.0, BossAuraColor(0.26f));
        idle.TrackInsertKey(auraColor, idle.Length, BossAuraColor(0.16f));

        return idle;
    }

    private Animation BuildAttackAnimation()
    {
        var attack = new Animation { Length = IsBoss ? 0.62 : 0.5 };
        var strikeX = IsBoss ? 96 : 72;

        var bodyPosition = AddValueTrack(attack, "BodyRoot:position");
        attack.TrackInsertKey(bodyPosition, 0.0, Vector2.Zero);
        attack.TrackInsertKey(bodyPosition, 0.14, new Vector2(18, -4));
        attack.TrackInsertKey(bodyPosition, 0.26, new Vector2(strikeX, -8));
        attack.TrackInsertKey(bodyPosition, attack.Length, Vector2.Zero);

        var bodyRotation = AddValueTrack(attack, "BodyRoot:rotation_degrees");
        attack.TrackInsertKey(bodyRotation, 0.0, 0.0f);
        attack.TrackInsertKey(bodyRotation, 0.26, IsBoss ? -4.5f : -3.0f);
        attack.TrackInsertKey(bodyRotation, attack.Length, 0.0f);

        var weaponRotation = AddValueTrack(attack, "BodyRoot/WeaponSocket/WeaponRoot:rotation_degrees");
        attack.TrackInsertKey(weaponRotation, 0.0, 0.0f);
        attack.TrackInsertKey(weaponRotation, 0.14, 28.0f);
        attack.TrackInsertKey(weaponRotation, 0.26, -52.0f);
        attack.TrackInsertKey(weaponRotation, attack.Length, 0.0f);

        var weaponPosition = AddValueTrack(attack, "BodyRoot/WeaponSocket/WeaponRoot:position");
        attack.TrackInsertKey(weaponPosition, 0.0, Vector2.Zero);
        attack.TrackInsertKey(weaponPosition, 0.26, new Vector2(34, -18));
        attack.TrackInsertKey(weaponPosition, attack.Length, Vector2.Zero);

        return attack;
    }

    private Animation BuildGrazeAnimation()
    {
        var graze = new Animation { Length = 0.28 };

        var bodyPosition = AddValueTrack(graze, "BodyRoot:position");
        graze.TrackInsertKey(bodyPosition, 0.0, Vector2.Zero);
        graze.TrackInsertKey(bodyPosition, 0.06, new Vector2(-8, 0));
        graze.TrackInsertKey(bodyPosition, 0.12, new Vector2(5, 0));
        graze.TrackInsertKey(bodyPosition, 0.28, Vector2.Zero);

        var bodyColor = AddValueTrack(graze, "BodyRoot:modulate");
        graze.TrackInsertKey(bodyColor, 0.0, Colors.White);
        graze.TrackInsertKey(bodyColor, 0.06, new Color(1.0f, 0.25f, 0.2f, 1.0f));
        graze.TrackInsertKey(bodyColor, 0.16, new Color(1.0f, 0.55f, 0.45f, 1.0f));
        graze.TrackInsertKey(bodyColor, 0.28, Colors.White);

        return graze;
    }

    private Animation BuildHitAnimation()
    {
        var hit = new Animation { Length = 0.62 };

        var bodyPosition = AddValueTrack(hit, "BodyRoot:position");
        hit.TrackInsertKey(bodyPosition, 0.0, Vector2.Zero);
        hit.TrackInsertKey(bodyPosition, 0.08, new Vector2(-28, 2));
        hit.TrackInsertKey(bodyPosition, 0.24, new Vector2(-16, 0));
        hit.TrackInsertKey(bodyPosition, 0.62, Vector2.Zero);

        var bodyRotation = AddValueTrack(hit, "BodyRoot:rotation_degrees");
        hit.TrackInsertKey(bodyRotation, 0.0, 0.0f);
        hit.TrackInsertKey(bodyRotation, 0.08, IsBoss ? 3.0f : 4.0f);
        hit.TrackInsertKey(bodyRotation, 0.24, IsBoss ? 1.2f : 1.8f);
        hit.TrackInsertKey(bodyRotation, 0.62, 0.0f);

        var bodyColor = AddValueTrack(hit, "BodyRoot:modulate");
        hit.TrackInsertKey(bodyColor, 0.0, Colors.White);
        hit.TrackInsertKey(bodyColor, 0.08, new Color(1.0f, 0.2f, 0.18f, 1.0f));
        hit.TrackInsertKey(bodyColor, 0.22, new Color(1.0f, 0.55f, 0.5f, 1.0f));
        hit.TrackInsertKey(bodyColor, 0.62, Colors.White);

        var weaponRotation = AddValueTrack(hit, "BodyRoot/WeaponSocket/WeaponRoot:rotation_degrees");
        hit.TrackInsertKey(weaponRotation, 0.0, 0.0f);
        hit.TrackInsertKey(weaponRotation, 0.1, 12.0f);
        hit.TrackInsertKey(weaponRotation, 0.62, 0.0f);

        return hit;
    }

    private Animation BuildDefeatAnimation()
    {
        var defeat = new Animation { Length = IsBoss ? 1.15 : 0.95 };

        var bodyPosition = AddValueTrack(defeat, "BodyRoot:position");
        defeat.TrackInsertKey(bodyPosition, 0.0, Vector2.Zero);
        defeat.TrackInsertKey(bodyPosition, 0.2, new Vector2(-20, 12));
        defeat.TrackInsertKey(bodyPosition, 0.58, new Vector2(-46, 48));
        defeat.TrackInsertKey(bodyPosition, defeat.Length, new Vector2(-62, IsBoss ? 86 : 72));

        var bodyRotation = AddValueTrack(defeat, "BodyRoot:rotation_degrees");
        defeat.TrackInsertKey(bodyRotation, 0.0, 0.0f);
        defeat.TrackInsertKey(bodyRotation, 0.2, IsBoss ? 3.0f : 5.0f);
        defeat.TrackInsertKey(bodyRotation, 0.58, IsBoss ? 8.0f : 14.0f);
        defeat.TrackInsertKey(bodyRotation, defeat.Length, IsBoss ? 11.0f : 18.0f);

        var bodyScale = AddValueTrack(defeat, "BodyRoot:scale");
        defeat.TrackInsertKey(bodyScale, 0.0, Vector2.One);
        defeat.TrackInsertKey(bodyScale, 0.58, new Vector2(1.0f, 0.94f));
        defeat.TrackInsertKey(bodyScale, defeat.Length, new Vector2(1.0f, IsBoss ? 0.9f : 0.84f));

        var bodyColor = AddValueTrack(defeat, "BodyRoot:modulate");
        defeat.TrackInsertKey(bodyColor, 0.0, Colors.White);
        defeat.TrackInsertKey(bodyColor, 0.2, new Color(1.0f, 0.4f, 0.36f, 1.0f));
        defeat.TrackInsertKey(bodyColor, 0.58, new Color(0.72f, 0.72f, 0.8f, 0.76f));
        defeat.TrackInsertKey(bodyColor, defeat.Length, new Color(0.42f, 0.42f, 0.48f, IsBoss ? 0.56f : 0.42f));

        var weaponVisible = AddValueTrack(defeat, "BodyRoot/WeaponSocket/WeaponRoot:visible");
        defeat.TrackInsertKey(weaponVisible, 0.0, true);
        defeat.TrackInsertKey(weaponVisible, defeat.Length, true);

        return defeat;
    }

    private Animation BuildSpawnAnimation()
    {
        var spawn = new Animation { Length = IsBoss ? 0.95 : 0.78 };

        var bodyPosition = AddValueTrack(spawn, "BodyRoot:position");
        spawn.TrackInsertKey(bodyPosition, 0.0, new Vector2(76, 28));
        spawn.TrackInsertKey(bodyPosition, 0.18, new Vector2(42, 10));
        spawn.TrackInsertKey(bodyPosition, 0.42, new Vector2(-8, 0));
        spawn.TrackInsertKey(bodyPosition, spawn.Length, Vector2.Zero);

        var bodyRotation = AddValueTrack(spawn, "BodyRoot:rotation_degrees");
        spawn.TrackInsertKey(bodyRotation, 0.0, IsBoss ? -5.0f : -3.0f);
        spawn.TrackInsertKey(bodyRotation, 0.42, IsBoss ? 1.5f : 0.8f);
        spawn.TrackInsertKey(bodyRotation, spawn.Length, 0.0f);

        var bodyScale = AddValueTrack(spawn, "BodyRoot:scale");
        spawn.TrackInsertKey(bodyScale, 0.0, new Vector2(0.94f, 0.98f));
        spawn.TrackInsertKey(bodyScale, 0.42, new Vector2(1.02f, 1.0f));
        spawn.TrackInsertKey(bodyScale, spawn.Length, Vector2.One);

        var bodyColor = AddValueTrack(spawn, "BodyRoot:modulate");
        spawn.TrackInsertKey(bodyColor, 0.0, new Color(1.0f, 1.0f, 1.0f, 0.0f));
        spawn.TrackInsertKey(bodyColor, 0.22, new Color(0.9f, 0.94f, 1.0f, 0.72f));
        spawn.TrackInsertKey(bodyColor, spawn.Length, Colors.White);

        var weaponVisible = AddValueTrack(spawn, "BodyRoot/WeaponSocket/WeaponRoot:visible");
        spawn.TrackInsertKey(weaponVisible, 0.0, true);
        spawn.TrackInsertKey(weaponVisible, spawn.Length, true);

        var weaponRotation = AddValueTrack(spawn, "BodyRoot/WeaponSocket/WeaponRoot:rotation_degrees");
        spawn.TrackInsertKey(weaponRotation, 0.0, 16.0f);
        spawn.TrackInsertKey(weaponRotation, 0.42, -8.0f);
        spawn.TrackInsertKey(weaponRotation, spawn.Length, 0.0f);

        return spawn;
    }

    private void PlayStartupAnimation()
    {
        var target = OS.GetEnvironment("WUXIA_GODOT_PREVIEW_TARGET").ToLowerInvariant();
        if (!string.Equals(target, ActorRole.ToLowerInvariant(), StringComparison.Ordinal))
        {
            PlayIdle();
            return;
        }

        var previewAction = OS.GetEnvironment("WUXIA_GODOT_PREVIEW_ACTION").ToLowerInvariant();
        switch (previewAction)
        {
            case "attack":
                PlayAttack();
                break;
            case "graze":
                PlayGraze();
                break;
            case "hit":
                PlayHit();
                break;
            case "defeat":
                PlayDefeat();
                break;
            default:
                PlayIdle();
                break;
        }
    }

    private void OnAnimationFinished(StringName animationName)
    {
        if (animationName != IdleAnimation && animationName != DefeatAnimation)
        {
            PlayIdle();
        }
    }

    private Color BossAuraColor(float alpha)
    {
        return IsBoss
            ? new Color(0.52f, 0.18f, 0.72f, alpha)
            : Colors.Transparent;
    }

    private static int AddValueTrack(Animation animation, string path)
    {
        var track = animation.AddTrack(Animation.TrackType.Value);
        animation.TrackSetPath(track, new NodePath(path));
        return track;
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

    private static void SetNearestFilters(Node node)
    {
        if (node is Sprite2D sprite)
        {
            sprite.TextureFilter = TextureFilterEnum.Nearest;
        }

        foreach (var child in node.GetChildren())
        {
            if (child is Node childNode)
            {
                SetNearestFilters(childNode);
            }
        }
    }
}
