namespace Content.Trauma.Shared.Strip.Components;

/// <summary>
/// Marks an entity's ItemSlots as eligible for the strip-system's "draw weapon" verb.
/// Add this to sheath prototypes.
/// </summary>
[RegisterComponent]
public sealed partial class QuickDrawableComponent : Component;