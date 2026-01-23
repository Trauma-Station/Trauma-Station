// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.EntityConditions;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory.Events;

namespace Content.Trauma.Shared.Clothing;

public sealed class ClothingConditionsSystem : EntitySystem
{
    [Dependency] private readonly SharedEntityConditionsSystem _conditions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClothingConditionsComponent, BeingEquippedAttemptEvent>(OnEquipAttempt);
    }

    private void OnEquipAttempt(Entity<ClothingConditionsComponent> ent, ref BeingEquippedAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        var target = args.EquipTarget;
        if (_conditions.TryConditions(target, ent.Comp.Conditions))
            return;

        var identity = Identity.Entity(target, EntityManager);
        var user = args.Equipee;
        args.Reason = Loc.GetString(ent.Comp.Reason, ("target", identity), ("self", target == user));
        args.Cancel();
    }
}
