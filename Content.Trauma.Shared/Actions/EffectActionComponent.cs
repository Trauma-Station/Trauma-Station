using Content.Shared.EntityEffects;
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Actions;

/// <summary>
/// Applies entity effects to the user when performing this action.
/// Does nothing for targeted entities!
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(EffectActionSystem))]
public sealed partial class EffectActionComponent : Component
{
    /// <summary>
    /// The effects to apply.
    /// </summary>
    [DataField(required: true)]
    public EntityEffect[] Effects = default!;
}
