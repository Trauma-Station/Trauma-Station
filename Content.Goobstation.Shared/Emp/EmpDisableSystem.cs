// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Emp;
using Content.Shared.IdentityManagement;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Goobstation.Shared.Emp;

public sealed class EmpDisableSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmpDisabledComponent, ItemToggleActivateAttemptEvent>(OnActivateAttempt);
        SubscribeLocalEvent<EmpDisabledComponent, AttemptShootEvent>(OnShootAttempt);
        SubscribeLocalEvent<EmpDisabledComponent, RefreshChargeRateEvent>(OnRefreshChargeRate,
            after: new[] { typeof(SharedBatterySystem) });
    }

    private void OnShootAttempt(Entity<EmpDisabledComponent> ent, ref AttemptShootEvent args)
    {
        args.Cancelled = true;
        args.Message = Loc.GetString("emp-disabled-activate-attempt",
            ("item", Identity.Entity(ent.Owner, EntityManager)));
    }

    private void OnActivateAttempt(Entity<EmpDisabledComponent> ent, ref ItemToggleActivateAttemptEvent args)
    {
        args.Cancelled = true;
        args.Popup = Loc.GetString("emp-disabled-activate-attempt",
            ("item", Identity.Entity(ent.Owner, EntityManager)));
    }

    private void OnRefreshChargeRate(Entity<EmpDisabledComponent> ent, ref RefreshChargeRateEvent args)
    {
        args.NewChargeRate = 0f; // no charging while disabled
    }
}
