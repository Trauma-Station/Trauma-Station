// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 pheenty <fedorlukin2006@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Medical.Common.Body;
using Content.Medical.Shared.Body;
using Content.Shared.Body;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Forensics;
using Content.Shared.Item.ItemToggle.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Goobstation.Shared.Autosurgeon;

// There might be some goidacode inside, I warned you.
// It should also maybe be in _Shitmed instead of here, but who cares.
public sealed class AutoSurgeonSystem : EntitySystem
{
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly BodyPartSystem _part = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AutoSurgeonComponent, ItemToggleActivateAttemptEvent>(OnActivated);
        SubscribeLocalEvent<AutoSurgeonComponent, AutoSurgeonDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<AutoSurgeonComponent, ExaminedEvent>(OnExamined);
    }

    // TODO: why are you using an attempt event...
    private void OnActivated(Entity<AutoSurgeonComponent> ent, ref ItemToggleActivateAttemptEvent args)
    {
        _audio.Stop(ent.Comp.ActiveSound);
        ent.Comp.ActiveSound = null;
        args.Cancelled = true;

        if (ent.Comp.Used || args.User == null)
            return;

        if (!_doAfter.TryStartDoAfter(new DoAfterArgs(
                EntityManager,
                ent.Owner,
                ent.Comp.DoAfterTime,
                new AutoSurgeonDoAfterEvent(),
                ent.Owner,
                args.User,
                ent.Owner)
            {
                BreakOnMove = true,
                DistanceThreshold = 0.1f,
                MovementThreshold = 0.1f,
            }))
            return;

        var ev = new TransferDnaEvent { Donor = args.User.Value, Recipient = ent };
        RaiseLocalEvent(args.User.Value, ref ev);

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
