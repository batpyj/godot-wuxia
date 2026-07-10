using System;
using Godot;

public partial class PixelFxPlayer : Node2D
{
    public static readonly StringName WeaponSlash = "weapon_slash";
    public static readonly StringName HitSpark = "hit_spark";
    public static readonly StringName HeavyImpact = "heavy_impact";
    public static readonly StringName InnerGuard = "inner_guard";
    public static readonly StringName ShaolinGoldBody = "shaolin_gold_body";
    public static readonly StringName TaijiSpiral = "taiji_spiral";
    public static readonly StringName SixMeridianSword = "six_meridian_sword";
    public static readonly StringName AncientTombMoon = "ancient_tomb_moon";
    public static readonly StringName RomanceAssist = "romance_assist";

    private AnimatedSprite2D _sprite = null!;

    public override void _Ready()
    {
        _sprite = new AnimatedSprite2D
        {
            Name = "AnimatedSprite",
            Centered = true,
            Visible = false,
            TextureFilter = TextureFilterEnum.Nearest,
            ZIndex = 1,
            SpriteFrames = BuildSpriteFrames()
        };
        _sprite.AnimationFinished += OnAnimationFinished;
        AddChild(_sprite);
    }

    public void PlayWeaponSlash(Vector2 position, bool facingRight)
    {
        Play(WeaponSlash, position, new Vector2(facingRight ? 3.6f : -3.6f, 3.6f), facingRight ? -8.0f : 8.0f);
    }

    public void PlayHitSpark(Vector2 position)
    {
        Play(HitSpark, position, new Vector2(3.2f, 3.2f), 0.0f);
    }

    public void PlayHeavyImpact(Vector2 position)
    {
        Play(HeavyImpact, position, new Vector2(3.6f, 3.6f), 0.0f);
    }

    public void PlayInnerGuard(Vector2 position)
    {
        Play(InnerGuard, position, new Vector2(4.2f, 4.2f), 0.0f);
    }

    public void PlayShaolinGoldBody(Vector2 position)
    {
        Play(ShaolinGoldBody, position, new Vector2(4.2f, 4.2f), 0.0f);
    }

    public void PlayTaijiSpiral(Vector2 position)
    {
        Play(TaijiSpiral, position, new Vector2(4.3f, 4.3f), 0.0f);
    }

    public void PlaySixMeridianSword(Vector2 position, bool facingRight)
    {
        Play(SixMeridianSword, position, new Vector2(facingRight ? 4.5f : -4.5f, 4.5f), -8.0f);
    }

    public void PlayAncientTombMoon(Vector2 position)
    {
        Play(AncientTombMoon, position, new Vector2(4.0f, 4.0f), 0.0f);
    }

    public void PlayRomanceAssist(Vector2 position)
    {
        Play(RomanceAssist, position, new Vector2(4.1f, 4.1f), 0.0f);
    }

    public void PlayPreview(string fxName, Vector2 heroPosition, Vector2 enemyPosition, bool enemyIsBoss)
    {
        switch (fxName.ToLowerInvariant())
        {
            case "weapon":
            case "weapon_slash":
            case "slash":
                PlayWeaponSlash(new Vector2((heroPosition.X + enemyPosition.X) * 0.5f, 560), facingRight: true);
                break;
            case "hit":
            case "hit_spark":
            case "spark":
                PlayHitSpark(enemyPosition + new Vector2(enemyIsBoss ? -8 : -18, -250));
                break;
            case "heavy":
            case "heavy_impact":
            case "impact":
                PlayHeavyImpact(enemyPosition + new Vector2(enemyIsBoss ? -12 : -18, -245));
                break;
            case "guard":
            case "inner":
            case "inner_guard":
                PlayInnerGuard(heroPosition + new Vector2(0, -300));
                break;
            case "shaolin":
            case "shaolin_gold":
            case "shaolin_gold_body":
            case "gold_body":
                PlayShaolinGoldBody(heroPosition + new Vector2(0, -300));
                break;
            case "taiji":
            case "taiji_spiral":
            case "taichi":
                PlayTaijiSpiral(heroPosition + new Vector2(0, -295));
                break;
            case "six_meridian":
            case "six_meridian_sword":
            case "liumai":
                PlaySixMeridianSword(new Vector2((heroPosition.X + enemyPosition.X) * 0.5f, 540), facingRight: true);
                break;
            case "ancient_tomb":
            case "ancient_tomb_moon":
            case "cold_moon":
            case "moon":
                PlayAncientTombMoon(enemyPosition + new Vector2(enemyIsBoss ? -22 : -28, -330));
                break;
            case "romance":
            case "romance_assist":
            case "partner":
                PlayRomanceAssist(heroPosition + new Vector2(150, -285));
                break;
            default:
                PlayWeaponSlash(new Vector2((heroPosition.X + enemyPosition.X) * 0.5f, 560), facingRight: true);
                break;
        }
    }

