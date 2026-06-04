// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Clothing;
using Content.Shared.FixedPoint;
using Content.Shared.Hands;
using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;

namespace Content.Trauma.Shared.Knowledge.Attribute.Systems;

public sealed partial class WeightAttributeSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KnowledgeHolderComponent, DidEquipHandEvent>(OnCarry);
        SubscribeLocalEvent<KnowledgeHolderComponent, DidUnequipHandEvent>(OnUncarry);
        SubscribeLocalEvent<KnowledgeHolderComponent, ClothingDidEquippedEvent>(OnEquip);
        SubscribeLocalEvent<KnowledgeHolderComponent, ClothingDidUnequippedEvent>(OnUnequip);
    }

    private void OnCarry(Entity<KnowledgeHolderComponent> ent, ref DidEquipHandEvent args)
    {
        if (!TryComp<PhysicsComponent>(args.Equipped, out var fixtures))
            return;

        AdjustWeight(ent, fixtures.FixturesMass);
    }

    private void OnUncarry(Entity<KnowledgeHolderComponent> ent, ref DidUnequipHandEvent args)
    {
        if (!TryComp<PhysicsComponent>(args.Unequipped, out var fixtures))
            return;

        AdjustWeight(ent, -fixtures.FixturesMass);
    }

    private void OnEquip(Entity<KnowledgeHolderComponent> ent, ref ClothingDidEquippedEvent args)
    {
        if (!TryComp<PhysicsComponent>(args.Clothing, out var fixtures))
            return;

        AdjustWeight(ent, fixtures.FixturesMass);
    }

    private void OnUnequip(Entity<KnowledgeHolderComponent> ent, ref ClothingDidUnequippedEvent args)
    {
        if (!TryComp<PhysicsComponent>(args.Clothing, out var fixtures))
            return;

        AdjustWeight(ent, -fixtures.FixturesMass);
    }

    public void AdjustWeight(Entity<KnowledgeHolderComponent> ent, FixedPoint2 weight)
    {
        var selfEv = new GetCarryLimitsEvent();
        RaiseLocalEvent(ent.Owner, ref selfEv);

        // TODO: Weight time
        //selfEv.Weight += weight;
    }
}
