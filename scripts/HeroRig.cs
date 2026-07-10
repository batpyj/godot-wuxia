using Godot;

public partial class HeroRig : Node2D
{
    private static readonly StringName IdleAnimation = "idle";
    private static readonly StringName AttackAnimation = "attack";
    private static readonly StringName GrazeAnimation = "graze";
    private static readonly StringName HitAnimation = "hit";
    private static readonly StringName DefeatAnimation = "defeat";
    private static readonly StringName MeditateAnimation = "meditate";

    public Marker2D GroundAnchor { get; private set; } = null!;
    public Node2D BodyRoot { get; private set; } = null!;
    public Node2D LayerRoot { get; private set; } = null!;
    public Marker2D FrontHand { get; private set; } = null!;
    public Marker2D WeaponSocket { get; private set; } = null!;
    public Node2D WeaponRoot { get; private set; } = null!;
    public Sprite2D Weapon { get; private set; } = null!;
    public Node2D FxRoot { get; private set; } = null!;
    public AnimationPlayer AnimationPlayer { get; private set; } = null!;

    public override void _Ready()
    {
        GroundAnchor = GetNode<Marker2D>("GroundAnchor");
        BodyRoot = GetNode<Node2D>("BodyRoot");
        LayerRoot = GetNode<Node2D>("BodyRoot/LayerRoot");
        FrontHand = GetNode<Marker2D>("BodyRoot/FrontHand");
        WeaponSocket = GetNode<Marker2D>("BodyRoot/FrontHand/WeaponSocket");
        WeaponRoot = GetNode<Node2D>("BodyRoot/FrontHand/WeaponSocket/WeaponRoot");
        Weapon = GetNode<Sprite2D>("BodyRoot/FrontHand/WeaponSocket/WeaponRoot/Weapon");
        FxRoot = GetNode<Node2D>("FxRoot");
        AnimationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");

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

    public void PlayMeditate()
    {
        AnimationPlayer.Play(MeditateAnimation);
    }

    public void SetWeaponTexturePath(string path)
    {
        var texture = GD.Load<Texture2D>(path);
        if (texture == null)
        {
            GD.PushWarning($"Hero weapon texture not found: {path}");
            return;
        }

        Weapon.Texture = texture;
    }

    private void EnsureBattleAnimations()
    {
        var library = new AnimationLibrary();
        AddAnimationIfMissing(library, IdleAnimation, BuildIdleAnimation());
        AddAnimationIfMissing(library, AttackAnimation, BuildAttackAnimation());
        AddAnimationIfMissing(library, GrazeAnimation, BuildGrazeAnimation());
        AddAnimationIfMissing(library, HitAnimation, BuildHitAnimation());
        AddAnimationIfMissing(library, DefeatAnimation, BuildDefeatAnimation());
        AddAnimationIfMissing(library, MeditateAnimation, BuildMeditateAnimation());
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
        if (AnimationPlayer.HasAnimation(IdleAnimation))
        {
            return AnimationPlayer.GetAnimation(IdleAnimation);
        }

        var idle = new Animation
        {
            Length = 2.8,
            LoopMode = Animation.LoopModeEnum.Linear
        };

        var bodyPositionTrack = AddValueTrack(idle, "BodyRoot:position");
        idle.TrackInsertKey(bodyPositionTrack, 0.0, Vector2.Zero);
        idle.TrackInsertKey(bodyPositionTrack, 2.8, Vector2.Zero);

        var bodyRotationTrack = AddValueTrack(idle, "BodyRoot:rotation_degrees");
        idle.TrackInsertKey(bodyRotationTrack, 0.0, 0.0f);
        idle.TrackInsertKey(bodyRotationTrack, 2.8, 0.0f);

        var bodyScaleTrack = AddValueTrack(idle, "BodyRoot:scale");
        idle.TrackInsertKey(bodyScaleTrack, 0.0, Vector2.One);
        idle.TrackInsertKey(bodyScaleTrack, 2.8, Vector2.One);

        var bodyColorTrack = AddValueTrack(idle, "BodyRoot:modulate");
        idle.TrackInsertKey(bodyColorTrack, 0.0, Colors.White);
        idle.TrackInsertKey(bodyColorTrack, 2.8, Colors.White);

        var weaponVisibleTrack = AddValueTrack(idle, "BodyRoot/FrontHand/WeaponSocket/WeaponRoot:visible");
        idle.TrackInsertKey(weaponVisibleTrack, 0.0, true);
        idle.TrackInsertKey(weaponVisibleTrack, 2.8, true);

        var hairTrack = AddValueTrack(idle, "BodyRoot/LayerRoot/Hair:position");
        idle.TrackInsertKey(hairTrack, 0.0, new Vector2(-500, -1488));
        idle.TrackInsertKey(hairTrack, 1.4, new Vector2(-499, -1491));
        idle.TrackInsertKey(hairTrack, 2.8, new Vector2(-500, -1488));

        var headTrack = AddValueTrack(idle, "BodyRoot/LayerRoot/Head:position");
        idle.TrackInsertKey(headTrack, 0.0, new Vector2(-500, -1488));
        idle.TrackInsertKey(headTrack, 1.4, new Vector2(-500, -1490));
        idle.TrackInsertKey(headTrack, 2.8, new Vector2(-500, -1488));

        var torsoTrack = AddValueTrack(idle, "BodyRoot/LayerRoot/Torso:position");
        idle.TrackInsertKey(torsoTrack, 0.0, new Vector2(-500, -1488));
        idle.TrackInsertKey(torsoTrack, 1.4, new Vector2(-500, -1489));
        idle.TrackInsertKey(torsoTrack, 2.8, new Vector2(-500, -1488));

        var hipsTrack = AddValueTrack(idle, "BodyRoot/LayerRoot/Hips:position");
        idle.TrackInsertKey(hipsTrack, 0.0, new Vector2(-500, -1488));
        idle.TrackInsertKey(hipsTrack, 1.4, new Vector2(-500, -1488.5f));
        idle.TrackInsertKey(hipsTrack, 2.8, new Vector2(-500, -1488));

        var frontHandTrack = AddValueTrack(idle, "BodyRoot/FrontHand:position");
        idle.TrackInsertKey(frontHandTrack, 0.0, new Vector2(256, -1016));
        idle.TrackInsertKey(frontHandTrack, 1.4, new Vector2(258, -1019));
        idle.TrackInsertKey(frontHandTrack, 2.8, new Vector2(256, -1016));

        var weaponRotationTrack = AddValueTrack(idle, "BodyRoot/FrontHand/WeaponSocket/WeaponRoot:rotation_degrees");
        idle.TrackInsertKey(weaponRotationTrack, 0.0, 0.0f);
        idle.TrackInsertKey(weaponRotationTrack, 1.4, -1.2f);
        idle.TrackInsertKey(weaponRotationTrack, 2.8, 0.0f);

        return idle;
    }

    private static Animation BuildAttackAnimation()
    {
        var attack = new Animation { Length = 0.5 };

        var hand = AddValueTrack(attack, "BodyRoot/FrontHand:position");
        attack.TrackInsertKey(hand, 0.0, new Vector2(256, -1016));
        attack.TrackInsertKey(hand, 0.16, new Vector2(372, -1062));
        attack.TrackInsertKey(hand, 0.32, new Vector2(230, -1006));
        attack.TrackInsertKey(hand, 0.5, new Vector2(256, -1016));

        var weapon = AddValueTrack(attack, "BodyRoot/FrontHand/WeaponSocket/WeaponRoot:rotation_degrees");
        attack.TrackInsertKey(weapon, 0.0, 0.0f);
        attack.TrackInsertKey(weapon, 0.16, -46.0f);
        attack.TrackInsertKey(weapon, 0.32, 14.0f);
        attack.TrackInsertKey(weapon, 0.5, 0.0f);

        var torso = AddValueTrack(attack, "BodyRoot/LayerRoot/Torso:position");
        attack.TrackInsertKey(torso, 0.0, new Vector2(-500, -1488));
        attack.TrackInsertKey(torso, 0.16, new Vector2(-492, -1490));
        attack.TrackInsertKey(torso, 0.5, new Vector2(-500, -1488));

        var head = AddValueTrack(attack, "BodyRoot/LayerRoot/Head:position");
        attack.TrackInsertKey(head, 0.0, new Vector2(-500, -1488));
        attack.TrackInsertKey(head, 0.16, new Vector2(-493, -1490));
        attack.TrackInsertKey(head, 0.5, new Vector2(-500, -1488));

        return attack;
    }

    private static Animation BuildGrazeAnimation()
    {
        var graze = new Animation { Length = 0.28 };

        var bodyPosition = AddValueTrack(graze, "BodyRoot:position");
        graze.TrackInsertKey(bodyPosition, 0.0, Vector2.Zero);
        graze.TrackInsertKey(bodyPosition, 0.06, new Vector2(-8, 0));
        graze.TrackInsertKey(bodyPosition, 0.12, new Vector2(4, 0));
        graze.TrackInsertKey(bodyPosition, 0.28, Vector2.Zero);

        var bodyColor = AddValueTrack(graze, "BodyRoot:modulate");
        graze.TrackInsertKey(bodyColor, 0.0, Colors.White);
        graze.TrackInsertKey(bodyColor, 0.06, new Color(1.0f, 0.18f, 0.18f, 1.0f));
        graze.TrackInsertKey(bodyColor, 0.18, new Color(1.0f, 0.42f, 0.42f, 1.0f));
        graze.TrackInsertKey(bodyColor, 0.28, Colors.White);

        return graze;
    }

    private static Animation BuildHitAnimation()
    {
        var hit = new Animation { Length = 0.58 };

        var bodyPosition = AddValueTrack(hit, "BodyRoot:position");
        hit.TrackInsertKey(bodyPosition, 0.0, Vector2.Zero);
        hit.TrackInsertKey(bodyPosition, 0.08, new Vector2(-20, 0));
        hit.TrackInsertKey(bodyPosition, 0.24, new Vector2(-12, 0));
        hit.TrackInsertKey(bodyPosition, 0.58, Vector2.Zero);

        var bodyRotation = AddValueTrack(hit, "BodyRoot:rotation_degrees");
        hit.TrackInsertKey(bodyRotation, 0.0, 0.0f);
        hit.TrackInsertKey(bodyRotation, 0.08, -3.0f);
        hit.TrackInsertKey(bodyRotation, 0.24, -1.0f);
        hit.TrackInsertKey(bodyRotation, 0.58, 0.0f);

        var bodyColor = AddValueTrack(hit, "BodyRoot:modulate");
        hit.TrackInsertKey(bodyColor, 0.0, Colors.White);
        hit.TrackInsertKey(bodyColor, 0.08, new Color(1.0f, 0.22f, 0.22f, 1.0f));
        hit.TrackInsertKey(bodyColor, 0.2, new Color(1.0f, 0.58f, 0.58f, 1.0f));
        hit.TrackInsertKey(bodyColor, 0.58, Colors.White);

        var hair = AddValueTrack(hit, "BodyRoot/LayerRoot/Hair:position");
        hit.TrackInsertKey(hair, 0.0, new Vector2(-500, -1488));
        hit.TrackInsertKey(hair, 0.12, new Vector2(-512, -1485));
        hit.TrackInsertKey(hair, 0.58, new Vector2(-500, -1488));

        var torso = AddValueTrack(hit, "BodyRoot/LayerRoot/Torso:position");
        hit.TrackInsertKey(torso, 0.0, new Vector2(-500, -1488));
        hit.TrackInsertKey(torso, 0.12, new Vector2(-510, -1486));
        hit.TrackInsertKey(torso, 0.58, new Vector2(-500, -1488));

        return hit;
    }

    private static Animation BuildDefeatAnimation()
    {
        var defeat = new Animation { Length = 0.95 };

        var bodyPosition = AddValueTrack(defeat, "BodyRoot:position");
        defeat.TrackInsertKey(bodyPosition, 0.0, Vector2.Zero);
        defeat.TrackInsertKey(bodyPosition, 0.18, new Vector2(-16, 10));
        defeat.TrackInsertKey(bodyPosition, 0.5, new Vector2(-36, 42));
        defeat.TrackInsertKey(bodyPosition, 0.95, new Vector2(-46, 74));

        var bodyRotation = AddValueTrack(defeat, "BodyRoot:rotation_degrees");
        defeat.TrackInsertKey(bodyRotation, 0.0, 0.0f);
        defeat.TrackInsertKey(bodyRotation, 0.18, -4.0f);
        defeat.TrackInsertKey(bodyRotation, 0.5, -13.0f);
        defeat.TrackInsertKey(bodyRotation, 0.95, -18.0f);

        var bodyScale = AddValueTrack(defeat, "BodyRoot:scale");
        defeat.TrackInsertKey(bodyScale, 0.0, Vector2.One);
        defeat.TrackInsertKey(bodyScale, 0.5, new Vector2(1.0f, 0.94f));
        defeat.TrackInsertKey(bodyScale, 0.95, new Vector2(1.0f, 0.86f));

        var bodyColor = AddValueTrack(defeat, "BodyRoot:modulate");
        defeat.TrackInsertKey(bodyColor, 0.0, Colors.White);
        defeat.TrackInsertKey(bodyColor, 0.18, new Color(1.0f, 0.5f, 0.5f, 1.0f));
        defeat.TrackInsertKey(bodyColor, 0.5, new Color(0.75f, 0.75f, 0.8f, 0.82f));
        defeat.TrackInsertKey(bodyColor, 0.95, new Color(0.45f, 0.45f, 0.5f, 0.48f));

        var weaponVisible = AddValueTrack(defeat, "BodyRoot/FrontHand/WeaponSocket/WeaponRoot:visible");
        defeat.TrackInsertKey(weaponVisible, 0.0, true);
        defeat.TrackInsertKey(weaponVisible, 0.95, true);

        var hair = AddValueTrack(defeat, "BodyRoot/LayerRoot/Hair:position");
        defeat.TrackInsertKey(hair, 0.0, new Vector2(-500, -1488));
        defeat.TrackInsertKey(hair, 0.5, new Vector2(-518, -1472));
        defeat.TrackInsertKey(hair, 0.95, new Vector2(-526, -1456));

        return defeat;
    }

    private static Animation BuildMeditateAnimation()
    {
        var meditate = new Animation
        {
            Length = 2.4,
            LoopMode = Animation.LoopModeEnum.Linear
        };

        var bodyPosition = AddValueTrack(meditate, "BodyRoot:position");
        meditate.TrackInsertKey(bodyPosition, 0.0, new Vector2(0, 28));
        meditate.TrackInsertKey(bodyPosition, 1.2, new Vector2(0, 24));
        meditate.TrackInsertKey(bodyPosition, 2.4, new Vector2(0, 28));

        var bodyRotation = AddValueTrack(meditate, "BodyRoot:rotation_degrees");
        meditate.TrackInsertKey(bodyRotation, 0.0, 0.0f);
        meditate.TrackInsertKey(bodyRotation, 2.4, 0.0f);

        var bodyScale = AddValueTrack(meditate, "BodyRoot:scale");
        meditate.TrackInsertKey(bodyScale, 0.0, new Vector2(0.96f, 0.9f));
        meditate.TrackInsertKey(bodyScale, 1.2, new Vector2(0.97f, 0.88f));
        meditate.TrackInsertKey(bodyScale, 2.4, new Vector2(0.96f, 0.9f));

        var bodyColor = AddValueTrack(meditate, "BodyRoot:modulate");
        meditate.TrackInsertKey(bodyColor, 0.0, new Color(0.9f, 1.0f, 0.94f, 1.0f));
        meditate.TrackInsertKey(bodyColor, 1.2, new Color(0.78f, 1.0f, 0.86f, 1.0f));
        meditate.TrackInsertKey(bodyColor, 2.4, new Color(0.9f, 1.0f, 0.94f, 1.0f));

        var hand = AddValueTrack(meditate, "BodyRoot/FrontHand:position");
        meditate.TrackInsertKey(hand, 0.0, new Vector2(190, -880));
        meditate.TrackInsertKey(hand, 1.2, new Vector2(190, -884));
        meditate.TrackInsertKey(hand, 2.4, new Vector2(190, -880));

        var weaponVisible = AddValueTrack(meditate, "BodyRoot/FrontHand/WeaponSocket/WeaponRoot:visible");
        meditate.TrackInsertKey(weaponVisible, 0.0, false);
        meditate.TrackInsertKey(weaponVisible, 2.4, false);

        var hair = AddValueTrack(meditate, "BodyRoot/LayerRoot/Hair:position");
        meditate.TrackInsertKey(hair, 0.0, new Vector2(-500, -1488));
        meditate.TrackInsertKey(hair, 1.2, new Vector2(-500, -1490));
        meditate.TrackInsertKey(hair, 2.4, new Vector2(-500, -1488));

        return meditate;
    }

    private void PlayStartupAnimation()
    {
        var target = OS.GetEnvironment("WUXIA_GODOT_PREVIEW_TARGET").ToLowerInvariant();
        if (!string.IsNullOrEmpty(target) && target != "hero")
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
            case "meditate":
                PlayMeditate();
                break;
            default:
                PlayIdle();
                break;
        }
    }

    private void OnAnimationFinished(StringName animationName)
    {
        if (animationName != IdleAnimation && animationName != DefeatAnimation && animationName != MeditateAnimation)
        {
            PlayIdle();
        }
    }

    private static int AddValueTrack(Animation animation, string path)
    {
        var track = animation.AddTrack(Animation.TrackType.Value);
        animation.TrackSetPath(track, new NodePath(path));
        return track;
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
