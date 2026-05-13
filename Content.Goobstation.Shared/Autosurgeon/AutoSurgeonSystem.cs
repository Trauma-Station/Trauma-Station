// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Body;
using Content.Medical.Shared.Body;
using Content.Shared.Buckle.Components;
using Content.Shared.Body;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Forensics;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using System.Linq;

namespace Content.Goobstation.Shared.Autosurgeon;

// There might be some goidacode inside, I warned you.
// It should also maybe be in _Shitmed instead of here, but who cares.
public sealed partial class AutoSurgeonSystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private BodyPartSystem _part = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AutoSurgeonComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<AutoSurgeonComponent, AutoSurgeonDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<AutoSurgeonComponent, ExaminedEvent>(OnExamined);
    }

    private void OnActivate(Entity<AutoSurgeonComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        ent.Comp.ActiveSound = _audio.Stop(ent.Comp.ActiveSound);

        var user = args.User;
        var name = Name(ent);
        if (ent.Comp.Used)
        {
            _popup.PopupClient($"The {name} has already been used!", ent, user, PopupType.SmallCaution);
            return;
        }

        var buckled = Comp<StrapComponent>(ent).BuckledEntities;
        if (buckled.Count == 0)
        {
            _popup.PopupClient($"Nothing is strapped to the {name}!", ent, user, PopupType.SmallCaution);
            return;
        }

        var target = buckled.First();
        if (!HasComp<BodyComponent>(target))
        {
            _popup.PopupClient($"{Name(target)} can't be operated on!", ent, user, PopupType.SmallCaution);
            return;
        }

        if (!_doAfter.TryStartDoAfter(new DoAfterArgs(
                EntityManager,
                ent.Owner,
                ent.Comp.DoAfterTime,
                new AutoSurgeonDoAfterEvent(),
                ent.Owner,
                target: target,
                used: ent.Owner)
            {
                BreakOnMove = true,
                DistanceThreshold = 0.1f,
                MovementThreshold = 0.1f,
            }))
            return;

        _popup.PopupClient("You start up the {name}...", ent, user, PopupType.Medium);

        var ev = new TransferDnaEvent { Donor = user, Recipient = ent };
        RaiseLocalEvent(user, ref ev);

        if (_net.IsClient) // Fuck sound networking
            return;

        if (_audio.PlayPvs(ent.Comp.Sound, ent) is {} sound)
            ent.Comp.ActiveSound = sound.Entity;
    }

    private void OnDoAfter(Entity<AutoSurgeonComponent> ent, ref AutoSurgeonDoAfterEvent args)
    {
        _audio.Stop(ent.Comp.ActiveSound);
        ent.Comp.ActiveSound = null;

        if (args.Cancelled || ent.Comp.Used || args.Target is not {} target)
            return;

        var coords = Transform(target).Coordinates;
        foreach (var entry in ent.Comp.Entries)
        {
            if (_body.GetOrgan(target, entry.TargetCategory) is not {} organ)
                continue;

            if (entry.NewOrganProto is {} proto)
            {
                if (!TryComp<BodyPartComponent>(organ, out var part))
                {
                    Log.Error($"{ToPrettyString(ent)} had non-part {ToPrettyString(organ)} for {entry.TargetCategory} it tried to add {proto} to!");
                    continue;
                }

                var parent = (organ, part);
                var newPart = PredictedSpawnAtPosition(proto, coords);
                if (_body.GetCategory(newPart) is not {} category || !_part.HasOrganSlot(parent, category))
                {
                    // you are missing its slot sorry chud
                    PredictedDel(newPart);
                    continue;
                }

                if (_part.GetOrgan(parent, category) is {} oldPart)
                    _part.RemoveOrgan(parent, oldPart);

                if (!_part.InsertOrgan(parent, newPart))
                    Log.Error($"{ToPrettyString(ent)} failed to install {ToPrettyString(newPart)} into {ToPrettyString(target)}!");
                continue;
            }

            // If we didn't replace it, then we try to upgrade it.

            // TODO: continue if all OrganComponents are present on the organ
            if (entry.OrganComponents is {} organComps)
                EntityManager.AddComponents(organ, organComps);

            if (entry.UserComponents is {} comps)
            {
                var components = EnsureComp<OrganComponentsComponent>(organ);
                // add any extra components to the user and update the organ so if it's transplanted to someone else they get it too
                var added = new ComponentRegistry();
                components.OnAdd ??= new();
                foreach (var (name, data) in comps)
                {
                    if (components.OnAdd.TryAdd(name, data))
                        added.Add(name, data);
                }
                Dirty(organ, components);
                EntityManager.AddComponents(target, added);
            }
        }

        if (ent.Comp.OneTimeUse)
            ent.Comp.Used = true;
        Dirty(ent);
    }

    private void OnExamined(Entity<AutoSurgeonComponent> ent, ref ExaminedEvent args) =>
        args.PushMarkup(ent.Comp.Used ? Loc.GetString("gun-cartridge-spent") : Loc.GetString("gun-cartridge-unspent")); // Yes gun locale, and?
}

[Serializable, NetSerializable]
public sealed partial class AutoSurgeonDoAfterEvent : SimpleDoAfterEvent;
