# Godot 动画迁移落地方案

目标：把当前网页端武侠挂机游戏的战斗表现，逐步迁移到 Godot。网页端仍然作为当前可玩的正式版本；Godot 项目先作为战斗动画、角色比例、背景接地和后续 UI 迁移的验证场。

## 当前环境

- Godot .NET 版本：`4.7.stable.mono.official.5b4e0cb0f`
- .NET SDK：`9.0.315`
- ffmpeg：`8.1.2`
- Godogen/Codex 运行时：已部署到本目录
- 资产生成 API key：当前不接入，先不用 `GOOGLE_API_KEY`、`XAI_API_KEY`、`TRIPO3D_API_KEY`
- 文本编码：所有文本文件统一为 UTF-8 + LF，由 `.editorconfig` 和 `.gitattributes` 约束，可用 `tools/verify-utf8.ps1` 检查

## 当前进度

- `V13G.0`：已完成 Godot C# 项目骨架、最小主场景、编码约束和 headless 验证。
- `V13G.1`：已完成首批网页端资源桥接，包含背景、男女主、敌人、Boss、基础武器和来源清单。
- `V13G.2`：已完成第一版静态战斗画面，主场景已能加载背景、主角、敌人和基础剑，并可输出本地截图。
- `V13G.3`：已完成 `HeroRig.tscn` 临时整图版，包含 `GroundAnchor`、`BodyRoot`、`FrontHand`、`WeaponSocket`、`WeaponRoot`、`FxRoot` 和 `AnimationPlayer`。
- `V13G.4`：已完成第一版 `idle` 呼吸动画，使用 `AnimationPlayer` 运行时创建 `idle` 轨道，并已导出本地预览 GIF。
- `V13G.5`：已完成男主第一版可重组粗分层，`recomposed.png` 与原图像素差异为 `AE=0 / RMSE=0`，并已接入 `HeroRig`。
- `V13G.6`：已完成分层 idle 工程版，腿和脚没有动画轨道，上身、头发、头部和手部做轻微呼吸，并已导出本地预览 GIF。
- `V13G.7`：已完成 `attack`、`graze`、`hit` 三个工程动作。`attack` 为单次出手，`graze` 为小伤红闪和小震，`hit` 为重伤后仰和红闪，动作结束后回到 `idle`。
- `V13G.8`：已完成 `defeat` 和 `meditate` 工程动作。`defeat` 会下沉、后仰、淡出并保持结束状态；`meditate` 会隐藏武器、进入低姿态并循环呼吸。
- `V13G.9`：已完成 `EnemyRig.tscn` 和 `EnemyRig.cs` 简化敌方 rig。普通敌人和 Boss 已接入 `idle`、`attack`、`graze`、`hit`、`defeat`，Boss 先带独立低透明压迫气场。
- `V13G.10.1`：已完成第一版 Godot 像素 FX 管线，使用 `PixelFxPlayer` 运行时生成 `AnimatedSprite2D` 硬边像素帧，先落地武器斩击、命中火花、重击冲击、内功护体。
- `V13G.10.2`：已完成门派、武功、情缘代表 FX 映射，包含少林金身、太极旋流、六脉气剑、古墓冷月、情缘援手。
- `V14G.1`：已完成第一版顶部战斗状态条，使用 Godot `Control` UI 显示层数、武学境界、方向、双方血量、战力和预览状态。
- `V14G.2`：已完成第一版左侧角色信息面板，显示头像、姓名、门派、气血、综合状态、战斗数值和资源材料。
- `V14G.3`：已完成第一版右侧信息面板，显示闯荡、门派、功法、武器只读状态。
- `V14G.4`：已完成第一版底部随身功能卡，江湖、门派、情缘、功法、武器 5 个模块并列显示。
- `V14G.5`：已完成第一版游戏内弹窗系统，遮罩只覆盖中间战斗层，支持只读详情弹窗和确认/取消按钮。
- `V14G.6`：已完成第一版游戏内存档入口，包含导入、导出、清空存档按钮和三类弹窗预览。
- `V15G.0`：已完成第一版 `GameState` / `SaveManager`，支持导出、导入校验、写入本地 `user://` 存档和清空本地存档。
- `V15G.1`：已完成左右侧栏、顶部状态条和底部功能卡的 `GameState` 刷新接入；导入/清空存档后会广播状态变更并重建 UI。
- `V15G.2`：已扩展 `GameState v15g.1`，纳入战斗数值、资源、资质、门派加成、功法、情缘和武器评分，并补导入刷新验证脚本。
- `V15G.3`：已新增状态命令层，右侧闯荡按钮和底部江湖/功法/情缘/武器卡片可以通过命令修改 `GameState` 并刷新 UI。
- `V15G.4`：已新增状态 dump/断言、网页存档桥接脚本，并让命令结果联动到主角动作、武器贴图和像素 FX。
- `V15G.5`：已新增 `combat` 稳定状态、`combat_tick` 真实扣血结算、网页存档字段覆盖报告和 V15G.5 命令预览验证。
- `V15G.6`：已把战斗结算接入自动战斗循环，新增战斗事件日志、网页迁移警告显示和自动战斗验证脚本。
- `V15G.7`：已新增结构化战斗事件队列，自动战斗击败敌人后会推进层数并生成下一名敌人，右侧 UI 优先读取事件队列。
- `V15G.8`：已把层数推进事件绑定到敌方视觉刷新和入场节奏，新敌人会按楼层切换 body/weapon profile 并播放入场动画。
- `V15G.9`：已新增战斗表现时间轴门控，自动战斗会等待命中恢复、击败停顿、奖励结算、新敌人入场和下一回合准备节拍结束后再继续 tick。
- `V15G.10`：已把击败结算接到真实资源奖励、掉落、门派/江湖提示、奖励飞字和右侧战利事件 UI。
- `V15G.11`：已把战斗表现时间轴抽成 `CombatPresentationTimeline`，并把奖励、掉落、门派和江湖提示规则迁入 `data/reward-rules.json`。
- `V15G.12`：已把敌人视觉 profile、武器循环、门派/奇遇飞字表现和 Boss 节奏迁入 `data/combat-presentation-rules.json`。
- 下一步：进入 `V15G.13`，把奖励飞字从 `BattleScene` 抽成可复用表现层，并准备 Boss/门派/奇遇专属战斗表现钩子。

## 总原则

- 先迁移战斗动画验证，不急着完整复刻网页 UI。
- 人物本体不带武器，武器永远独立挂到 `WeaponSocket`。
- 常规人物动作优先使用 Godot 分层 rig 和 `AnimationPlayer`，避免再依赖不稳定的整张 AI 帧动画。
- 主角先做完整 rig；普通敌人先做简化 rig；Boss 后续单独精修。
- 动画必须有明确动作状态：`idle`、`attack`、`graze`、`hit`、`defeat`、`meditate`。
- 低于最大生命 15% 的单次伤害播放 `graze`；高于或等于 15% 播放 `hit`。
- 背景、人物比例和脚底锚点必须统一，人物要像踩在地面上。
- 特效独立做像素风资产或粒子，不画进人物身体层。

## 目录规划

```text
project.godot
wuxia-godot-godogen.csproj
scenes/
  BattleScene.tscn
  actors/HeroRig.tscn
  actors/EnemyRig.tscn
scripts/
  BattleScene.cs
  ActorRig.cs
  HeroRig.cs
  WeaponSocket.cs
  AnimationStateDriver.cs
assets/
  backgrounds/
  actors/hero/
  actors/enemies/
  weapons/
  fx/
data/
  art-rig.json
  combat-sample.json
screenshots/
tools/
  godot-env.ps1
  verify-godot.ps1
```

## V13G.0 项目骨架

目标：建立可编译、可导入、可用 Godot headless 验证的 C# 项目。

交付：
- `project.godot`
- `wuxia-godot-godogen.csproj`
- `scripts/BattleScene.cs`
- `scenes/BattleScene.tscn`
- `data/combat-sample.json`
- `.editorconfig`

实施：
1. 使用 Godot 4.7 .NET 和 .NET 9 创建项目骨架。
2. 设置窗口尺寸为横向战斗画面：`1600 x 900`。
3. 创建最小 `BattleScene.tscn`。
4. 创建 `BattleScene.cs`，只做启动验证，不放最终 UI。
5. 设置 C# assembly 名称，避免 Godot 找不到脚本类。
6. 统一文本编码为 UTF-8 + LF。
7. 跑 `dotnet build` 和 Godot headless 导入验证。

验收：
- `powershell -ExecutionPolicy Bypass -File tools\verify-godot.ps1` 通过。
- `dotnet build` 通过。
- Godot headless `--import` 和 `--quit` 通过。
- Godot 编辑器能打开项目。

## V13G.1 资源桥接

