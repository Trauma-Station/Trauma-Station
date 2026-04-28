// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Trigger.Systems;
using Content.Trauma.Common.Storage;

namespace Content.Trauma.Shared.Trigger.Triggers;

public sealed class TriggerOnOpenStorageSystem : EntitySystem
{
    [Dependency] private readonly TriggerSystem _trigger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerOnOpenStorageComponent, StorageOpenedEvent>(OnStorageOpened);
    }

    private void OnStorageOpened(Entity<TriggerOnOpenStorageComponent> ent, ref StorageOpenedEvent args)
    {
        _trigger.Trigger(ent, args.User, ent.Comp.KeyOut);
    }
}
