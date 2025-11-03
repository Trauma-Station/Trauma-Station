using Content.Shared.EntityEffects;
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Weapons.Hitscan;

/// <summary>
/// Applies entity effects to targets hit by a hitscan.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(HitscanEntityEffectsSystem))]
public sealed partial class HitscanEntityEffectsComponent : Component
{
    [DataField(required: true)]
    public EntityEffect[] Effects = default!;
}
