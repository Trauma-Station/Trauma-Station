// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.StatusEffectNew.Components;
using Content.Trauma.Common.Heretic;
using Content.Trauma.Shared.Heretic.Components.Side;
using Content.Trauma.Shared.Heretic.Events;
using Content.Trauma.Shared.Heretic.Systems;

namespace Content.Trauma.Server.Heretic.Systems;

public sealed class MansusGraspSystem : SharedMansusGraspSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StatusEffectContainerComponent, ParentPacketReceiveAttemptEvent>(OnPacket);
        SubscribeLocalEvent<MansusGraspUpgradeComponent, AfterTouchSpellAbilityUsedEvent>(OnAfterTouchSpell);
    }

    private void OnAfterTouchSpell(Entity<MansusGraspUpgradeComponent> ent, ref AfterTouchSpellAbilityUsedEvent args)
    {
        EntityManager.AddComponents(args.TouchSpell, ent.Comp.AddedComponents);
    }

    private void OnPacket(Entity<StatusEffectContainerComponent> ent, ref ParentPacketReceiveAttemptEvent args)
    {
        if (Status.HasStatusEffect(ent, GraspAffectedStatus))
            args.Cancelled = true;
    }
}