目标：把网页端已有美术资源以只读复制方式接入 Godot 项目，不改动原网页项目。

优先资源：
- 战斗背景
- 男主参考图
- 女主参考图
- 普通敌人参考图
- 武器图片
- V13 初始姿态参考图

实施：
1. 把必要资源复制到 Godot `assets/` 下，不直接引用网页项目路径。
2. 记录资源来源，避免以后不知道哪张图来自哪里。
3. 背景先只接一张，先验证比例和脚底接地。
4. 武器按类型建目录：`sword`、`blade`、`staff`、`spear`、`hidden`、`fan`、`flute`。
5. 建立 `data/art-rig.json`，记录脚底锚点、角色缩放、武器 socket。

验收：
- Godot importer 能识别资源。
- 资源不缺失、不重名冲突。
- 原网页项目资源没有被移动或删除。

## V13G.2 静态战斗画面

目标：先在 Godot 中复现一张静态战斗画面，验证背景、人物比例、站位和接地。

交付：
- 背景 `Sprite2D`
- 主角临时 `Sprite2D`
- 敌人临时 `Sprite2D`
- 固定 viewport 布局

实施：
1. 背景放入 `BattleScene`。
2. 主角和敌人先用整张参考图。
3. 只调站位和缩放，不做动作。
4. 脚底锚点对齐地面。
5. 记录主角、敌人、武器 socket 的 Godot 坐标。

验收：
- 人物像站在地面上，不飘。
- 人物比例和背景一致。
- 主角和敌人有足够战斗空间。
- 不出现人物站在画面底部阴影里的问题。

## V13G.3 HeroRig 临时整图版

目标：在 Godot 里先建立主角 rig 节点结构，即使暂时只显示整张身体图。

节点结构：

```text
HeroRig Node2D
  GroundAnchor Marker2D
  BodyRoot Node2D
    FullBody Sprite2D
    FrontHand Marker2D
      WeaponSocket Marker2D
  WeaponRoot Node2D
    Weapon Sprite2D
  FxRoot Node2D
  AnimationPlayer
```

实施：
1. 创建 `HeroRig.tscn`。
2. 导入整张男主图作为 `FullBody`。
3. 设置 `GroundAnchor`。
4. 设置 `FrontHand` 和 `WeaponSocket`。
5. 把一把剑挂到 `WeaponSocket`。
6. 加入空 `AnimationPlayer`。

验收：
- 换武器只换 `Weapon Sprite2D`，不换人物图。
- 人物脚底锚点固定。
- 武器在手掌前方，不在脚底。
- 后续拆层时节点结构不用推翻。

## V13G.4 第一版 idle

目标：先做 Godot 版待机呼吸，但不追求最终分层精度。

方式：
- 临时阶段：`BodyRoot` 轻微缩放和上身错觉，脚底位置反向补偿。
- 正式阶段：拆层后只动胸腔、手臂、头发、发带、衣摆，脚和腿不动。

实施：
1. 在 `AnimationPlayer` 建 `idle`。
2. 循环时长先定为 2.8 秒。
3. 让 `BodyRoot` 做很小的呼吸变化。
4. 用脚底补偿保证 `GroundAnchor` 不动。
5. 导出短 GIF 或视频预览。

验收：
- 脚不漂移。
- 呼吸可见但不夸张。
- 不出现整个人上下浮动的感觉。
- 循环首尾自然。

## V13G.5 主角分层资源

目标：拆出 Godot 可控制的主角层。

优先层：
- `feet`
- `legs`
- `hips`
- `torso`
- `head`
- `hair`
- `ribbon`
- `frontArm`
- `backArm`
- `frontHand`
- `backHand`
- `robeFront`
- `robeBack`
- `sleeveFront`
- `sleeveBack`
- `tassel`

实施：
1. 以批准的男主姿态为基准。
2. 拆层时保留透明重叠区域。
3. 每层单独导出 PNG。
4. 在 Godot 中替换 `FullBody` 为多层 `Sprite2D`。
5. 调整 z-index 和 pivot。
6. 重组站姿和原姿态对齐。

验收：
- 重组后脸、身形、衣服身份不变。
- 没有断臂、断袖、关节裂缝。
- 透明边干净。
- 关闭任意层可以调试。

## V13G.6 分层 idle 正式版

目标：把 idle 从整图假动作替换为真实分层动作。

动作规则：
- 脚和腿完全不动。
- 胸腔上下 1-2 px。
- 前后手臂轻微跟随胸腔。
- 头发、发带、衣摆、吊坠有延迟摆动。
- 武器轻微随手呼吸，不抢戏。

验收：
- 脚底固定。
- 呼吸自然。
- 头发和飘带有生命感。
- 和网页端整图动画相比明显更稳定。

## V13G.7 attack / graze / hit

目标：做第一组战斗动作。

动作：
- `attack`：单次出手，不做多招连段。
- `graze`：小伤，红闪、轻震、轻微后挫。
- `hit`：重伤，上身后仰，手臂和衣摆延迟。

规则：
- 低于最大生命 15% 的单次伤害只播放 `graze`。
- 高于或等于最大生命 15% 才播放 `hit`。
- 攻击时脚可以有重心变化，但站位脚点不能滑。
- 武器跟随 `WeaponSocket`，拖尾和剑气属于 FX。

验收：
- `attack` 不是整张图平移。
- `hit` 不再是翻面或闪一下。
- `graze` 和 `hit` 能明显区分。
- 武器和手掌关系稳定。

## V13G.8 defeat / meditate

目标：补齐战斗常用状态。

`defeat`：
- 普通敌人可简化为下沉、后仰、淡出。
- 主角需要保留清晰结束姿势。

`meditate`：
- 隐藏武器。
- 身体进入低姿态或打坐姿态。
- 内功气息用独立 FX。

验收：
- 死亡不是突然消失。
- 打坐不是站姿加光圈。
- FX 不遮挡角色主体。

当前落地：
- 已在 `HeroRig` 接入 `defeat` 和 `meditate` 两个动作状态。
- `defeat` 当前为工程版：红闪后下沉、后仰、淡出，并停留在败退姿态，不自动回到 `idle`。
- `meditate` 当前为工程版：隐藏武器，身体压低并循环呼吸；正式盘坐姿态需要后续美术分层或逐帧资源补齐。
- 已输出预览：`screenshots/v13g8-defeat-preview.gif`、`screenshots/v13g8-meditate-preview.gif`。

## V13G.9 敌人和 Boss

目标：把主角流程复用到敌人。

普通敌人：
- 简化 rig：`body`、`head`、`arms`、`legs`、`cloth`。
- 先做 `idle`、`attack`、`graze`、`hit`、`defeat`。

Boss：
- 单独 rig。
- 披风、触须、护体气劲独立层。
- 可追加入场和破防演出。

验收：
- 敌人不再像纸片平移。
- Boss 有独立压迫感。
- 标签不挡角色头部和武器。

当前落地：
- 已新增 `scenes/actors/EnemyRig.tscn` 和 `scripts/EnemyRig.cs`。
- 普通敌人和 Boss 使用同一个 rig 入口，通过 profile 设置身体图、武器图、脚底锚点、武器挂点、缩放和 Boss 标记。
- 敌方动作已接入 `idle`、`attack`、`graze`、`hit`、`defeat`。`attack` 会向主角方向压进并带武器摆动；`graze` 为小伤红闪小震；`hit` 为重伤红闪后退；`defeat` 为下沉、后退、淡出并保持结束状态。
- `BattleScene` 已移除旧的静态敌人贴图路径，改为实例化 `EnemyRig.tscn`。
- 预览控制：`WUXIA_GODOT_PREVIEW_TARGET=enemy|boss`，`WUXIA_GODOT_PREVIEW_ACTION=attack|graze|hit|defeat`。
- 已输出预览：`screenshots/v13g9-enemy-attack-preview.gif`、`screenshots/v13g9-enemy-hit-preview.gif`、`screenshots/v13g9-boss-defeat-preview.gif`。
- 当前限制：敌人和 Boss 仍是整图工程 rig，不是最终语义分层。Boss 的披风、触须、护体气劲仍留到后续精修。

## V13G.10 像素 FX

目标：把特效改成 Godot 可控的像素风资产。

分类：
- 武器 FX：剑气、刀弧、枪线、棍风、扇风。
- 命中 FX：火花、冲击波、破防碎片。
- 内功 FX：护体、回气、吸收、反震。
- 门派 FX：少林金身、太极旋流、古墓冷月、桃花机关、六脉气剑。
- 情缘 FX：援手剪影、粉金粒子、牵手同心状态。

实现：
- `AnimatedSprite2D` 用于序列帧。
- `GPUParticles2D` 用于轻量粒子。
- Shader 只用于必要的闪白、描边、受击色调。

