using Content.Goobstation.Shared.EntityEffects.Effects;
using Content.Server.Polymorph.Components;
using Content.Server.Polymorph.Systems;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Polymorph;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Server.EntityEffects.Effects;

public sealed class SpeciesChangeEffectSystem : SharedSpeciesChangeEffectSystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;

    public override void Polymorph(EntityUid target, ProtoId<SpeciesPrototype> id)
    {
        if (!_proto.Resolve(id, out var species))
            return;

        var config = new PolymorphConfiguration
        {
            Entity = species.Prototype,
            TransferDamage = true,
            Forced = true,
            Inventory = PolymorphInventoryChange.Transfer,
            RevertOnCrit = false,
            RevertOnDeath = false,
            TransferName = true,
        };

        if (_polymorph.PolymorphEntity(target, config) is {} uid)
            RemComp<PolymorphedEntityComponent>(uid);
    }
}
