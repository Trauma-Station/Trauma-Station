// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Stealth.Components;
using Content.Shared.Stealth;
using Content.Shared.Damage.Systems;
using Content.Shared.Ninja.Components;
using Content.Shared.Ninja.Systems;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Throwing;

namespace Content.Goobstation.Shared.Stealth;

/// <summary>
/// This handles goobstations additions to stealth system
/// </summary>
public sealed partial class SharedGoobStealthSystem : EntitySystem
{
    [Dependency] private SharedStealthSystem _stealth = default!;
    [Dependency] private SharedNinjaSuitSystem _suit = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<StealthComponent, MeleeAttackEvent> (OnMeleeAttack);
        SubscribeLocalEvent<StealthComponent, SelfBeforeGunShotEvent> (OnGunShootAttack);
        SubscribeLocalEvent<StealthComponent, BeforeDamageChangedEvent>(OnTakeDamage);
        SubscribeLocalEvent<StealthComponent, BeforeThrowEvent>(OnThrow);
    }

    private void OnTakeDamage(Entity<StealthComponent> ent, ref BeforeDamageChangedEvent args)
    {
        if (!ent.Comp.RevealOnDamage)
            return;

        if (!args.Damage.AnyPositive()) // being healed does not reveal
            return;

        if (args.Damage.GetTotal() <= ent.Comp.Threshold) //damage needs to be above threshold
            return;

        _stealth.ModifyVisibility(ent.Owner, ent.Comp.MaxVisibility, ent.Comp);
        TryRevealNinja(ent.Owner);
    }

    private void OnMeleeAttack(Entity<StealthComponent> ent, ref MeleeAttackEvent args)
    {
        if (!ent.Comp.RevealOnAttack)
            return;

        _stealth.ModifyVisibility(ent.Owner, ent.Comp.MaxVisibility, ent.Comp);
        TryRevealNinja(ent.Owner);
    }

    private void OnGunShootAttack(Entity<StealthComponent> ent, ref SelfBeforeGunShotEvent args)
    {
        if (!ent.Comp.RevealOnAttack)
            return;

        _stealth.ModifyVisibility(ent.Owner, ent.Comp.MaxVisibility, ent.Comp);
        TryRevealNinja(ent.Owner);
    }

    private void OnThrow(Entity<StealthComponent> ent, ref BeforeThrowEvent args)
    {
        if (!ent.Comp.RevealOnAttack)
            return;

        _stealth.ModifyVisibility(ent.Owner, ent.Comp.MaxVisibility, ent.Comp);
        TryRevealNinja(ent.Owner);
    }

    public void TryRevealNinja(EntityUid uid)
    {
        if (!TryComp(uid, out SpaceNinjaComponent? ninja))
            return;

        if (ninja.Suit is { } suit
            && TryComp<NinjaSuitComponent>(suit, out var suitComp))
            _suit.RevealNinja((suit, suitComp), uid, true);
    }
}