验收：
- 像素风统一。
- 不再出现柔光大圆圈占主体。
- 一眼能区分武器、门派、武功、情缘。

当前落地：
- 已新增 `scripts/PixelFxPlayer.cs` 和 `scenes/fx/PixelFxPlayer.tscn`。
- 已新增 `data/fx-catalog.json`，记录 FX ID、分类、默认锚点和后续可替换的资产路径。
- 当前四类基础 FX：
  - `fx.weapon_slash`：武器斩击，蓝白硬边弧线和少量金色热像素。
  - `fx.hit_spark`：小伤命中火花，黄橙十字火花和红色碎片。
  - `fx.heavy_impact`：重击冲击，菱形冲击线、横向震波和碎片块。
  - `fx.inner_guard`：内功护体，持续循环的破碎青绿色像素环。
- V13G.10.2 新增五类主题 FX：
  - `fx.shaolin_gold_body`：少林金身，金色菱形护体和铜色十字轴。
  - `fx.taiji_spiral`：太极旋流，黑白旋转断弧和青绿色辅助像素。
  - `fx.six_meridian_sword`：六脉气剑，六道青蓝金色像素气剑线。
  - `fx.ancient_tomb_moon`：古墓冷月，冷白月弧和蓝色残影。
  - `fx.romance_assist`：情缘援手，粉金双人剪影和牵手像素线。
- `BattleScene` 已支持 `WUXIA_GODOT_PREVIEW_FX=weapon_slash|hit_spark|heavy_impact|inner_guard|all`。
- `BattleScene` 额外支持 `WUXIA_GODOT_PREVIEW_FX=shaolin_gold_body|taiji_spiral|six_meridian_sword|ancient_tomb_moon|romance_assist|themes`。
- 如果未显式设置 `WUXIA_GODOT_PREVIEW_FX`，会根据 `WUXIA_GODOT_PREVIEW_ACTION` 推断：`attack -> weapon_slash`、`graze -> hit_spark`、`hit -> heavy_impact`、`meditate -> inner_guard`。
- 已输出预览：`screenshots/v13g10-fx-all-preview.gif`、`screenshots/v13g10-weapon-slash-preview.gif`、`screenshots/v13g10-inner-guard-preview.gif`。
- 已输出 V13G.10.2 主题预览：`screenshots/v13g10-2-theme-fx-all-preview.gif`、`screenshots/v13g10-2-six-meridian-preview.gif`、`screenshots/v13g10-2-ancient-tomb-moon-preview.gif`、`screenshots/v13g10-2-romance-assist-preview.gif`。

当前限制：
- V13G.10.1 是管线和工程美术首版，不是最终精修特效。
- V13G.10.2 已有代表效果，但仍是工程生成帧；正式版本需要手绘或工具产出的序列帧替换。
- `themes` 是同屏展示用，会比实战单项触发更拥挤，不作为最终战斗排版。
- 运行时生成帧后续可以替换为 `assets/fx/` 下的手绘 PNG 序列帧或 `.tres` SpriteFrames。

## V14G UI 迁移预案

V13G 只验证战斗动画，UI 暂时不完整迁移。

后续 UI 迁移顺序：
1. 顶部战斗状态条。
2. 左侧角色信息。
3. 右侧闯荡、门派、功法、武器面板。
4. 底部随身功能卡。
5. 弹窗系统。
6. 存档导入和导出。

UI 原则：
- 不先追求完全复刻网页 CSS。
- Godot UI 使用 `Control`、`PanelContainer`、`GridContainer`、`ScrollContainer`、`Button`、`RichTextLabel`。
- 先保证可读、稳定、不遮挡战斗画面。

### V14G.1 顶部战斗状态条

目标：先迁移顶部战斗状态信息，让 Godot 画面能显示当前层数、门派/武学境界、双方血量、战力和预览状态，同时不遮挡人物和 FX。

当前落地：
- 已新增 `scripts/CombatStatusBar.cs` 和 `scenes/ui/CombatStatusBar.tscn`。
- `CombatStatusBar` 读取 `data/combat-sample.json`，生成 Godot `Control` UI。
- 状态条包含：江湖层数、武学境界、江湖岁月、发展方向、我方气血、气血稳健、我方效果标签、综合战力、敌方气血、敌方战力、敌方标签、当前 `target/action/fx` 预览状态。
- `BattleScene` 已移除旧的临时 `MigrationStatus` debug 标签，改为加载正式 `CombatStatusBar.tscn`。
- 已输出预览：`screenshots/v14g1-combat-status-bar.png`、`screenshots/v14g1-combat-status-bar-preview.gif`。

验收：
- 状态条固定在顶部，不遮挡主角、敌人和 FX 主表现区。
- 动态 FX 播放时状态条不抖动、不改尺寸。
- 文字可读，血条和战力信息一眼能识别。
- UI 使用 Godot `Control` 节点，不依赖网页 CSS。

当前限制：
- 仍使用 `data/combat-sample.json` 样例数据，不是真实战斗状态绑定。
- 字体、图标和更细的中文排版还没有进入最终美术阶段。
- 左侧角色信息、右侧功能面板、底部功能卡和弹窗仍待迁移。

### V14G.2 左侧角色信息面板

目标：迁移网页端左侧侠客信息的第一版，让 Godot 画面能显示当前角色身份、头像、核心属性和资源，同时不遮挡中间战斗舞台。

当前落地：
- 已新增 `scripts/CharacterInfoPanel.cs` 和 `scenes/ui/CharacterInfoPanel.tscn`。
- `CharacterInfoPanel` 读取 `data/combat-sample.json` 的 `hero` 数据，生成 Godot `Control` UI。
- 面板固定在左侧，起点为 `x=16, y=140`，避开顶部战斗状态条。
- 面板包含：头像、姓名、身份、门派/境界、气血条、效果标签、综合状态、战斗数值、资源材料。
- `BattleScene` 已在顶部状态条之后加载 `CharacterInfoPanel.tscn`。
- `data/art-rig.json` 已登记 `characterInfoPanelScene` 和 `characterInfoPanelScript`。
- 已输出预览：`screenshots/v14g2-character-info-panel.png`。

验收：
- 左侧面板和顶部状态条不重叠。
- 面板不遮挡主角身体、武器和主要 FX。
- 头像能使用主角图片资源。
- 文字横向可读，效果标签不再被挤成竖排。
- UI 使用 Godot `Control` 节点，不依赖网页 CSS。

当前限制：
- 面板仍使用样例数据，不是真实存档状态绑定。
- 细节背景、图标、滚动条和最终中文字体仍未进入终版 UI 美术。
- 资质类信息在当前高度下需要滚动查看，后续可跟随右侧面板和底部卡片一起重排。

### V14G.3 右侧闯荡/门派/功法/武器面板

目标：迁移网页端右侧功能信息的第一版，只做只读状态卡，不先接按钮交互，保证敌人、武器和右侧 FX 不被挡住。

当前落地：
- 已新增 `scripts/RightInfoPanel.cs` 和 `scenes/ui/RightInfoPanel.tscn`。
- `data/combat-sample.json` 已升级到 `v14g.3`，新增 `rightPanel` 数据段。
- `rightPanel` 当前包含：
  - `adventure`：闯荡状态、路线、进度、当前说明。
  - `sect`：门派、身份、门派加成。
  - `martialArts`：当前代表功法、类型、突破进度、属性效果。
  - `weapon`：武器名、类型、评分、挂点说明、武器效果。
- `RightInfoPanel` 固定在右侧，起点由 viewport 宽度计算，宽度为 `270 px`，顶部从 `y=140` 开始。
- `BattleScene` 已在左侧角色面板后加载 `RightInfoPanel.tscn`。
- `data/art-rig.json` 已登记 `rightInfoPanelScene` 和 `rightInfoPanelScript`。
- 已输出预览：`screenshots/v14g3-right-info-panel.png`。

验收：
- 右侧面板和顶部状态条不重叠。
- 面板位于敌人主体外侧，不压住敌人身体和武器。
- 右侧 FX 主体仍能在战斗区显示。
- 闯荡、门派、功法、武器信息均可读。
- UI 使用 Godot `Control` 节点，不依赖网页 CSS。

当前限制：
- 只读信息卡先行，按钮交互、自动闯荡开关、功法切换、武器更换后续再接。
- 仍使用样例数据，不是真实存档绑定。
- 图标、卡片密度、滚动条样式和最终中文字体仍未进入终版 UI 美术。

### V14G.4 底部随身功能卡

目标：迁移网页端底部随身功能卡的第一版，把江湖、门派、情缘、功法、武器做成 5 个大模块并列，文字比网页细小卡片更清楚，同时保留中间战斗表现空间。

