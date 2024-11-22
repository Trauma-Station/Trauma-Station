using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.Implants;

/// <summary>
/// Implant component that applies effects to the implanted mob when added or removed.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ImplantEffectsComponent : Component
{
    [DataField]
    public EntityEffect[]? Added;

    [DataField]
    public EntityEffect[]? Removed;
}
