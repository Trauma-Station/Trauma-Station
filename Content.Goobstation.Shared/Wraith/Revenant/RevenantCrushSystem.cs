// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Wraith.Curses;
using Content.Goobstation.Shared.Wraith.Events;
using Content.Shared.Administration.Logs;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Gibbing;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Robust.Shared.Audio.Systems;

namespace Content.Goobstation.Shared.Wraith.Revenant;

/// <summary>
/// The target immediately collapses and begins to take a huge amount of brute damage over time
/// as their bones crack and their body implodes. The victim explodes into gibs once this damage becomes lethal,
/// but the process is interrupted if they are removed from your line of sight or you move (or are moved).
/// </summary>
public sealed partial class RevenantCrushSystem : EntitySystem
{
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private GibbingSystem _gibbing = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private ISharedAdminLogManager _admin = default!;

    [SubscribeLocalEvent]
    private void OnRevenantCrush(Entity<RevenantCrushComponent> ent, ref RevenantCrushEvent args)
    {
        if (ent.Comp.InitialDamage == null)
            return;

        if (HasComp<CurseImmuneComponent>(args.Target))
        {
            _popup.PopupEntity(Loc.GetString("revenant-crush-chaplain"), ent.Owner, ent.Owner);
            return;
        }

        var doAftersArgs = new DoAfterArgs(
            EntityManager,
            ent.Owner,
            ent.Comp.AbilityDuration,
            new RevenantCrushDoAfterEvent(),
            ent.Owner,
            args.Target)
        {
            // technically, in order for them to be removed from our line of sight, they need to move...
            BreakOnMove = true,
            MovementThreshold = 0.3f,
            DistanceThreshold = 15f,
        };

        _popup.PopupEntity(Loc.GetString("revenant-crush-start"), args.Target, args.Target, PopupType.MediumCaution);
        _doAfter.TryStartDoAfter(doAftersArgs);
        _audio.PlayPredicted(ent.Comp.CrushSound, args.Target, args.Target);

        _stun.KnockdownOrStun(args.Target, ent.Comp.KnockdownDuration);
        _damage.TryChangeDamage(args.Target, ent.Comp.InitialDamage, true);

        args.Handled = true;
    }

    // TODO: Make it so there's a 25% each second that it plays Flesh_Tear1.ogg and picks one of these three pop-ups revenant-crush-crack1, 2 or 3.
    // TODO: Deal damage to their chest every second. Theoretically 5 damage, but who knows. Doesn't have to be the chest if you don't wanna deal with that.
    [SubscribeLocalEvent]
    private void OnRevenantCrushDoAfter(Entity<RevenantCrushComponent> ent, ref RevenantCrushDoAfterEvent args)
    {
        if (args.Cancelled || args.Target is not { } target)
            return;

        _popup.PopupEntity(Loc.GetString("revenant-crush-you"), target, target);
        _admin.Add(LogType.Gib, LogImpact.High, $"{ent.Owner} gibbed {target} via wraith Crush");
        _gibbing.Gib(target);

        args.Handled = true;
    }
}