当前落地：
- 已新增 `scripts/BottomQuickBar.cs` 和 `scenes/ui/BottomQuickBar.tscn`。
- `data/combat-sample.json` 已升级到 `v14g.4`，新增 `bottomBar` 数组。
- `bottomBar` 当前包含 5 个只读模块：
  - 江湖：闯荡层数和当前方向。
  - 门派：门派身份和主要加成。
  - 情缘：当前情缘对象和援手状态。
  - 功法：已学功法数量和代表突破进度。
  - 武器：当前武器名、评分和代表效果。
- `BottomQuickBar` 固定在左右侧面板之间，使用 `x=286` 到 `x=1314` 的中间区域，底部留 `16 px` 间距。
- `BattleScene` 已在左右面板后加载 `BottomQuickBar.tscn`。
- `data/art-rig.json` 已登记 `bottomQuickBarScene` 和 `bottomQuickBarScript`。
- 已输出预览：`screenshots/v14g4-bottom-quick-bar.png`。

验收：
- 底部栏 5 个模块并列，信息一眼可读。
- 底部栏不覆盖主角和敌人的主体，不占用左右侧面板宽度。
- 中间战斗区仍保留主要 FX 表现空间。
- UI 使用 Godot `Control` 节点，不依赖网页 CSS。

当前限制：
- 模块目前是只读状态入口，点击展开、开关控制和详细弹窗留到后续。
- 仍使用样例数据，不是真实存档绑定。
- 最终图标、交互态、选中态和中文字体仍未进入终版 UI 美术。

### V14G.5 游戏内弹窗系统

目标：迁移网页端弹窗系统的第一版，建立 Godot 内统一遮罩、弹窗框、标题、正文、详情列表、确认/取消按钮。遮罩必须只覆盖中间战斗/动画层，不能把左右侧栏和底部功能卡全部压黑。

当前落地：
- 已新增 `scripts/GameModalLayer.cs` 和 `scenes/ui/GameModalLayer.tscn`。
- `data/combat-sample.json` 已升级到 `v14g.5`，新增 `modalPreview` 数据段。
- `modalPreview` 当前包含：上方标签、标题、正文、详情列表、确认按钮文案、取消按钮文案。
- `GameModalLayer` 常驻加载但默认隐藏，避免普通战斗预览被弹窗遮挡。
- 预览开关：设置 `WUXIA_GODOT_PREVIEW_MODAL=detail`、`confirm`、`on` 或 `true` 时显示样例弹窗。
- 遮罩范围固定为中间动画层：`x=286..1314`、`y=124..806`，避开左侧角色面板、右侧信息面板和底部随身功能卡。
- 弹窗面板居中显示，宽度 `620 px`，高度 `360 px`。
- `BattleScene` 已在全部 UI 后加载 `GameModalLayer.tscn`。
- `data/art-rig.json` 已登记 `gameModalLayerScene`、`gameModalLayerScript` 和 `modalPreviewEnvironment`。
- 已输出预览：`screenshots/v14g5-game-modal-layer.png`。

验收：
- 弹窗遮罩只压暗中间战斗层，不把左右侧栏和底部栏一起压黑。
- 弹窗标题、正文、详情列表、确认/取消按钮均可读。
- 弹窗层默认隐藏，不影响常规战斗预览。
- UI 使用 Godot `Control` 节点，不依赖网页 CSS。

当前限制：
- 按钮当前只关闭弹窗，真实存档导入、清空存档、奇遇选择和功法详情交互留到后续接状态机。
- 仍使用样例数据，不是真实存档绑定。
- 复杂事件选择弹窗、长文本滚动、危险操作二次确认还未细分。

### V14G.6 存档导入/导出入口

目标：迁移存档导入、导出和清空存档入口。导入流程不依赖浏览器文件弹窗，改为游戏内弹窗粘贴文本；清空存档必须使用危险操作二次确认样式。

当前落地：
- 已新增 `scripts/SaveActionBar.cs` 和 `scenes/ui/SaveActionBar.tscn`。
- `data/combat-sample.json` 已升级到 `v14g.6`，新增 `saveActions` 和 `saveDialogs`。
- `saveActions` 当前包含 3 个入口：
  - `import`：导入存档，打开粘贴存档文本弹窗。
  - `export`：导出存档，打开只读导出文本弹窗。
  - `clear`：清空存档，打开危险操作二次确认弹窗。
- `saveDialogs` 当前包含导入、导出、清空三类弹窗样例数据。
- `SaveActionBar` 固定在中间战斗区右上方，位置为 `x=904, y=130`，不占用左右侧栏。
- `GameModalLayer` 已扩展：
  - 支持 `WUXIA_GODOT_PREVIEW_MODAL=import|export|clear`。
  - 支持带输入框/只读文本框的弹窗。
  - 支持危险操作样式。
  - 暴露 `ShowPreset(key)`，供 `SaveActionBar` 按钮调用。
- `BattleScene` 已加载 `SaveActionBar.tscn`，并继续复用 `GameModalLayer.tscn`。
- `data/art-rig.json` 已登记 `saveActionBarScene` 和 `saveActionBarScript`。
- 已输出预览：
  - `screenshots/v14g6-save-import-modal.png`
  - `screenshots/v14g6-save-export-modal.png`
  - `screenshots/v14g6-save-clear-modal.png`

验收：
- 导入存档入口使用游戏内文本框，不依赖浏览器文件弹窗。
- 导出存档入口使用游戏内只读文本区域。
- 清空存档入口使用危险操作二次确认样式。
- 三类弹窗仍只遮罩中间战斗层，不压黑左右侧栏和底部功能卡。
- 普通战斗预览默认不显示弹窗。

当前限制：
- V15G.0 已补上第一版解析、写入、复制和删除流程；V14G.6 本身仍可视作 UI 入口阶段。
- 复杂版本迁移、字段级错误提示、回滚保护和真实网页存档兼容仍留到后续。
- 复制到剪贴板和本地文件读写策略还未定。

## V15G 状态和存档管理

V15G 开始把 V14G 的静态 UI 样例逐步接到真实状态。原则是先建立安全的状态管线，再一块一块替换 UI 数据来源。

### V15G.0 GameState / SaveManager

目标：建立 Godot 侧真实状态和存档管理器，让导入/导出/清空入口不再只是静态样式。

当前落地：
- 已新增 `scripts/GameState.cs`。
- 已新增 `scripts/SaveManager.cs`。
- `GameState` 当前包含最小可用状态：
  - `StageSnapshot`：层数、江湖章节、武学境界、年份、发展方向、综合战力。
  - `HeroSnapshot`：姓名、身份、门派、境界、武器、气血、气血稳健、综合战力、修为、修为速度。
  - `EnemySnapshot`：敌人名、层级、标签、气血、战力。
- `SaveManager` 当前支持：
  - 从 `user://wuxia-save-v15g0.json` 读取本地存档。
  - 没有本地存档时回退到 `data/combat-sample.json`。
  - 导出 `WUXIA_SAVE_V15G0::payload::checksum` 文本。
  - 导入时校验前缀、Base64、SHA-256 校验片段、存档版本和核心字段。
  - 校验通过后写入 Godot 本地 `user://` 存档。
  - 清空本地存档并回退到样例状态。
- `GameModalLayer` 已接入 `SaveManager`：
  - 导出弹窗显示真实导出文本，并支持复制到系统剪贴板。
  - 导入弹窗会调用 `SaveManager.TryImportSaveText`。
  - 清空弹窗会调用 `SaveManager.ClearLocalSave`。
  - 导入失败不会覆盖当前存档。
- `CombatStatusBar` 已优先读取 `SaveManager.CurrentState` 的核心状态；效果标签暂时仍沿用静态样例数据。
- `data/art-rig.json` 已登记 `gameStateScript`、`saveManagerScript`、`saveTextPrefix` 和 `localSavePath`。
- 已输出预览：`screenshots/v15g0-save-manager-export.png`。

验收：
- 导出弹窗里能看到 `WUXIA_SAVE_V15G0::` 开头的真实存档文本。
- 导入流程会进行前缀、校验和核心字段检查。
- 清空流程会删除 Godot 本地存档槽并回退到样例状态。
- 顶部状态条已经从 `SaveManager.CurrentState` 读取核心战斗状态。
- 现有 V14G UI 默认排版不被破坏。

当前限制：
- 左侧角色面板、右侧信息面板、底部随身功能卡在 V15G.1 已读取 `GameState` 的核心字段，战斗数值、资源、功法列表、情缘和武器评分仍暂时沿用样例 JSON。
- 导入/清空成功后已经通过 `SaveManager.StateChanged` 广播刷新事件；后续还需要补交互级自动化测试，验证导入不同存档后的字段变化。
- 真实网页端存档格式兼容、字段迁移和错误定位还没做。
- 当前校验使用短 SHA-256 片段，后续正式存档可以扩展完整签名或更强校验策略。