    private void Play(StringName animationName, Vector2 position, Vector2 scale, float rotationDegrees)
    {
        Position = position;
        Scale = scale;
        RotationDegrees = rotationDegrees;
        Visible = true;
        _sprite.Visible = true;
        _sprite.Play(animationName);
    }

    private void OnAnimationFinished()
    {
        _sprite.Visible = false;
    }

    private static SpriteFrames BuildSpriteFrames()
    {
        var frames = new SpriteFrames();
        ConfigureAnimation(frames, WeaponSlash, 18.0, BuildWeaponSlashFrames(), loop: false);
        ConfigureAnimation(frames, HitSpark, 16.0, BuildHitSparkFrames(), loop: false);
        ConfigureAnimation(frames, HeavyImpact, 14.0, BuildHeavyImpactFrames(), loop: false);
        ConfigureAnimation(frames, InnerGuard, 12.0, BuildInnerGuardFrames(), loop: true);
        ConfigureAnimation(frames, ShaolinGoldBody, 12.0, BuildShaolinGoldBodyFrames(), loop: true);
        ConfigureAnimation(frames, TaijiSpiral, 12.0, BuildTaijiSpiralFrames(), loop: true);
        ConfigureAnimation(frames, SixMeridianSword, 16.0, BuildSixMeridianSwordFrames(), loop: false);
        ConfigureAnimation(frames, AncientTombMoon, 14.0, BuildAncientTombMoonFrames(), loop: false);
        ConfigureAnimation(frames, RomanceAssist, 12.0, BuildRomanceAssistFrames(), loop: false);
        return frames;
    }

    private static void ConfigureAnimation(SpriteFrames spriteFrames, StringName name, double fps, Texture2D[] textures, bool loop)
    {
        if (!spriteFrames.HasAnimation(name))
        {
            spriteFrames.AddAnimation(name);
        }

        spriteFrames.SetAnimationLoopMode(name, loop ? SpriteFrames.LoopMode.Linear : SpriteFrames.LoopMode.None);
        spriteFrames.SetAnimationSpeed(name, fps);

        foreach (var texture in textures)
        {
            spriteFrames.AddFrame(name, texture);
        }
    }

    private static Texture2D[] BuildWeaponSlashFrames()
    {
        var frames = new Texture2D[6];
        for (var frame = 0; frame < frames.Length; frame++)
        {
            frames[frame] = MakeTexture(96, 96, image =>
            {
                var opacity = 1.0f - frame * 0.14f;
                var hot = new Color(1.0f, 0.95f, 0.45f, opacity);
                var core = new Color(0.75f, 0.95f, 1.0f, opacity);
                var edge = new Color(0.15f, 0.55f, 0.85f, opacity * 0.75f);

                var startX = 18 + frame * 4;
                var startY = 64 - frame * 5;
                var endX = 76 + frame * 2;
                var endY = 24 + frame * 3;

                DrawLine(image, startX, startY, endX, endY, 3, edge);
                DrawLine(image, startX + 3, startY - 2, endX - 4, endY + 2, 2, core);
                DrawLine(image, startX + 10, startY - 5, endX - 12, endY + 5, 1, hot);

                if (frame is >= 1 and <= 3)
                {
                    DrawRect(image, startX - 4, startY + 2, 4, 4, new Color(1.0f, 0.75f, 0.3f, opacity));
                    DrawRect(image, endX - 2, endY - 2, 3, 3, new Color(1.0f, 1.0f, 0.75f, opacity));
                }
            });
        }

        return frames;
    }

