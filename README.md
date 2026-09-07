# MaxGodotUI

Godot .NET 通用 UI 与视觉表现插件。当前 `0.2.0` 提供悬停内容定位、位移动画和颜色闪烁。运行时无需启用编辑器插件；组件不负责宿主规则或流程。

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
- Tween 绑定目标生命周期，场景树暂停时停止。宿主需避免对同一属性同时发起冲突动画，不用动画回调计算规则结果。

GDScript 示例：

```gdscript
var motion = load("res://addons/max_godot_ui/runtime/UiMotion.cs").new()
add_child(motion)
var tween: Tween = motion.Move($Panel, Vector2(100, 50), 0.2)
await tween.finished
```

宿主提供主题、文本与输入规则；组件不识别卡牌、战斗或平台事件。后续按实际需求增加震动等视觉表现，不提供声音、存档或设置模块。

## 验证

在独立宿主分别及同时加载通用插件，检查 GDScript 调用、面板首次边缘定位、移动／闪烁、暂停、编辑器启停及 ExportRelease 构建。测试宿主和生成文件留在插件仓库外。

## 许可

采用 [MIT License](LICENSE)。随源码保留 Godot 生成的 `.cs.uid`。
