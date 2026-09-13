using Godot;

namespace MaxGodotUI;

// Godot's managed Callable supports delegates and object methods. Native GDScript
// lambdas/bound callables cannot round-trip through it (engine issue #97358).
internal static class UiCallback
{
    public static bool Valid(Callable callback) => callback.Delegate != null ||
        GodotObject.IsInstanceValid(callback.Target) && callback.Target.HasMethod(callback.Method);
    public static Variant Invoke(Callable callback, params Variant[] arguments) => callback.Call(arguments);
}