### V15G.1 UI 读取 GameState 和刷新事件

目标：让 V14G 的静态 UI 面板开始响应真实 Godot 存档状态，不再只展示样例 JSON。

当前落地：
- `SaveManager` 新增 `StateChanged` 事件。
- `SaveManager.SaveState` 在导入成功后写入本地存档，并广播当前 `GameState`。
- `SaveManager.ClearLocalSave` 删除本地存档、回退样例状态，并广播当前 `GameState`。
- `SaveManager.DescribeWeaponType` 统一把 `sword`、`blade`、`staff` 等武器枚举显示为中文。
- `CombatStatusBar` 订阅状态变化，层数、境界、方向、双方气血和战力会在存档变化后重建。
- `CharacterInfoPanel` 订阅状态变化，姓名、身份、门派、境界、气血、综合战力、修为和修为速度读取 `GameState`；头像、战斗数值、资源和资质暂时沿用样例 JSON。
- `RightInfoPanel` 订阅状态变化，路线、进度、门派、身份、武器类型和综合战力说明读取 `GameState`；功法列表和武器评分暂时沿用样例 JSON。
- `BottomQuickBar` 订阅状态变化，江湖层数/路线、门派身份、修为速度、武器类型和战力读取 `GameState`；情缘卡暂时沿用样例 JSON。
- `data/combat-sample.json` 和 `data/art-rig.json` 在 V15G.1 阶段曾标记到 `v15g.1`；V15G.2 后已升级到 `v15g.2`。存档传输协议仍保持 `WUXIA_SAVE_V15G0`。
- 已输出预览：`screenshots/v15g1-game-state-panels.png`。

验收：
- 项目可编译，`dotnet build` 无警告无错误。
- 正常 Windows 渲染器可输出 V15G.1 UI 预览图。
- 左侧、右侧、顶部和底部 UI 都存在 `SaveManager.StateChanged` 订阅和解绑。
- 导入成功与清空存档会触发同一个状态刷新事件，不需要重启场景才能刷新核心 UI。
- 原 V14G 排版不被破坏，底部仍保持 5 个大模块并列。

当前限制：
- V15G.2 已扩展 `GameState v15g.1`，但还没有把按钮点击、功法切换、情缘援手、武器更换做成真实状态命令。
- V15G.2 已有导入刷新截图脚本，但还没做像素/文本 OCR 级自动断言；当前验收以导入成功、截图可见字段变化和编译验证为主。
- 存档传输前缀仍保持 `WUXIA_SAVE_V15G0`，因为传输格式未变；稳定 payload schema 已从 `v15g.0` 迁移到 `v15g.1`，旧 `v15g.0` payload 会自动补默认字段。

### V15G.2 扩展状态字段和导入刷新验证

目标：把 V15G.1 仍沿用样例 JSON 的可变字段纳入 `GameState`，并提供可复测的导入刷新验证入口。

当前落地：
- `GameState.CurrentSaveSchemaVersion` 升级为 `v15g.1`。
- `GameState.FromStableJson` 支持导入旧 `v15g.0` payload，导入后自动归一化为 `v15g.1` 并补齐新增字段默认值。
- `HeroSnapshot` 新增：
  - `Effects`
  - `BattleStats`
  - `Resources`
  - `JianghuQualities`
- `GameState` 新增：
  - `SectBonuses`
  - `MartialArts`
  - `Romance`
  - `WeaponInfo`
- `CharacterInfoPanel` 的效果、战斗数值、资源材料、江湖资质已读取 `GameState.Hero`。
- `RightInfoPanel` 的门派加成、功法列表、武器名称/类型/评分/挂点/效果已读取 `GameState`。
- `BottomQuickBar` 的情缘、功法、武器评分已读取 `GameState`。
- `BattleScene` 新增仅供验证使用的环境变量入口：
  - `WUXIA_GODOT_PREVIEW_IMPORT_SAVE_TEXT`
  - `WUXIA_GODOT_PREVIEW_IMPORT_SAVE_PATH`
  - `WUXIA_GODOT_PREVIEW_CLEAR_SAVE`
- 新增 `tools/verify-save-refresh.ps1`：
  - 生成当前 `v15g.1` 测试存档。
  - 生成旧 `v15g.0` 兼容测试存档。
  - 运行 Godot 导入测试存档并截图。
  - 截图完成后清理本地预览存档。
- `data/combat-sample.json` 和 `data/art-rig.json` 标记到 `v15g.2`。
- 已输出预览：
  - `screenshots/v15g2-import-refresh.png`
  - `screenshots/v15g2-legacy-import-refresh.png`

验收：
- 当前 `v15g.1` 测试存档导入后，截图可见：
  - 层数、主角名、敌人名、战力、气血刷新。
  - 资源、资质、功法、情缘、武器评分刷新。
- 旧 `v15g.0` 测试存档导入后，截图可见旧核心字段刷新，同时新增字段按默认值补齐。
- 导入验证脚本执行后会清理 Godot 本地预览存档，不污染下一次正常打开。
- `dotnet build`、Godot 导入、headless 启动均通过。

当前限制：
- V15G.3 已补状态命令层，右侧闯荡按钮和底部部分卡片已可改状态；左侧角色面板、存档弹窗和大部分右侧信息仍以展示为主。
- 还没有把网页端真实存档转换成 Godot 当前 `v15g.2` payload 的桥接脚本。
- 自动化验证还没有解析截图文字，后续可以增加状态 dump 或 UI 文本探针。

### V15G.3 状态命令层和交互入口

目标：让暂停/自动闯荡、功法查看、情缘援手、武器切换从只读展示推进到真实改 `GameState` 的流程。

当前落地：
- `GameState.CurrentSaveSchemaVersion` 升级为 `v15g.2`。
- 新增 `CommandSnapshot`：
  - `IsPaused`
  - `AutoExplore`
  - `ActiveMartialArtIndex`
  - `LastCommand`
- `GameState.FromStableJson` 支持导入旧 `v15g.0` 和 `v15g.1` payload，并自动归一化到 `v15g.2`。
- 新增 `scripts/GameCommands.cs`，集中处理状态命令：
  - `toggle_pause`
  - `toggle_auto_explore`
  - `next_martial_art`
  - `trigger_romance_assist`
  - `cycle_weapon`
- `RightInfoPanel` 的江湖闯荡卡新增两个真实命令按钮：
  - 暂停/继续
  - 自动：开/关
- `BottomQuickBar` 的部分卡片已接入点击命令：
  - 江湖：切换自动闯荡
  - 功法：切换当前查看功法
  - 情缘：触发情缘援手
  - 武器：循环切换武器
- `BattleScene` 新增 `WUXIA_GODOT_PREVIEW_COMMANDS` 环境变量入口，可在截图前顺序执行命令。
- 新增 `tools/verify-commands.ps1`，执行命令序列并输出预览截图。
- `data/combat-sample.json` 和 `data/art-rig.json` 标记到 `v15g.3`。
- 已输出预览：
  - `screenshots/v15g3-command-preview.png`

验收：
- 执行 `auto,martial,romance,weapon` 后截图可见：
  - 江湖状态变成自动闯荡。
  - 当前查看功法切到第二门功法。
  - 情缘好感和状态发生变化。
  - 武器切换到玄铁重剑，评分变成 888。
- 命令统一通过 `GameCommands` 修改状态并调用 `SaveManager.SaveState`，UI 通过 `StateChanged` 刷新。
- `dotnet build` 通过。

当前限制：
- 命令还只是状态层和 UI 层联动，没有驱动角色动画、战斗结算或 FX。
- 底部卡片点击没有额外按钮提示，后续可以在不破坏布局的前提下补 hover/tooltip。
- 还没有状态 dump 自动断言，截图验证仍主要依赖视觉检查。

### V15G.4 状态 dump、网页存档桥接和命令战斗反馈

目标：把 V15G.3 的命令层从“只刷新 UI 文本”推进到可验证、可桥接、可驱动战斗表现的状态管线。

当前落地：
- `BattleScene` 新增 `WUXIA_GODOT_STATE_DUMP_PATH`，可在 headless 或截图预览后导出当前 `GameState.ToStableJson()`。
- 新增 `tools/verify-state-dump.ps1`，执行 `auto,martial,romance,weapon` 后直接断言 `v15g.2` schema、自动闯荡、功法索引、情缘好感和武器评分。
- 新增 `tools/convert-web-save.ps1`，先支持网页端纯 Base64 JSON 和直接 JSON 输入，输出 Godot 当前 `WUXIA_SAVE_V15G0::payload::checksum` 存档文本。
- 新增 `tools/verify-web-save-bridge.ps1`，把样例 JSON 转成 Godot 存档文本后再导入并 dump，验证桥接链路没有断。
- `BattleScene` 订阅 `SaveManager.StateChanged`，对命令状态变化触发战斗层反馈：自动闯荡、功法切换、情缘援手和武器切换都会驱动主角动作、武器贴图或像素 FX。
- `HeroRig` 暴露 `SetWeaponTexturePath`，后续武器系统可以继续替换贴图，不需要换人物本体。
- 新增预览截图：`screenshots/v15g4-command-fx-preview.png`。
- `data/art-rig.json` 已登记状态 dump、命令验证和网页桥接脚本。

