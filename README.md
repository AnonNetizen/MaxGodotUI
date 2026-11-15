# MaxGodotUI

Godot .NET 通用 UI 与视觉表现插件。当前 `0.5.1` 提供程序化背景、交互式悬停说明、显式时钟过渡、视觉层运动、控件工厂与 CanvasItem shader，并增加边缘连接与流动直线箭头；保留 `0.3.0`／`0.4.0`／`0.5.0` 的公共接口。运行时无需启用编辑器插件；组件不负责宿主规则或流程。

## 安装

将本仓库根目录放在宿主 `addons/max_godot_ui/`。使用 Godot 4.7.2 .NET 与 .NET 8，宿主 `.csproj` 保留默认源码包含规则和 `<EnableDynamicLoading>true</EnableDynamicLoading>`。C# 与 GDScript 均通过同一组 Godot 类调用，不依赖 MaxGodotFrame。

## 运行时 API

[HoverDetails](runtime/HoverDetails.cs) 继承 `PanelContainer`：

- `ShowAt(Control content, Vector2 anchor, Vector2 bounds)`：接收尚无父节点的显示内容，在父级画布坐标中显示，按内容最小尺寸与面板内边距计算位置；在给定范围内保留 8 像素边距。
- 内容设置 `CustomMinimumSize`，长内容需由宿主提供滚动容器或合适的显示形式。范围小于内容时无法保证全部可见，宿主负责可读尺寸。
- 下一次显示会移除并释放旧内容；`Close()` 只隐藏面板。面板和内容忽略鼠标输入。

[UiMotion](runtime/UiMotion.cs) 继承 `Node`：

- `Move(Control target, Vector2 destination, double duration)`：以 Cubic / Out 插值移动到父坐标中的目标位置，返回 `Tween`。
- `Flash(CanvasItem target, Color color, double duration)`：立即设置闪烁色，然后恢复调用前颜色，返回 `Tween`。
- `Shake(Control target, float amplitude, double duration)`：固定衰减波形的位移震动，返回 `Tween`，不使用随机源。对同一目标再次调用时先恢复原位置，再启动新震动。
- `StopShake(Control target)`：停止震动并恢复原位置；自然结束、目标离开场景树或节点退出时也会清理。宿主可用它实现立即关闭震动。
- Tween 绑定目标生命周期，场景树暂停时停止。宿主需避免对同一属性同时发起冲突动画，不用动画回调计算规则结果。

GDScript 示例：

```gdscript
var motion = load("res://addons/max_godot_ui/runtime/UiMotion.cs").new()
add_child(motion)
var tween: Tween = motion.Move($Panel, Vector2(100, 50), 0.2)
await tween.finished
```

宿主提供主题、文本与输入规则；组件不识别卡牌、战斗或平台事件。后续按实际需求增加视觉表现，不提供声音、存档或设置模块。

## 0.5.0 程序化背景

[BackgroundStyle](runtime/BackgroundStyle.cs) 是配置资源：`Shader`、`BaseColor`、`PrimaryColor`、`SecondaryColor`、`Speed`、`Intensity`、归一化 `Center` 和确定性 `Seed`。先配置再传入控件；同一资源可共享给多个控件，材质参数互相隔离。修改风格时传入新的资源，重复设置同一资源不会重置或重建材质。

[ShaderBackground](runtime/ShaderBackground.cs) 继承 `Control`，内部固定两个忽略输入的绘制面：

- `SetStyle(style, transitionSeconds = 0.18)`：首次立即显示，后续交叉淡化。正常只有一个绘制面，过渡最多两个；连续切换从当前占主导的风格过渡到最新目标，取消旧过渡并释放旧材质。
- `UpdateSurface(time, delta, normalizedPointer, displaySize)`：时间单位为秒；指针范围为 `[-1, 1]`；显示尺寸是控件在屏幕上实际占用的像素，用于宽高比及边缘抗锯齿。控件自身的 `Size` 仍是父画布布局尺寸。
- `SetDetail(2 / 1 / 0)`：动态程序图案／静态程序图案／低成本静态渐变。切为静态时立即收束过渡，忽略指针和时间；不由插件读取宿主设置。
- `FinishTransition()`：立即呈现最新目标。`CurrentStyle`、`Detail`、`ActiveLayers`、`IsTransitioning`、`TransitionProgress` 可用于观察状态。
- 控件不自行推进时间。暂停时传入原时间、零增量及原指针；速度由宿主决定。退出场景树会释放两个材质并清空风格，重新加入后需再设置风格。

五个单遍 Compatibility CanvasItem 程序位于 `shaders/`：`background_nebula`（流动色带）、`background_starmap`（网格与轨道）、`background_warp`（径向光带）、`background_field`（外围能量场）、`background_halo`（扩散光环）。它们共享 [background_common.gdshaderinc](shaders/background_common.gdshaderinc) 的噪声、星点、几何与调色函数；无纹理、屏幕采样、内置 `TIME`、临时粒子或随机源依赖。自定义 shader 需遵循该 include 的 uniform 名称及颜色调制约定，才能使用控件的时钟、等级和交叉淡化。

