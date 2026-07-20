// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Weapons.Melee.Events;

namespace Content.Medical.Shared.Augments;

public sealed partial class AugmentRelaySystem : EntitySystem
{
    [Dependency] private AugmentSystem _augment = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InstalledAugmentsComponent, GetUserMeleeDamageEvent>(_augment.RelayEvent);
    }
}
