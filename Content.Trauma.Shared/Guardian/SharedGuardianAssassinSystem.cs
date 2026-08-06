// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Shared.Guardian.Components;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Content.Shared.Weapons.Melee.Events;
using Content.Trauma.Shared.Guardian.Components;

namespace Content.Trauma.Shared.Guardian;

/// <summary>
/// Handles the Assassin holoparasite variant: its stealth burst and its poisoned blades.
/// The assassin's melee attacks are amplified while stealthed, and it reveals itself when
/// it deals or takes damage.
/// </summary>
public abstract partial class SharedGuardianAssassinSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GuardianAssassinComponent, GuardianStealthToggleEvent>(OnStealthToggle);
        SubscribeLocalEvent<GuardianAssassinComponent, GetUserMeleeDamageEvent>(OnGetUserMeleeDamage);
    }

    private void OnStealthToggle(Entity<GuardianAssassinComponent> ent, ref GuardianStealthToggleEvent args)
    {
        if (args.Handled)
            return;

        GuardianComponent? guardian = null;
        if (!Resolve(ent, ref guardian) || !guardian.GuardianLoose)
        {
            _popup.PopupEntity(Loc.GetString("guardian-assassin-not-manifested"), ent, ent, PopupType.MediumCaution);
            return;
        }

        if (_statusEffects.HasStatusEffect(ent, ent.Comp.StealthEffect))
        {
            // Already stealthed: drop the burst early.
            _statusEffects.TryRemoveStatusEffect(ent, ent.Comp.StealthEffect);
            _popup.PopupEntity(Loc.GetString("guardian-assassin-stealth-end"), ent, ent, PopupType.Medium);
            args.Handled = true;
            return;
        }

        if (!_statusEffects.TryAddStatusEffectDuration(ent, ent.Comp.StealthEffect, ent.Comp.StealthDuration))
        {
            _popup.PopupEntity(Loc.GetString("guardian-assassin-stealth-fail"), ent, ent, PopupType.MediumCaution);
            return;
        }

        _popup.PopupEntity(Loc.GetString("guardian-assassin-stealth-start"), ent, ent, PopupType.Medium);

        args.Handled = true;
    }

    private void OnGetUserMeleeDamage(Entity<GuardianAssassinComponent> ent, ref GetUserMeleeDamageEvent args)
    {
        if (!_statusEffects.HasStatusEffect(ent, ent.Comp.StealthEffect))
            return;

        // The next attack out of stealth replaces the regular melee damage, amplified while stealthed.
        args.Damage = ent.Comp.StealthAttackDamage * ent.Comp.StealthDamageMultiplier;
    }
}