    private static Texture2D[] BuildHitSparkFrames()
    {
        var frames = new Texture2D[5];
        for (var frame = 0; frame < frames.Length; frame++)
        {
            frames[frame] = MakeTexture(64, 64, image =>
            {
                var opacity = 1.0f - frame * 0.18f;
                var center = 32;
                var reach = 8 + frame * 6;
                var hot = new Color(1.0f, 0.95f, 0.42f, opacity);
                var ember = new Color(1.0f, 0.4f, 0.12f, opacity);
                var red = new Color(0.85f, 0.12f, 0.08f, opacity * 0.8f);

                DrawLine(image, center - reach, center, center + reach, center, 2, ember);
                DrawLine(image, center, center - reach, center, center + reach, 2, ember);
                DrawLine(image, center - reach / 2, center - reach / 2, center + reach / 2, center + reach / 2, 1, hot);
                DrawLine(image, center - reach / 2, center + reach / 2, center + reach / 2, center - reach / 2, 1, hot);

                DrawRect(image, center - 2, center - 2, 4, 4, hot);
                DrawRect(image, center - reach - 3, center - 2, 3, 3, red);
                DrawRect(image, center + reach, center + 2, 3, 3, red);
                DrawRect(image, center + frame * 2 - 6, center - reach, 3, 3, ember);
                DrawRect(image, center - frame * 2 + 4, center + reach, 3, 3, ember);
            });
        }

        return frames;
    }

    private static Texture2D[] BuildHeavyImpactFrames()
    {
        var frames = new Texture2D[7];
        for (var frame = 0; frame < frames.Length; frame++)
        {
            frames[frame] = MakeTexture(96, 96, image =>
            {
                var opacity = 1.0f - frame * 0.11f;
                var radius = 10 + frame * 5;
                var core = new Color(1.0f, 0.86f, 0.38f, opacity);
                var orange = new Color(1.0f, 0.36f, 0.12f, opacity * 0.9f);
                var dark = new Color(0.42f, 0.12f, 0.08f, opacity * 0.7f);

                DrawDiamondOutline(image, 48, 48, radius, 2, orange);
                DrawLine(image, 48 - radius - 12, 50, 48 + radius + 12, 50, 2, dark);
                DrawLine(image, 48 - radius, 45, 48 + radius, 45, 1, core);

                DrawRect(image, 48 - radius - 6, 42 - frame, 5, 4, orange);
                DrawRect(image, 48 + radius + 2, 53 + frame, 5, 4, orange);
                DrawRect(image, 44 - frame, 48 - radius - 5, 4, 4, core);
                DrawRect(image, 50 + frame, 48 + radius + 1, 4, 4, dark);
            });
        }

        return frames;
    }

    private static Texture2D[] BuildInnerGuardFrames()
    {
        var frames = new Texture2D[8];
        for (var frame = 0; frame < frames.Length; frame++)
        {
            frames[frame] = MakeTexture(96, 96, image =>
            {
                var opacity = 0.82f - frame * 0.07f;
                var radius = 25 + frame % 4 * 3;
                var jade = new Color(0.25f, 1.0f, 0.65f, opacity);
                var teal = new Color(0.12f, 0.62f, 0.78f, opacity * 0.85f);
                var pale = new Color(0.8f, 1.0f, 0.72f, opacity);

                DrawBrokenCircle(image, 48, 48, radius, frame, 2, jade);
                DrawBrokenCircle(image, 48, 48, radius - 9, frame + 2, 1, teal);
                DrawRect(image, 47, 48 - radius - 3, 3, 5, pale);
                DrawRect(image, 47, 48 + radius - 2, 3, 5, pale);
                DrawRect(image, 48 - radius - 3, 47, 5, 3, teal);
                DrawRect(image, 48 + radius - 2, 47, 5, 3, teal);
            });
        }

        return frames;
    }

    private static Texture2D[] BuildShaolinGoldBodyFrames()
    {
        var frames = new Texture2D[8];
        for (var frame = 0; frame < frames.Length; frame++)
        {
            frames[frame] = MakeTexture(96, 96, image =>
            {
                var pulse = frame % 4;
                var radius = 25 + pulse * 2;
                var gold = new Color(1.0f, 0.78f, 0.18f, 0.84f);
                var pale = new Color(1.0f, 0.96f, 0.58f, 0.92f);
                var bronze = new Color(0.72f, 0.38f, 0.08f, 0.7f);

                DrawDiamondOutline(image, 48, 48, radius, 2, gold);
                DrawDiamondOutline(image, 48, 48, radius - 10, 1, pale);
                DrawLine(image, 48, 48 - radius - 8, 48, 48 + radius + 8, 2, bronze);
                DrawLine(image, 48 - radius - 8, 48, 48 + radius + 8, 48, 2, bronze);

                DrawRect(image, 44, 36, 8, 5, pale);
                DrawRect(image, 41, 47, 14, 4, gold);
                DrawRect(image, 45, 57, 6, 5, pale);
                DrawRect(image, 48 - radius - 8 + frame, 44, 4, 4, gold);
                DrawRect(image, 48 + radius + 4 - frame, 49, 4, 4, gold);
            });
        }

        return frames;
    }

