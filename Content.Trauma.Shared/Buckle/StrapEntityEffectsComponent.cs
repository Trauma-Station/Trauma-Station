using Content.Shared.EntityEffects;
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Buckle;

/// <summary>
/// Applies entity effects to whatever is strapped to this entity.
/// They are applied immediately on buckle and periodically after that.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(StrapEntityEffectsSystem))]
public sealed partial class StrapEntityEffectsComponent : Component
{
    /// <summary>
    /// The effects to apply.
    /// </summary>
    [DataField(required: true)]
    public EntityEffect[] Effects = default!;

    /// <summary>
    /// How long to wait between applying effects.
    /// </summary>
    [DataField]
    public TimeSpan UpdateDelay = TimeSpan.FromSeconds(1);
}
