// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Weapons.Melee;
using Content.Shared.Whitelist;
using Content.Trauma.Shared.BloodCult;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

namespace Content.Trauma.Shared.Whetstone;

// TODO: move to shared bruh
public sealed partial class WhetstoneSystem : EntitySystem
{
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WhetstoneComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnAfterInteract(Entity<WhetstoneComponent> stone, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target is not { } target || stone.Comp.Uses <= 0 ||
            !TryComp<MeleeWeaponComponent>(target, out var meleeWeapon) ||
            !HasComp<ItemComponent>(target) || // We don't want to sharpen felinids or vulps
            !_whitelist.CheckBoth(target, stone.Comp.Blacklist, stone.Comp.Whitelist))
            return;

        foreach (var (damageTypeId, value) in stone.Comp.DamageIncrease.DamageDict)
        {
            if (!meleeWeapon.Damage.DamageDict.TryGetValue(damageTypeId, out var defaultDamage) ||
                defaultDamage > stone.Comp.MaximumIncrease)
                continue;

            var newDamage = defaultDamage + value;
            if (newDamage > stone.Comp.MaximumIncrease)
                newDamage = stone.Comp.MaximumIncrease;

            meleeWeapon.Damage.DamageDict[damageTypeId] = newDamage;
        }
        Dirty(target, meleeWeapon);

        _audio.PlayEntity(stone.Comp.SharpenAudio, Filter.Pvs(target), target, true);
        stone.Comp.Uses--;
        Dirty(stone);
        if (stone.Comp.Uses <= 0)
            _appearance.SetData(stone, GenericCultVisuals.State, false);
    }
}
