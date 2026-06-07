// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage.Systems;
using Content.Shared.Hands;
using Robust.Shared.Containers;

namespace Content.Trauma.Shared.Mail;

/// <summary>
/// A placeholder for another entity, spawned when taken out of a container, with the placeholder deleted shortly after.
/// Useful for storing instant effect entities, e.g. smoke, in the mail.
/// </summary>
public sealed class DelayedItemSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        // destroy the placeholder if dropped or equipped
        SubscribeLocalEvent<DelayedItemComponent, DropAttemptEvent>(Destroy);
        SubscribeLocalEvent<DelayedItemComponent, GotEquippedHandEvent>(Destroy);
        // spawn the intended entity after damaged or removed from a container
        SubscribeLocalEvent<DelayedItemComponent, DamageDealtEvent>(SpawnItem);
        SubscribeLocalEvent<DelayedItemComponent, EntGotRemovedFromContainerMessage>(SpawnItem);
    }

    private void SpawnItem<T>(Entity<DelayedItemComponent> ent, ref T args) where T : notnull
    {
        if (TerminatingOrDeleted(ent))
            return;

        PredictedSpawnNextToOrDrop(ent.Comp.Item, ent);
        PredictedQueueDel(ent);
    }

    private void Destroy<T>(Entity<DelayedItemComponent> ent, ref T args) where T : notnull
    {
        PredictedDel(ent.Owner);
    }
}