验收：
- `tools/verify-state-dump.ps1` 能通过机器断言，不再只靠截图目测状态变化。
- `tools/convert-web-save.ps1` 生成的存档文本可被 Godot 导入，并可再次 dump 出 `v15g.2` 稳定状态；`tools/verify-web-save-bridge.ps1` 通过。
- 执行 `auto,martial,romance,weapon` 后，UI 状态、主角武器贴图和像素 FX 均有反馈。
- `dotnet build` 通过。

当前限制：
- 网页桥接仍是字段映射雏形；需要拿真实网页导出存档做字段覆盖表和失败字段报告。
- 命令反馈仍是表现层联动，不等于真实战斗结算；伤害、回合、掉落和事件队列留到 V15G.5 以后。
- 功法和情缘 FX 仍使用工程生成的像素帧，最终美术精修还要替换资源。

### V15G.5 战斗结算事件和网页字段覆盖报告

目标：把 V15G.4 的命令表现反馈推进为可断言的战斗结算状态，并为网页真实存档迁移补字段覆盖报告。

当前落地：
- `GameState` 稳定 payload schema 升级到 `v15g.3`，新增 `CombatSnapshot`：
  - `turn`
  - `lastAction`
  - `lastTarget`
  - `lastDamage`
  - `lastDamageRatio`
  - `lastHitKind`
  - `enemyDefeated`
  - `message`
- `GameCommands` 新增 `combat_tick` / `resolve_combat_tick` / `tick` 命令：
  - 根据主角外功、内功、当前功法索引和武器评分计算伤害。
  - 实际扣减敌方血量。
  - 按伤害占敌方最大生命比例产出 `graze` / `hit` / `defeat`。
  - 写入 `CombatSnapshot`，供 UI、动画和验证脚本读取。
- `BattleScene` 监听 `CombatSnapshot.Turn` 变化：
  - 主角播放 `attack`。
  - 敌方按 `lastHitKind` 播放 `graze`、`hit` 或 `defeat`。
  - 同步触发武器斩击、命中火花或重击像素 FX。
- `data/combat-sample.json` 升级到 `v15g.5`，新增 `combat` 初始块。
- `tools/convert-web-save.ps1` 输出 `v15g.3` 稳定 payload，并为导入的网页存档补 `combat` 默认块。
- 新增 `tools/analyze-web-save-fields.ps1`：
  - 支持直接 JSON、Base64 JSON 和 Godot save text envelope。
  - 展开 JSON 字段路径。
  - 按当前桥接脚本支持的字段前缀输出 mapped / unmapped 报告。
- 新增 `tools/verify-web-save-field-report.ps1`，验证字段报告可以生成，并确认 `stage`、`hero`、`enemy`、`rightPanel` 等核心来源能被识别。
- `data/art-rig.json` 登记 V15G.5 的字段报告脚本和验证脚本。
- 新增预览截图：`screenshots/v15g5-combat-tick-preview.png`。

验收：
- `powershell -ExecutionPolicy Bypass -File tools\verify-utf8.ps1` 通过。
- JSON 数据能用 UTF-8 读取并解析。
- `dotnet build` 通过。
- `tools/verify-state-dump.ps1` 执行 `auto,martial,romance,weapon,combat_tick` 后断言：
  - schema 为 `v15g.3`。
  - 自动闯荡开启。
  - 当前功法索引推进。
  - 情缘状态变化。
  - 武器切换到玄铁重剑。
  - `combat.turn` 推进到 1。
  - `combat.lastDamage` 大于 0。
  - 敌方血量下降。
- `tools/verify-web-save-bridge.ps1` 通过，样例网页存档可桥接为 Godot 存档并导入。
- `tools/verify-web-save-field-report.ps1` 通过，能输出字段覆盖报告。
- `tools/verify-save-refresh.ps1` 当前和旧 `v15g.0` 存档导入刷新均通过。

当前限制：
- 伤害公式仍是工程版，用于验证事件链路，不是最终战斗数值。
- `combat_tick` 仍由预览命令触发，还没有接入游戏内定时自动战斗循环。
- 字段覆盖报告只负责识别和报告未映射字段，暂不阻止导入。
- 迁移警告还没有显示在 Godot 游戏内 UI。

### V15G.6 自动战斗循环、事件日志和迁移警告

目标：把 V15G.5 的单次结算推进为能自动连续执行的战斗循环，并让战斗事件和网页存档迁移警告进入 Godot UI。

当前落地：
- `GameState` 稳定 payload schema 升级到 `v15g.4`。
- `CombatSnapshot` 新增：
  - `eventLog`：保留最近 6 条战斗事件。
  - `migrationWarnings`：保存网页存档桥接警告。
- `GameCommands.ResolveCombatTick`：
  - 改为中文战斗消息。
  - 每次结算追加事件日志。
  - 敌人被击败后自动设置 `AutoExplore=false`、`IsPaused=true`，避免循环空转。
- `BattleScene`：
  - 新增自动战斗循环，只有 `AutoExplore=true`、未暂停、敌人未击败时才按间隔触发 `combat_tick`。
  - 每次 `StateChanged` 都会在设置了 `WUXIA_GODOT_STATE_DUMP_PATH` 时写出最新状态，自动战斗验证可以读取最终 dump。
- `RightInfoPanel`：
  - 新增“战斗事件”卡片。
  - 显示当前回合、最近伤害、最近事件日志。
  - 如果导入的是网页桥接存档，会显示迁移警告。
- `tools/convert-web-save.ps1`：
  - 输出 `v15g.4` 稳定 payload。
  - 对网页桥接存档写入默认 `migrationWarnings`，提醒运行字段覆盖报告。
- 新增 `tools/verify-auto-combat.ps1`：
  - 清空本地存档。
  - 只执行 `auto` 命令。
  - 等待约 420 帧。
  - 断言自动战斗至少推进 2 回合、敌人血量下降、事件日志生成。
- `data/combat-sample.json` 标记到 `v15g.6`。
- `data/art-rig.json` 标记到 `v15g.6`，并登记 `autoCombatVerifyScript`。
- 新增预览截图：
  - `screenshots/v15g6-combat-event-preview.png`
  - `screenshots/v15g6-web-migration-warning.png`

验收：
- `tools/verify-auto-combat.ps1` 通过。
- `tools/verify-state-dump.ps1` 通过，命令序列执行后能看到 `v15g.4` schema、战斗回合推进和事件日志。
- `tools/verify-web-save-bridge.ps1` 通过，网页桥接导入后能看到 `migrationWarnings`。
- `tools/verify-web-save-field-report.ps1` 通过，字段覆盖报告仍能生成。
- `tools/verify-save-refresh.ps1` 当前和旧 `v15g.0` 存档导入刷新均通过。
- `dotnet build`、Godot headless import/run、UTF-8 和 JSON 检查均通过。

当前限制：
- 自动战斗只打当前敌人；击败后会停下，还没有自动生成下一层敌人。
- `eventLog` 是快照内的小队列，还不是独立事件总线。
- 伤害公式仍是工程版，后续需要接真实数值系统。
- 迁移警告只显示文本，暂未做点击展开字段报告详情。

### V15G.7 结构化事件队列和层数推进

目标：把 V15G.6 的字符串事件日志升级为结构化战斗事件队列，并让自动战斗在击败敌人后推进到下一层、生成下一名敌人。

当前落地：
- `GameState` 稳定 payload schema 升级到 `v15g.5`。
- `CombatSnapshot` 新增 `events`：
  - `turn`
  - `kind`
  - `actor`
  - `target`
  - `amount`
  - `message`
- 新增 `CombatEventSnapshot`，旧存档没有 `events` 时会从 `eventLog` 自动回填为 `log` 事件。
- `GameCommands.ResolveCombatTick`：
  - 每次伤害结算都会写入结构化事件。
  - 击败敌人时写入 `defeat` 事件，但如果自动闯荡仍开启，不会把自动战斗停掉。
  - 下一次 tick 遇到已击败敌人时，会推进到下一层并生成下一名敌人。
- 新增工程版敌人生成规则：
  - 层数 `Stage.Floor + 1`。
  - 敌方名称从若干江湖敌人名中轮换。
  - 敌方标签从外功、身法、防反、内功、破招中轮换。
  - 气血按上一名敌人的最大气血和层数递增。
