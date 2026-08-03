// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Damage;
using Content.Medical.Common.Targeting;
using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.Damage.Systems;
using Content.Shared.Popups;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Vampires.Dantalion;

public sealed partial class BloodBondSystem : EntitySystem
{
    // Note: This system is too hardcoded design-wise but i cbf to think of how to make it more generic like the others
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private AlertsSystem _alert = default!;
    [Dependency] private VampireSystem _vampire = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    private HashSet<Entity<VampireThrallComponent>> _thralls = new();
    private HashSet<Entity<BloodBondLinkedComponent>> _bloodLinked = new();

    private static readonly ProtoId<AlertPrototype> BloodBondAlert = "BloodBond";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VampireThrallsComponent, BloodBondActionEvent>(OnBloodBond);

        SubscribeLocalEvent<BloodBondLinkedComponent, MoveEvent>(OnMove);
        SubscribeLocalEvent<BloodBondLinkedComponent, DamageModifyEvent>(OnDamageModify);

        SubscribeLocalEvent<BloodBondLinkedComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<BloodBondLinkedComponent, ComponentShutdown>(OnShutdown);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;

        var eqe = EntityQueryEnumerator<ActiveBloodLinkerComponent, VampireComponent>();
        while (eqe.MoveNext(out var uid, out var bloodLinker, out var vampire))
        {
            if (now < bloodLinker.NextUpdate)
                continue;

            var vamp = (uid, vampire);
            if (!_vampire.HasUsableBlood(vamp, bloodLinker.BloodDrain))
            {
                _actions.SetToggled(bloodLinker.Action, false);

                ClearBloodLinked(uid);
                RemCompDeferred(uid, bloodLinker);

                _popup.PopupEntity("You don't have enough power to continue the link!", uid, PopupType.MediumCaution);
                continue;
            }

            _vampire.SubtractUsableBlood(vamp, bloodLinker.BloodDrain);

            bloodLinker.NextUpdate = bloodLinker.Update + now;
            Dirty(uid, bloodLinker);
        }
    }

    private void OnBloodBond(Entity<VampireThrallsComponent> ent, ref BloodBondActionEvent args)
    {
        var user = ent.Owner;

        args.Toggle = !args.Toggle;
        Dirty(args.Action);

        if (args.Toggle)
        {
            _popup.PopupEntity("You start the blood bond!", user, PopupType.MediumCaution);
            var xform = Transform(user);

            _thralls.Clear();
            _lookup.GetEntitiesInRange(xform.Coordinates, args.Range, _thralls);
            foreach (var thrall in _thralls)
            {
                // We only check for thralls that we own.
                if (!ent.Comp.Thralls.Contains(thrall))
                    return;

                var comp = EnsureComp<BloodBondLinkedComponent>(thrall);
                comp.Vampire = user;
                comp.Range = args.Range;
                Dirty(thrall, comp);
            }

            // Vampire is the one doing the linking, once it can't drain anymore, it removes the link
            var linker = EnsureComp<ActiveBloodLinkerComponent>(user);
            linker.Action = args.Action;
            linker.BloodDrain = args.BloodDrain;
            linker.Update = args.Update;
            linker.NextUpdate = linker.Update + _timing.CurTime;
            Dirty(user, linker);

            // Vampire also shares damage between linked thralls
            var vampLinked = EnsureComp<BloodBondLinkedComponent>(user);
            vampLinked.Vampire = user;
            vampLinked.Range = args.Range;
            Dirty(user, vampLinked);

            args.Handled = true;

            return;
        }

        _popup.PopupEntity("The blood bond halts!", user, PopupType.MediumCaution);

        ClearBloodLinked(user);
        RemCompDeferred<ActiveBloodLinkerComponent>(user);
    }

    private void OnMove(Entity<BloodBondLinkedComponent> ent, ref MoveEvent args)
    {
        // Vampires are the ones linking so we don't have to check about them.
        if (TerminatingOrDeleted(ent.Comp.Vampire) || ent.Owner == ent.Comp.Vampire)
            return;

        var vampireXform = Transform(ent.Comp.Vampire);
        var userXform = Transform(ent.Owner);

        var vampirePos = _transform.GetMapCoordinates(vampireXform);
        var userPos =  _transform.GetMapCoordinates(userXform);

        // Remove the component itself when getting out of range.
        if ((userPos.Position - vampirePos.Position).Length() <= ent.Comp.Range)
            RemCompDeferred(ent.Owner, ent.Comp);
    }

    private void OnDamageModify(Entity<BloodBondLinkedComponent> ent, ref DamageModifyEvent args)
    {
        // Before getting any damage, we must gather all who are linked to the same vampire as us (including the vampire).
        _bloodLinked.Clear();

        var bloodLinkedQuery = EntityQueryEnumerator<BloodBondLinkedComponent>();
        while (bloodLinkedQuery.MoveNext(out var uid, out var bloodLink))
        {
            // Skip those who don't belong on the same vampire, or ourselves (since we modify our damage in this event).
            if (bloodLink.Vampire != ent.Comp.Vampire || uid == ent.Owner)
                continue;

            _bloodLinked.Add((uid, bloodLink));
        }

        if (_bloodLinked.Count == 0)
            return;

        var newDamage = args.Damage / _bloodLinked.Count;
        args.Damage = newDamage;

        // Share damage equally between everyone.
        foreach (var bloodLink in _bloodLinked)
        {
            _damage.ChangeDamage(
                ent: bloodLink.Owner,
                damage: newDamage,
                ignoreResistances: true,
                targetPart: TargetBodyPart.All,
                splitDamage: SplitDamageBehavior.SplitEnsureAll,
                canMiss: false);
        }
    }

    private void OnStartup(Entity<BloodBondLinkedComponent> ent, ref ComponentStartup args)
    {
        _alert.ShowAlert(ent.Owner, BloodBondAlert);
    }

    private void OnShutdown(Entity<BloodBondLinkedComponent> ent, ref ComponentShutdown args)
    {
        _alert.ClearAlert(ent.Owner, BloodBondAlert);
    }

    #region Helper

    /// <summary>
    /// Clears all thralls linked to this vampire.
    /// </summary>
    private void ClearBloodLinked(EntityUid vampire)
    {
        var bloodLinked = EntityQueryEnumerator<BloodBondLinkedComponent>();
        while (bloodLinked.MoveNext(out var uid, out var bloodLink))
        {
            if (bloodLink.Vampire != vampire)
                continue;

            RemCompDeferred(uid, bloodLink);
        }
    }
    #endregion
}