```csharp
var style = new BackgroundStyle {
    Shader = GD.Load<Shader>("res://addons/max_godot_ui/shaders/background_nebula.gdshader"),
    PrimaryColor = new Color("55bbcc"), SecondaryColor = new Color("665599")
};
var background = new ShaderBackground { Size = new Vector2(1280, 720) };
AddChild(background);
background.SetStyle(style);
background.UpdateSurface(3.0, 0.016, Vector2.Zero, new Vector2(1280, 720));
```

```gdscript
var style = load("res://addons/max_godot_ui/runtime/BackgroundStyle.cs").new()
style.Shader = load("res://addons/max_godot_ui/shaders/background_starmap.gdshader")
var background = load("res://addons/max_godot_ui/runtime/ShaderBackground.cs").new()
add_child(background)
background.size = Vector2(1280, 720)
background.SetStyle(style, 0.18)
background.UpdateSurface(3.0, 0.016, Vector2.ZERO, Vector2(1280, 720))
```

`AmbientView` 和 `ambient` shader 继续保留，供依赖图片背景的现有宿主使用。

## 0.5.0 轮廓亮边与柔光

[UiHighlight](runtime/UiHighlight.cs) 是忽略输入的 `Control`，通过 [HighlightStyle](runtime/HighlightStyle.cs) 配置颜色、线宽、柔光宽度／透明度、角标长度、呼吸幅度与周期。组件不识别交互状态，宿主决定状态优先级和样式。

- `Source` 指定视觉控件，`SourceRect` 为空时使用来源尺寸，`Radius` 指定圆角。绘制时读取来源最新画布变换；可放在来源内部，也可放在宿主独立裁切的装饰层。来源的布局和输入范围不变。
- `SetStyle(style, seconds = 0.14)` 从当前外观切换到新样式；`null` 淡出。重复设置同一资源不重启过渡；样式资源配置后按只读共享。
- `Pulse(style, seconds = 0.16)` 立即应用一个临时样式，结束时恢复最新目标样式。重复调用替换当前临时反馈，不叠加。它不占用 `UiTimeline` 的临时效果预算。
- `UpdateSurface(time, delta)` 接收宿主秒数，零增量冻结过渡和临时样式；暂停呼吸需同时保持 `time`。组件不自行推进时钟。
- `SetDetail(2 / 1 / 0)` 对应完整／静态薄柔光／无柔光即时描边；角标始终保留。呼吸幅度最大 10%，无样式或来源不可见时不持续更新材质参数。
- `CurrentStyle`、`IsTransitioning`、`IsPulsing`、`SourceTransform` 与 `ParameterUpdates` 可供宿主检查。换父节点保留样式及过渡进度；离树时释放原生材质，重新加入后的首次更新恢复参数，不重播过渡。宿主推进时发现来源销毁也立即隐藏并释放材质。外置装饰的节点生命周期由宿主与来源一起管理。

共享 [highlight_outline](shaders/highlight_outline.gdshader) 是单遍 Compatibility 距离场轮廓，使用像素导数抗锯齿，正文内部透明；不依赖图片、粒子、屏幕采样或 SubViewport。各控件材质参数独立，外部留白与裁切由宿主决定。

```csharp
var outline = new UiHighlight { Source = visual, Radius = 4 };
decorationLayer.AddChild(outline);
outline.SetStyle(new HighlightStyle { Ink = Colors.Cyan, GlowWidth = 8, CornerLength = 14 });
outline.UpdateSurface(2.0, 0.016);
```

```gdscript
var outline = load("res://addons/max_godot_ui/runtime/UiHighlight.cs").new()
outline.Source = $Visual
add_child(outline)
var style = load("res://addons/max_godot_ui/runtime/HighlightStyle.cs").new()
style.Ink = Color.CYAN
outline.SetStyle(style, 0.14)
outline.UpdateSurface(2.0, 0.016)
```

## 0.5.0 面板进入与退出

`UiPanelTransition` 是由宿主 `UiTimeline` 推进的通用节点。`Configure(timeline, visual, shownPosition, hiddenPosition, channel)` 绑定独立视觉容器及两个端点，初始呈现隐藏端；不管理遮罩、输入、暂停、层级或业务状态。

