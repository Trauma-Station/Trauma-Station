// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.VoiceMask;
using Content.Server.VoiceMask;
using Content.Shared.Chat.RadioIconsEvents;
using Content.Shared.Implants;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Roles.Jobs;
using Content.Shared.StatusIcon;
using Content.Shared.VoiceMask;

namespace Content.Goobstation.Server.VoiceMask;

public sealed partial class VoiceMaskSystemGoob : EntitySystem
{
    [Dependency] private SharedJobSystem _job = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private VoiceMaskSystem _voiceMask = default!;

    public override void Initialize()
    {
        base.Initialize();

        Subs.BuiEvents<VoiceMaskComponent>(VoiceMaskUIKey.Key, subs =>
        {
            subs.Event<VoiceMaskChangeJobIconMessage>(OnChangeJobIcon);
        });
    }

    private void OnChangeJobIcon(Entity<VoiceMaskComponent> entity, ref VoiceMaskChangeJobIconMessage ev)
    {
        if (!ProtoMan.TryIndex<JobIconPrototype>(ev.JobIcon, out var proto))
            return;

        entity.Comp.JobIconProtoId = proto.ID;

        entity.Comp.JobName = _job.TryFindJobFromIcon(proto, out var job) ? job.LocalizedName : null;

        _popup.PopupEntity(Loc.GetString("voice-mask-popup-success"), entity, ev.Actor);
        _voiceMask.UpdateUI(entity);
    }

    [SubscribeLocalEvent]
    private void OnTransformJobIcon(Entity<VoiceMaskComponent> ent, ref ImplantRelayEvent<TransformSpeakerJobIconEvent> args)
    {
        TransformJobIcon(ent, ref args.Args);
    }

    [SubscribeLocalEvent]
    private void OnTransformJobIcon(Entity<VoiceMaskComponent> ent, ref InventoryRelayedEvent<TransformSpeakerJobIconEvent> args)
    {
        TransformJobIcon(ent, ref args.Args);
    }

    private void TransformJobIcon(Entity<VoiceMaskComponent> ent, ref TransformSpeakerJobIconEvent args)
    {
        if (!ent.Comp.Active)
            return;

        if (ent.Comp.JobIconProtoId is { } jobIcon)
            args.JobIcon = jobIcon;

        if (!string.IsNullOrWhiteSpace(ent.Comp.JobName))
            args.JobName = ent.Comp.JobName;
    }
}
