namespace Content.Trauma.Common.MouseWheel;

public abstract class CommonMouseWheelSystem : EntitySystem
{
    public abstract void HandleMouseWheel(Vector2 delta);
}
