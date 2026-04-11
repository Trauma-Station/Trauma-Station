// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Inventory.Events;
using Content.Trauma.Shared.Wizard;

namespace Content.Trauma.Shared.Heretic.Systems;

public sealed class HereticClothingSystem : EntitySystem
{
    [Dependency] private readonly SharedHereticSystem _heretic = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Components.HereticClothingComponent, BeingEquippedAttemptEvent>(OnEquipAttempt);
    }

    private void OnEquipAttempt(Entity<Components.HereticClothingComponent> ent, ref BeingEquippedAttemptEvent args)
    {
        if (IsTargetValid(args.EquipTarget) && (args.EquipTarget == args.Equipee || IsTargetValid(args.Equipee)))
            return;

        args.Cancel();
        args.Reason = Loc.GetString("heretic-clothing-component-fail");
    }

    private bool IsTargetValid(EntityUid target)
    {
        return _heretic.IsHereticOrGhoul(target) || HasComp<WizardComponent>(target) ||
               HasComp<ApprenticeComponent>(target);
    }
}