    private static Texture2D[] BuildTaijiSpiralFrames()
    {
        var frames = new Texture2D[8];
        for (var frame = 0; frame < frames.Length; frame++)
        {
            frames[frame] = MakeTexture(96, 96, image =>
            {
                var dark = new Color(0.12f, 0.18f, 0.18f, 0.86f);
                var light = new Color(0.82f, 0.96f, 0.9f, 0.9f);
                var jade = new Color(0.26f, 0.85f, 0.62f, 0.68f);
                var offset = frame * Math.PI / 4.0;

                DrawArc(image, 48, 48, 28, offset, offset + Math.PI * 1.18, 3, light);
                DrawArc(image, 48, 48, 18, offset + Math.PI, offset + Math.PI * 2.16, 3, dark);
                DrawArc(image, 48, 48, 34, offset + Math.PI * 0.18, offset + Math.PI * 1.04, 1, jade);
                DrawArc(image, 48, 48, 10, offset + Math.PI * 1.12, offset + Math.PI * 1.92, 1, jade);

                DrawRect(image, 45 + (int)Math.Round(Math.Cos(offset) * 13), 45 + (int)Math.Round(Math.Sin(offset) * 13), 5, 5, light);
                DrawRect(image, 45 + (int)Math.Round(Math.Cos(offset + Math.PI) * 13), 45 + (int)Math.Round(Math.Sin(offset + Math.PI) * 13), 5, 5, dark);
            });
        }

        return frames;
    }

    private static Texture2D[] BuildSixMeridianSwordFrames()
    {
        var frames = new Texture2D[6];
        for (var frame = 0; frame < frames.Length; frame++)
        {
            frames[frame] = MakeTexture(128, 64, image =>
            {
                var opacity = 1.0f - frame * 0.13f;
                var cyan = new Color(0.45f, 0.95f, 1.0f, opacity);
                var blue = new Color(0.16f, 0.46f, 0.95f, opacity * 0.78f);
                var gold = new Color(1.0f, 0.86f, 0.28f, opacity);
                var startX = 6 + frame * 8;

                DrawLine(image, startX, 31, 120, 20, 2, cyan);
                DrawLine(image, startX - 4, 38, 112, 34, 1, blue);
                DrawLine(image, startX + 2, 25, 116, 12, 1, gold);
                DrawLine(image, startX + 6, 44, 104, 47, 1, cyan);
                DrawLine(image, startX + 12, 18, 96, 9, 1, blue);
                DrawLine(image, startX + 16, 34, 124, 29, 1, gold);

                DrawRect(image, 116, 18, 4, 4, cyan);
                DrawRect(image, 108, 32, 4, 4, gold);
                DrawRect(image, 98, 45, 3, 3, blue);
            });
        }

        return frames;
    }

    private static Texture2D[] BuildAncientTombMoonFrames()
    {
        var frames = new Texture2D[7];
        for (var frame = 0; frame < frames.Length; frame++)
        {
            frames[frame] = MakeTexture(96, 96, image =>
            {
                var opacity = 1.0f - frame * 0.11f;
                var moon = new Color(0.82f, 0.92f, 1.0f, opacity);
                var cold = new Color(0.32f, 0.64f, 0.96f, opacity * 0.78f);
                var shadow = new Color(0.12f, 0.22f, 0.44f, opacity * 0.55f);
                var lift = frame * 3;

                DrawArc(image, 48, 48 - lift, 30, Math.PI * 0.1, Math.PI * 1.45, 3, moon);
                DrawArc(image, 55, 47 - lift, 22, Math.PI * 0.16, Math.PI * 1.35, 3, shadow);
                DrawLine(image, 22 + frame * 4, 62 - lift, 76, 34 - lift, 1, cold);
                DrawRect(image, 72 - frame * 2, 28 - lift, 4, 4, moon);
                DrawRect(image, 20 + frame * 3, 64 - lift, 3, 3, cold);
            });
        }

        return frames;
    }

