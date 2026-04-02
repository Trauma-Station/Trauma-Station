// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Inventory;
using Content.Shared.Roles;
using Content.Trauma.Shared.SelfExtinguisher;
using Robust.Shared.Prototypes;
using JetBrains.Annotations;

namespace Content.Trauma.Server.Jobs;

[UsedImplicitly]
public sealed partial class ModifyEnvirosuitSpecial : JobSpecial
{
    // <summary>
    //   The new charges of the envirosuit's self-extinguisher.
    // </summary>
    [DataField(required: true)]
    public int Charges { get; private set; }

    private static readonly ProtoId<SpeciesPrototype> Species = "Plasmaman";

    private const string Slot = "jumpsuit";

    public override void AfterEquip(EntityUid mob)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        if (!entMan.TryGetComponent<HumanoidProfileComponent>(mob, out var humanoid) ||
            humanoid.Species != Species ||
            !entMan.System<InventorySystem>().TryGetSlotEntity(mob, Slot, out var jumpsuit) ||
            jumpsuit is not { } envirosuit ||
            !entMan.TryGetComponent<SelfExtinguisherComponent>(envirosuit, out var selfExtinguisher))
            return;

        entMan.System<SharedSelfExtinguisherSystem>().SetCharges(envirosuit, Charges, selfExtinguisher);
    }
}
