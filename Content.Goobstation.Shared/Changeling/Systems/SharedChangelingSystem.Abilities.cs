// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Goobstation.Shared.Changeling.Actions;
using Content.Goobstation.Shared.Changeling.Components;
using Content.Shared._Goobstation.Weapons.AmmoSelector;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Mind;

namespace Content.Goobstation.Shared.Changeling.Systems;

public abstract partial class SharedChangelingSystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SelectableAmmoSystem _selectableAmmo = default!;
    [Dependency] protected readonly SharedMindSystem Mind = default!;
    [Dependency] protected readonly SharedHandsSystem Hands = default!;

    protected virtual void InitAbilities()
    {
        SubscribeLocalEvent<ChangelingIdentityComponent, LingToggleItemEvent>(OnToggleItem);
        SubscribeLocalEvent<ChangelingIdentityComponent, ToggleDartGunEvent>(OnToggleDartGun);
        SubscribeLocalEvent<ChangelingIdentityComponent, CreateBoneShardEvent>(OnCreateBoneShard);
        SubscribeLocalEvent<ChangelingIdentityComponent, ToggleChitinousArmorEvent>(OnToggleArmor);
    }

    #region Combat Abilities

    private void OnToggleItem(EntityUid uid, ChangelingIdentityComponent comp, ref LingToggleItemEvent args)
    {
        if (!TryToggleItem(uid, args.Item, comp, out _))
            return;

        PlayMeatySound(uid, comp, predicted: true);
        args.Handled = true;
    }

    private void OnToggleDartGun(EntityUid uid, ChangelingIdentityComponent comp, ref ToggleDartGunEvent args)
    {
        if (!Mind.TryGetMind(uid, out var mindId, out _) || !TryComp(mindId, out ActionsContainerComponent? container))
            return;

        if (!TryToggleItem(uid, DartGunPrototype, comp, out var dartgun))
            return;

        if (!TryComp(dartgun, out AmmoSelectorComponent? ammoSelector))
        {
            Log.Error($"Changeling dartgun {ToPrettyString(dartgun)} of {ToPrettyString(uid)} was missing AmmoSelectorComponent!");
            PredictedDel(dartgun);
            PlayMeatySound(uid, comp, predicted: true);
            return;
        }

        var setProto = false;
        foreach (var ability in container.Container.ContainedEntities)
        {
            if (!TryComp(ability, out ChangelingReagentStingComponent? sting) || sting.DartGunAmmo == null)
                continue;

            ammoSelector.Prototypes.Add(sting.DartGunAmmo.Value);

            if (setProto)
                continue;

            _selectableAmmo.TrySetProto((dartgun.Value, ammoSelector), sting.DartGunAmmo.Value);
            setProto = true;
        }

        if (ammoSelector.Prototypes.Count == 0)
        {
            Popup.PopupClient(Loc.GetString("changeling-dartgun-no-stings"), uid, uid);
            comp.Equipment.Remove(DartGunPrototype);
            Dirty(uid, comp);
            PredictedDel(dartgun.Value);
            return;
        }

        args.Handled = true;

        Dirty(dartgun.Value, ammoSelector);

        PlayMeatySound(uid, comp, predicted: true);
    }

    private void OnCreateBoneShard(EntityUid uid, ChangelingIdentityComponent comp, ref CreateBoneShardEvent args)
    {
        var star = PredictedSpawnAtPosition(BoneShardPrototype, Transform(uid).Coordinates);
        Hands.TryPickupAnyHand(uid, star);

        PlayMeatySound(uid, comp, predicted: true);
    }

    private void OnToggleArmor(EntityUid uid, ChangelingIdentityComponent comp, ref ToggleChitinousArmorEvent args)
    {
        if (!TryToggleArmor(uid, comp, [(ArmorHelmetPrototype, "head"), (ArmorPrototype, "outerClothing")]))
        {
            Popup.PopupClient(Loc.GetString("changeling-equip-armor-fail"), uid, uid);
            return;
        }

        // don't take chemicals if the armor was removed
        args.Handled = comp.ActiveArmor != null;
    }

    #endregion
}
