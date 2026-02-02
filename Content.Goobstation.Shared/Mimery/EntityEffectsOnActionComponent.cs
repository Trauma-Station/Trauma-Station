using Content.Shared.EntityEffects;
using Robust.Shared.GameStates;

namespace Content.Goobstation.Shared.Mimery;

[RegisterComponent, NetworkedComponent]
public sealed partial class EntityEffectOnActionComponent : Component
{
    [DataField]
    public float Scale = 1f;

    [DataField(required: true)]
    public EntityEffect[] Effects;
}