- `RightInfoPanel` 的“战斗事件”卡片优先读取 `combat.events`，旧存档才回退到 `eventLog`。
- `BattleScene` 截图工具新增 `WUXIA_GODOT_CAPTURE_AFTER_FRAMES`，可等待自动战斗推进后再截图。
- 新增 `tools/verify-floor-advance-preview.ps1`，用于自动闯荡到下一层后截图。
- `tools/verify-auto-combat.ps1` 现在断言：
  - schema 为 `v15g.5`。
  - 自动战斗至少推进 2 回合。
  - 结构化事件数大于等于 3。
  - 层数已经大于 20。
- `data/combat-sample.json` 标记到 `v15g.7`。
- `data/art-rig.json` 标记到 `v15g.7`，并登记 `floorAdvancePreviewScript`。
- 新增预览截图：
  - `screenshots/v15g7-combat-event-queue-preview.png`
  - `screenshots/v15g7-floor-advance-preview.png`

验收：
- `tools/verify-auto-combat.ps1` 通过，证明自动战斗会击败敌人并推进层数。
- `tools/verify-state-dump.ps1` 通过，证明命令序列会写入结构化事件。
- `tools/verify-web-save-bridge.ps1` 通过，网页桥接导入后也包含结构化导入事件和迁移警告。
- `tools/verify-floor-advance-preview.ps1` 可输出自动推进后的预览截图。
- `dotnet build`、Godot headless import/run、UTF-8 和 JSON 检查均通过。

当前限制：
- 敌人视觉资源还没有随新敌人名/标签切换，仍使用同一个 Godot 敌方 rig。
- 层数推进和敌人刷新发生在状态层，视觉上还没有做“击败停顿 -> 新敌人入场”的完整节奏。
- `events` 仍保存在 `CombatSnapshot` 内，还没有拆成独立战斗事件总线或持久化事件表。
- 伤害和敌人成长公式仍是工程版。

### V15G.8 敌方视觉刷新和入场节奏

目标：把 V15G.7 的层数推进事件接到 Godot 场景表现层，让“生成下一名敌人”不只停留在状态和 UI 文本里，而是能看到敌人资源刷新、武器变化和入场动作。

当前落地：
- `EnemyRig` 新增 `spawn` 入场动画。
- `EnemyRig` 新增 `SetProfile`，支持在运行时刷新：
  - body 贴图。
  - weapon 贴图。
  - feet anchor。
  - weapon socket。
  - 武器缩放。
  - Boss 标记。
- `EnemyRig.ApplyResources` 会在切换资源时重置 body、weapon、透明度、旋转和位移，避免上一名敌人的受击/击败姿态污染下一名敌人。
- `BattleScene` 新增敌方视觉 profile 选择：
  - 20 层及以下使用 `enemy_early_idle.webp`。
  - 21 层开始使用 `enemy_mid_idle.webp`。
  - 30 层开始使用 `enemy_late_idle.webp`。
  - 武器按楼层轮换 sword、staff、cane、spear、blade。
- `BattleScene.OnGameStateChanged` 会识别 `combat.lastHitKind == "advance"` 且楼层或敌人名发生变化的状态更新。
- 识别到 `advance` 后，`BattleScene` 会：
  - 让主角回到 idle。
  - 调用 `UpdateEnemyProfile` 刷新敌人视觉资源。
  - 播放敌方 `spawn`。
  - 在敌方身位补一个像素火花提示，作为新敌人登场反馈。
- 新增 `tools/verify-enemy-visual-refresh.ps1`，用于自动闯荡到下一层后截图并断言：
  - schema 仍为 `v15g.5`。
  - 楼层已经大于 20。
  - 结构化事件队列里存在 `advance` 事件。
  - 敌人最大气血已经刷新为下一层生成敌人。
- `data/combat-sample.json` 标记到 `v15g.8`。
- `data/art-rig.json` 标记到 `v15g.8`，并登记 `enemyVisualRefreshVerifyScript`。
- 新增预览截图：
  - `screenshots/v15g8-enemy-visual-refresh-preview.png`

验收：
- `tools/verify-enemy-visual-refresh.ps1` 通过，证明自动战斗推进到下一层后，场景层能消费 `advance` 并输出刷新后的预览。
- `dotnet build` 通过，证明新增 profile 和入场链路没有破坏 Godot C# 编译。
- UTF-8、JSON、Godot headless 和既有 V15G 验证继续通过。

当前限制：
- 敌方视觉 profile 仍按楼层工程规则切换，还没有按真实敌人名、门派、武功、Boss 类型建立完整美术表。
- 新敌人入场节奏已经存在，但“击败停顿 -> 奖励结算 -> 新敌人入场 -> 下一回合”还没有拆成可配置时间轴。
- `spawn` 是工程动画，后续需要为不同门派、Boss、奇遇敌人补独立入场姿态和 FX。

### V15G.9 战斗事件时间轴驱动

目标：把自动战斗从“固定间隔直接 tick”推进到“由战斗视觉事件节拍门控 tick”。击败、奖励结算、新敌人入场和下一回合准备不再只是状态变化，而是有明确的表现时间。

当前落地：
- `BattleScene` 新增轻量时间轴队列：
  - `CombatTimelineStep`：记录 phase、持续时间和原因。
  - `_combatTimeline`：等待播放的 phase 队列。
  - `_activeTimelineStep` / `_activeTimelineRemainingSeconds`：当前正在占用自动战斗节拍的 phase。
  - `_combatTimelineTrace`：保留最近 24 个 phase，供验证和后续调试。
- 自动战斗 `_Process` 现在会先推进时间轴：
  - 如果时间轴仍有 active phase，则重置自动战斗计时并暂停本帧 tick。
  - 时间轴结束后，自动战斗才重新累计 `AutoCombatIntervalSeconds`。
- `PlayCombatSettlementFeedback` 已按命中类型写入时间轴：
  - `graze`：`graze_recover` -> `next_beat_ready`。
  - `hit`：`hit_recover` -> `next_beat_ready`。
  - `defeat`：`defeat_pause` -> `reward_settlement`。
- `PlayEnemyAdvanceFeedback` 已写入：
  - `enemy_spawn` -> `next_beat_ready`。
- 新增 `WUXIA_GODOT_TIMELINE_DUMP_PATH` 环境变量，可输出当前战斗时间轴 dump。
- 新增 `tools/verify-combat-timeline.ps1`，会自动闯荡并断言：
  - 楼层已推进到 20 层之后。
  - 结构化事件队列存在 `advance`。
  - 时间轴 trace 存在 `defeat_pause`。
  - 时间轴 trace 存在 `reward_settlement`。
  - 时间轴 trace 存在 `enemy_spawn`。
  - 时间轴 trace 存在 `next_beat_ready`。
- `data/combat-sample.json` 标记到 `v15g.9`。
- `data/art-rig.json` 标记到 `v15g.9`，并登记 `combatTimelineVerifyScript`。
- 新增预览截图：
  - `screenshots/v15g9-combat-timeline-preview.png`

验收：
- `tools/verify-combat-timeline.ps1` 通过，证明自动战斗不是无视视觉节拍连续 tick。
- `tools/verify-auto-combat.ps1` 继续通过，证明新增时间轴不会阻断自动闯关。
- `tools/verify-enemy-visual-refresh.ps1` 继续通过，证明 V15G.8 的敌方刷新链路仍然有效。
- `dotnet build`、Godot headless import/run、UTF-8 和 JSON 检查均通过。

当前限制：
- 时间轴目前仍写在 `BattleScene`，不是独立战斗表现服务；后续如果场景拆分，需要抽到单独类。
- `reward_settlement` 目前是节拍占位，还没有真正奖励、掉落、银两、修为飞字。
- 时间轴 trace 是调试 dump，不是最终 UI；右侧战斗事件面板仍显示结构化 combat events。

### V15G.10 奖励、掉落和战利事件 UI

目标：把 V15G.9 的 `reward_settlement` 从“等待节拍”升级为真实战斗结算。击败敌人后，资源要真的增加，事件队列要能描述奖励，时间轴要能播放奖励飞字，右侧战斗事件 UI 要能读懂奖励、掉落、门派和江湖提示。

当前落地：
- `GameCommands.ResolveCombatTick` 在敌人被击败时会计算工程版奖励：
  - 银两。
  - 修为。
  - 精铁。
  - 寒玉粮。
  - 门派战绩/江湖声望。
- 奖励会真实写回 `HeroSnapshot`：
  - `Hero.Resources.Silver` 增加。
  - `Hero.Resources.Essence` 增加。
  - `Hero.Resources.Food` 按层数条件增加。
  - `Hero.Cultivation` 增加。
  - `Hero.JianghuQualities.Reputation` 增加。