    private static Texture2D[] BuildRomanceAssistFrames()
    {
        var frames = new Texture2D[8];
        for (var frame = 0; frame < frames.Length; frame++)
        {
            frames[frame] = MakeTexture(96, 96, image =>
            {
                var opacity = 1.0f - frame * 0.08f;
                var rose = new Color(1.0f, 0.42f, 0.68f, opacity);
                var gold = new Color(1.0f, 0.78f, 0.28f, opacity * 0.9f);
                var pale = new Color(1.0f, 0.86f, 0.9f, opacity);
                var shift = frame * 2;

                DrawRect(image, 30, 28 - shift / 2, 8, 18, rose);
                DrawRect(image, 58, 30 - shift / 2, 8, 18, gold);
                DrawLine(image, 37, 42 - shift / 2, 59, 43 - shift / 2, 2, pale);
                DrawLine(image, 39, 44 - shift / 2, 57, 45 - shift / 2, 1, gold);

                DrawRect(image, 25 + frame, 22, 4, 4, rose);
                DrawRect(image, 69 - frame, 24, 4, 4, gold);
                DrawRect(image, 46, 18 + frame % 3, 5, 5, pale);
                DrawRect(image, 43, 58 - frame, 4, 4, rose);
                DrawRect(image, 51, 60 - frame, 4, 4, gold);
            });
        }

        return frames;
    }

    private static Texture2D MakeTexture(int width, int height, Action<Image> draw)
    {
        var image = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);
        image.Fill(Colors.Transparent);
        draw(image);
        return ImageTexture.CreateFromImage(image);
    }

    private static void DrawRect(Image image, int x, int y, int width, int height, Color color)
    {
        for (var py = y; py < y + height; py++)
        {
            for (var px = x; px < x + width; px++)
            {
                SetPixelSafe(image, px, py, color);
            }
        }
    }

    private static void DrawLine(Image image, int x0, int y0, int x1, int y1, int thickness, Color color)
    {
        var dx = Math.Abs(x1 - x0);
        var sx = x0 < x1 ? 1 : -1;
        var dy = -Math.Abs(y1 - y0);
        var sy = y0 < y1 ? 1 : -1;
        var error = dx + dy;

        while (true)
        {
            DrawRect(image, x0 - thickness / 2, y0 - thickness / 2, thickness, thickness, color);
            if (x0 == x1 && y0 == y1)
            {
                break;
            }

            var error2 = 2 * error;
            if (error2 >= dy)
            {
                error += dy;
                x0 += sx;
            }

            if (error2 <= dx)
            {
                error += dx;
                y0 += sy;
            }
        }
    }

    private static void DrawDiamondOutline(Image image, int centerX, int centerY, int radius, int thickness, Color color)
    {
        DrawLine(image, centerX, centerY - radius, centerX + radius, centerY, thickness, color);
        DrawLine(image, centerX + radius, centerY, centerX, centerY + radius, thickness, color);
        DrawLine(image, centerX, centerY + radius, centerX - radius, centerY, thickness, color);
        DrawLine(image, centerX - radius, centerY, centerX, centerY - radius, thickness, color);
    }

    private static void DrawArc(Image image, int centerX, int centerY, int radius, double startAngle, double endAngle, int thickness, Color color)
    {
        var steps = Math.Max(8, (int)Math.Ceiling(Math.Abs(endAngle - startAngle) * radius / 2.2));
        for (var i = 0; i <= steps; i++)
        {
            var t = i / (double)steps;
            var angle = startAngle + (endAngle - startAngle) * t;
            var px = centerX + (int)Math.Round(Math.Cos(angle) * radius);
            var py = centerY + (int)Math.Round(Math.Sin(angle) * radius);
            DrawRect(image, px - thickness / 2, py - thickness / 2, thickness, thickness, color);
        }
    }

    private static void DrawBrokenCircle(Image image, int centerX, int centerY, int radius, int frameOffset, int thickness, Color color)
    {
        for (var i = 0; i < 48; i++)
        {
            if ((i + frameOffset) % 6 is 0 or 1)
            {
                continue;
            }

            var angle = (Math.PI * 2.0 * i) / 48.0;
            var px = centerX + (int)Math.Round(Math.Cos(angle) * radius);
            var py = centerY + (int)Math.Round(Math.Sin(angle) * radius);
            DrawRect(image, px - thickness / 2, py - thickness / 2, thickness, thickness, color);
        }
    }

    private static void SetPixelSafe(Image image, int x, int y, Color color)
    {
        if (x < 0 || y < 0 || x >= image.GetWidth() || y >= image.GetHeight())
        {
            return;
        }

        image.SetPixel(x, y, color);
    }
}
