// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Atmos;

namespace Content.Trauma.Shared.Fire;

public sealed class FireImmunitySystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FireImmunityComponent, GetFireProtectionEvent>(OnGetFireProtection);
    }

    private void OnGetFireProtection(Entity<FireImmunityComponent> ent, ref GetFireProtectionEvent args)
    {
        if (args.Target != ent.Owner)
            return;

        args.Multiplier = 0f;
    }
}
