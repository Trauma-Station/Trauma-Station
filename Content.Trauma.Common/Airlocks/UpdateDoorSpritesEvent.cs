namespace Content.Trauma.Common.Airlocks;

[ByRefEvent]
public record struct UpdateDoorSpritesEvent(EntityPrototype Proto, bool Handled = false);
