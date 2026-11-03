using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.Megafauna;

/// <summary>
/// Component that runs effects once megafauna starts calculating logic
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MegafaunaStartupEffectsComponent : Component
{
    [DataField(required: true)]
    public EntityEffect[] Effects = default!;
}