- `Enter(seconds)` 使用 Cubic Out，`Exit(seconds)` 使用 Cubic In；按剩余距离缩短时长。相同目标不重启，反向请求保留当前显示位置。
- `SetPresentation(true)` 使用位移与原始透明度；`false` 在显示位置淡变。切换时收束现有过渡。
- `Finish()` 立即到达当前目标并发出一次 `Completed(bool shown)`；`Cancel()` 保持当前外观且不发完成信号，之后可再次进入或退出。零时长直接完成。
- `Amount` 为 0—1 的显示程度，宿主可据此绘制固定遮罩；`TargetShown` 与 `IsTransitioning` 提供状态。目标离树及组件释放取消剩余回调。
- `UiTimeline.CancelTarget(control)` 只取消该目标，不提前结束其子节点动画；整棵子树释放仍使用 `Clear`。既有 `Enter` 等接口兼容。

```csharp
var slide = new UiPanelTransition();
AddChild(slide);
slide.Configure(timeline, panel, Vector2.Zero, new Vector2(0, -800), 0);
slide.Enter(0.22);
// 宿主每帧调用 timeline.Advance(0, delta)，自行决定暂停与速度。
// 在之后的关闭请求中调用：
slide.Exit(0.18);
```

GDScript 可调用同名方法，`Configure` 的五个参数须完整提供；例如 `slide.Completed.connect(func(shown): print(shown))`。完成信号不承担宿主业务操作。

`UiControls.BindPress(button, Callable)` 只在按钮可接收输入时调用回调，重复发射已禁用控件的信号不会穿过输入屏障。`UiTextBinding.Enabled = false` 可让退场视图保留最后文本，避免其业务对象释放后再次刷新。

## 0.4.0 通用表现 API

新增组件均可在未启用编辑器插件时使用，Godot 属性、`Callable` 和信号对 C#／GDScript 开放。C# 方法的可选参数不会自动成为 GDScript 默认参数；GDScript 调用须给出完整参数，空回调用 `Callable()`，空详情用 `null`。Godot 4.7.2 的 C# `Callable` 不能往返传递 GDScript 匿名函数及绑定参数，见 [Godot 引擎问题 #97358](https://github.com/godotengine/godot/issues/97358)；GDScript 传入 `Callable(self, "method_name")`，C# 传入 `Callable.From(...)`。Godot 信号仍可在 GDScript 内直接连接匿名函数。

### 交互式说明

[HoverInspector](runtime/HoverInspector.cs) 继承 `Control`。`ShowEntries(Control anchor, Array<Control> entries, Control detail)` 接收尚无父节点的条目和可选详情，逐条包裹 `EntryStyle`；条目原名称保留在包装框上。调用后组件拥有这些节点，下一次显示释放旧内容。

`ExplanationWidth`、`MaxExplanationHeight` 控制说明列尺寸；`Bounds` 是组件本地画布大小，`BottomLimit` 是该坐标系中的额外底部避让线。列默认与来源顶部对齐，右侧不足时翻转，超出边缘或底线时上移；列表自然收紧并在超高时滚动。宿主提供 `Theme`，条目提供文本和最小宽度；组件每帧响应换行或语言变化后的尺寸。

`ContainsPoint` 接收全局画布坐标，只计入实际显示区域；`PointerInPopup`、`Retains(anchor)` 支持宿主维持悬停。离开来源及说明 0.2 秒后关闭，来源销毁、隐藏或开始原生拖拽时关闭。`LeaveSource()` 重置离开计时，`Close()` 关闭；`ExplanationScroll` 提供滚动入口。`ShowAt(content, position, available)` 用 `ContentStyle` 展示普通内容，不启用来源保留。

### 过渡和视觉层

[UiTimeline](runtime/UiTimeline.cs) 提供 `Animate(owner, seconds, sample, channel, complete, particles)`、`Advance(channel, delta)`、`Clear(root, finish)`、`FinishAll()`、`Enter(owner, delay, channel)` 和 `Pulse(owner, color, channel)`。整数通道不包含业务语义；宿主决定何时推进、暂停和加速。完成前移出队列，允许完成回调启动后续表现；回调不得用于规则或持久化操作。

同一目标开始新过渡时结束旧过渡；达到 `MaxEffects`／`MaxParticles` 预算时结束最早的过渡，单项超预算立即结束。`Immediate` 跳过排队，`DurationLimit` 限制新过渡时长；切换设置时宿主调用 `FinishAll()` 同步现有过渡。`Clear(root, false)` 取消而不调用采样或完成；已释放或离树目标在推进时移除，不回调；调度器退出时丢弃队列。旧 `UiMotion` 保留 Tween 返回值与树暂停约定，用于原有调用。