- 击败结算会写入结构化事件：
  - `reward`：银两、修为、食物等主奖励。
  - `drop`：战利品名称和数量。
  - `sect_notice`：门派战绩提示。
  - `adventure_notice`：江湖推进提示。
- `CombatSnapshot.events` 保留上限从 8 条增加到 12 条，避免击败、奖励、掉落、门派、江湖提示被挤掉。
- `BattleScene` 在时间轴进入 `reward_settlement` 时会读取最近奖励类事件，并在中间战斗区播放奖励飞字：
  - 金色：主奖励。
  - 青色：掉落。
  - 浅绿：门派战绩。
  - 米白：江湖推进。
- `RightInfoPanel` 的“战斗事件”卡片升级为：
  - `战斗过程`：显示命中、击败、推进等战斗事件。
  - `战利结算`：单独显示奖励、掉落、门派和江湖提示，避免和战斗过程重复。
- 新增 `tools/verify-reward-settlement.ps1`，会自动闯荡到击败结算并断言：
  - schema 仍为 `v15g.5`。
  - 当前处于击败结算节拍。
  - 事件队列包含 `reward`、`drop`、`sect_notice`、`adventure_notice`。
  - 时间轴 trace 包含 `reward_settlement`。
  - 银两、精铁、修为、声望都确实增长。
- `data/combat-sample.json` 标记到 `v15g.10`。
- `data/art-rig.json` 标记到 `v15g.10`，并登记 `rewardSettlementVerifyScript`。
- 新增预览截图：
  - `screenshots/v15g10-reward-settlement-preview.png`

验收：
- `tools/verify-reward-settlement.ps1` 通过，证明战斗奖励从状态、事件、时间轴和画面四条链路闭环。
- `tools/verify-combat-timeline.ps1` 继续通过，证明时间轴没有被奖励飞字破坏。
- `tools/verify-auto-combat.ps1` 继续通过，证明新增奖励不会阻断自动闯关。
- `dotnet build`、Godot headless import/run、UTF-8 和 JSON 检查均通过。

当前限制：
- 奖励公式仍是工程版，后续需要接真实掉落表、敌人表、门派贡献和奇遇触发表。
- 奖励飞字是 `BattleScene` 内的临时 Control/Tween 实现，后续应该抽成复用的 `FloatingTextLayer` 或战斗表现服务。
- 战利事件 UI 仍是右侧信息面板的一部分，还没有独立可展开的战斗结算弹窗或详细掉落面板。

### V15G.11 时间轴服务和奖励规则表

目标：减少 `BattleScene` 的职责，把战斗表现时间轴抽成可复用服务；同时把 V15G.10 的奖励/掉落/门派/江湖提示硬编码迁移到可配置 JSON 表，为后续门派、Boss、奇遇和掉落表扩展打基础。

当前落地：
- 新增 `scripts/CombatPresentationTimeline.cs`：
  - `CombatPresentationTimeline` 管理表现 phase 队列。
  - `CombatTimelineStep` 记录 phase、持续时间和原因。
  - `CombatTimelineTraceEntry` 保留最近表现 trace。
  - `CombatTimelineDump` 继续支持 `WUXIA_GODOT_TIMELINE_DUMP_PATH` 验证输出。
- `BattleScene` 不再直接持有 `Queue<CombatTimelineStep>`、active step、remaining seconds 和 trace list。
- `BattleScene` 只负责：
  - 创建 `CombatPresentationTimeline`。
  - 在 `_Process` 中调用 `Advance(delta)`。
  - 通过 `StepStarted` 事件播放具体表现，例如 `reward_settlement` 飞字。
  - 把时间轴 dump 委托给 `CombatPresentationTimeline.CreateDump`。
- 新增 `data/reward-rules.json`：
  - `rewardFormula`：银两、修为、精铁、食物、门派战绩的工程版公式参数。
  - `dropByTag`：按敌人标签映射掉落名。
  - `messages`：奖励、掉落、门派提示和江湖提示模板。
- 新增 `scripts/RewardRuleCatalog.cs`：
  - 从 `res://data/reward-rules.json` 读取规则。
  - 失败时回退到内置默认值。
  - 输出 `RewardSettlementData`，供 `GameCommands.ResolveCombatTick` 使用。
- `GameCommands` 不再硬编码奖励公式和掉落名，改为调用 `RewardRuleCatalog.Calculate`。
- 新增 `tools/verify-reward-rules-config.ps1`，会：
  - 检查 `reward-rules.json` 版本为 `v15g.11`。
  - 运行 Godot 自动战斗到奖励结算。
  - 按配置表重新计算期望银两、修为、精铁、食物、门派声望。
  - 精确断言存档里的资源增长和事件 amount 与配置表一致。
  - 断言掉落事件使用配置表中的敌人标签掉落名。
- `data/combat-sample.json` 标记到 `v15g.11`。
- `data/art-rig.json` 标记到 `v15g.11`，并登记：
  - `rewardRules`
  - `rewardRulesVerifyScript`
- 新增预览截图：
  - `screenshots/v15g11-reward-rules-preview.png`

验收：
- `tools/verify-reward-rules-config.ps1` 通过，证明奖励和掉落确实来自配置表。
- `tools/verify-reward-settlement.ps1` 继续通过，证明 V15G.10 的奖励结算表现仍有效。
- `tools/verify-combat-timeline.ps1` 继续通过，证明时间轴抽服务后没有破坏节拍。
- `dotnet build`、Godot headless import/run、UTF-8 和 JSON 检查均通过。

当前限制：
- 敌方视觉 profile、武器轮换、Boss 标记仍写在 `BattleScene`，还没有迁移到独立配置表。
- 奖励飞字仍由 `BattleScene` 创建，尚未抽成 `FloatingTextLayer`。
- `RewardRuleCatalog` 目前只覆盖战斗击败奖励，奇遇触发、门派任务、Boss 特殊掉落还没接入。

### V15G.12 Combat presentation rules

Goal: move combat presentation data out of `BattleScene` so enemy visuals, weapon cycles, reward notice presentation, and boss timing can be tuned from JSON tables.

Current landing:
- Added `data/combat-presentation-rules.json`.
- Added `scripts/CombatPresentationCatalog.cs`.
- `BattleScene` now reads enemy body texture, weapon texture, feet anchor, weapon socket, actor scale, and boss flag from `CombatPresentationCatalog`.
- `BattleScene` now reads combat timeline durations from the presentation table for `graze`, `hit`, `defeat`, `reward_settlement`, and `enemy_spawn`.
- `reward`, `drop`, `sect_notice`, and `adventure_notice` floating text max count, base position, row spacing, and colors now come from `rewardNoticePresentation`.
- `CombatTimelineDump` now includes `enemyProfileId`, `enemyBodyTexturePath`, and `enemyWeaponTexturePath` so automated checks can prove the presentation table is active.
- Added `tools/verify-combat-presentation-rules.ps1`.
- `data/art-rig.json` and `data/combat-sample.json` are marked `v15g.12`.
- New preview output: `screenshots/v15g12-combat-presentation-rules-preview.png`.

Acceptance:
- `tools/verify-combat-presentation-rules.ps1` must pass.
- The verification must confirm that the current enemy profile and weapon texture match `data/combat-presentation-rules.json`.
- The verification must confirm that `defeat_pause`, `reward_settlement`, and `enemy_spawn` durations match `data/combat-presentation-rules.json`.
- Existing V15G.6-V15G.11 state, auto combat, save bridge, timeline, and reward checks must continue to pass.

Known limits:
- Reward floating text is still created inside `BattleScene`; V15G.13 should extract it into a reusable presentation layer.
- Enemy visual profiles are still coarse floor bands, not full enemy-name/sect/boss-specific art tables.
- Sect/adventure notices are presentation-configurable, but their gameplay triggers still come from `RewardRuleCatalog` and `GameCommands`.

## 固定验证

每个阶段完成后至少运行：

```powershell
powershell -ExecutionPolicy Bypass -File tools\verify-utf8.ps1
powershell -ExecutionPolicy Bypass -File tools\verify-godot.ps1
dotnet build
```

有场景后增加：

```powershell
. .\tools\godot-env.ps1
Invoke-WuxiaGodot --headless --path . --import
Invoke-WuxiaGodot --headless --path . --quit
```

视觉阶段需要额外输出：
- 截图。
- 10-20 秒预览视频或 GIF。
- 当前缺陷清单。
- 当 headless dummy renderer 无法保存 viewport 时，使用 `WUXIA_GODOT_CAPTURE_SEQUENCE_DIR` 走正常 Windows 渲染器逐帧截图，再用 ffmpeg 合成 GIF。

## 下一步

进入 `V15G.13`：把奖励飞字从 `BattleScene` 抽成可复用表现层，并准备 Boss、门派和奇遇专属战斗表现钩子。
