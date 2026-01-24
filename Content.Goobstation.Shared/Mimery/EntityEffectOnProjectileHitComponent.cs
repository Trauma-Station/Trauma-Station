using Content.Shared.EntityEffects;
using Robust.Shared.GameStates;

namespace Content.Goobstation.Shared.Mimery;

[RegisterComponent, NetworkedComponent]
public sealed partial class EntityEffectOnProjectileHitComponent : Component
{
    [DataField]
    public List<EntityEffect> Effects = new();
}