[SurfaceMotion](runtime/SurfaceMotion.cs) 是 `RefCounted`，只操作指定 `Visual` 子控件。宿主配置惯性模式、稳定显示、静止漂移、选中、阅读起点、相位、偏移及震动开关，再调用 `Step(delta, pointer, normalized, enabled, frozen, triggerSpeed, force)`。`pointer` 是同一画布坐标中的指针，`normalized` 是相对稳定槽位的归一化坐标。`SetHover(value, pointer, direction, enabled)`、`Trigger()`、`Shake()` 启动反馈；角度限于 ±3°，读取区域静止后收稳，禁用时可在暂停中立即复位。`Transformed` 信号供宿主同步可见命中或连线。可选 `Shadow` 同步抬升阴影，`Impact`／`Triggered` 提供样式反馈值。组件不修改布局槽位，不接收游戏实例或随机源。

### 控件、纹理与几何

- [UiStyles](runtime/UiStyles.cs)：调用者配置颜色、字号、内边距，`Create(font)` 创建常用控件状态主题，`Box(fill, border, radius, width)` 创建平滑边框，`StyleButton(button, primary)` 设置按钮状态。
- [UiControls](runtime/UiControls.cs)：`Label`、`Button`、`Panel`、`Texture`、`Rule` 创建控件；`PadScroll(scroll, content, padding)` 为旋转、阴影等预留内容边距。无业务回调或音效选择。
- [UiTextBinding](runtime/UiTextBinding.cs)：C# 用 `Bind(owner, Action)`／`RefreshTree(root)`；GDScript 用 `BindTo(owner, Callable)`／`RefreshBindings(root)`，更新已有节点上的文本。
- [UiDropSurface](runtime/UiDropSurface.cs)：`ValidateDrop(position, Variant)` 回调判断合法性；`PositionClicked(position)` 与 `DataDropped(position, data)` 信号交给宿主处理，不限制数据类型。
- [UiArrow](runtime/UiArrow.cs)：`PointFrom(visual, anchor, tip, color)` 从显式锚点绘制箭头；`anchor` 使用来源视觉节点局部坐标，`tip` 使用箭头本地坐标。`Path` 默认 `UiArrowPath.Curved`，设为 `UiArrowPath.Straight` 使用带描边箭头和流动箭纹的直线。
  `PointBetween(from, to, color, gap = 6)` 自动连接两个控件朝向彼此的边缘，支持变换、缩放和旋转；间隔单位为箭头局部坐标。重叠或没有有效间隙时不绘制。绘制时读取最新变换，不会落后于布局 Tween。`Origin`、`Tip` 与 `HasSegment` 可读取当前几何。
  `UpdateSurface(seconds)` 由宿主提供非负秒数；同一时间保持相同流动位置，不读取系统时间或随机数。`SetDetail(2 / 1 / 0)` 分别使用流动箭纹与微光／静态箭纹／静态实线，宿主暂停时保持时间。装饰节点忽略鼠标，不处理目标合法性或输入。
- [UiShaders](runtime/UiShaders.cs)：`CreateMaterial(name)`／C# `Material(name)` 共享 shader 程序，每次返回独立材质。可用名称包括 `card_surface`、`ambient`、`rectangle_edges`、`ripple` 及 0.5.0 的 `highlight_outline`，原始 shader 与 UID 位于 `shaders/`。
- [AmbientView](runtime/AmbientView.cs)：提供纹理后调用 `UpdateSurface(time, enabled, holdPointer)`，由宿主推进时间并决定是否冻结视差。
- [UiBurst](runtime/UiBurst.cs)：配置 `Shape`（`cross`／`rays`／`segments`／`ring`）、`Ink`、`Seed`、`Particles`，调用 `EnableRipple(fractured)` 和 `Sample(progress, detailed)`。可选 `FloatingLabel`／`LabelOrigin` 用于数字弹出；节点自身不定义数字含义。粒子数应向 `UiTimeline.Animate` 申报以统一限额。

所有 shader 使用 Compatibility 的 CanvasItem 路径，不使用内置 `TIME`、实时 SubViewport 或规则随机源。`card_surface` 的光泽、揭示、溶解仅影响目标纹理，矩形边缘平滑与动效开关无关。插件不拥有图片、音效、字体资产或专属主题。

GDScript 示例：

```gdscript
var timeline = load("res://addons/max_godot_ui/runtime/UiTimeline.cs").new()
add_child(timeline)
timeline.Animate($Panel, 0.18, Callable(self, "sample_alpha"), 0, Callable(), 0)
# 在宿主时钟中调用；暂停对应通道时不推进它。
timeline.Advance(0, delta)

func sample_alpha(progress: float) -> void:
    $Panel.modulate.a = progress
```

## 验证

在独立宿主分别及同时加载通用插件，检查 C# 和 GDScript 调用、说明框首次定位／缩放／滚动／命中、文本刷新、时间通道与取消、视觉层暂停复位、箭头变换、效果预算、材质独立参数、实际 shader 渲染、编辑器启停及 Debug／ExportRelease。测试宿主和生成文件留在插件仓库外。

## 许可

采用 [MIT License](LICENSE)。随源码保留 Godot 生成的 `.cs.uid` 和 `.gdshader.uid`。
