// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.Movement.Components;
using Content.Trauma.Common.Heretic;
using Content.Trauma.Shared.AudioMuffle;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Lock;

namespace Content.Trauma.Shared.Heretic.Systems.PathSpecific.Lock;

public abstract class SharedDigitalCamouflageSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        Subs.SubscribeWithRelay<DigitalCamouflageComponent, CanSeeOnCameraEvent>(OnCanSee, held: false);

        SubscribeLocalEvent<InventoryComponent, ExamineAttemptEvent>(OnExamineAttempt);
        SubscribeLocalEvent<DigitalCamouflageComponent, ExamineAttemptEvent>(OnExamineAttempt);
    }

    private void OnExamineAttempt(EntityUid uid, Component comp, ExamineAttemptEvent args)
    {
        if (!TryComp(args.Examiner, out RelayInputMoverComponent? relay) || !HasComp<AiEyeComponent>(relay.RelayEntity))
            return;

        var ev = new CanSeeOnCameraEvent(uid);
        RaiseLocalEvent(uid, ref ev);
        if (ev.Cancelled)
            args.Cancel();
    }

    private void OnCanSee(Entity<DigitalCamouflageComponent> ent, ref CanSeeOnCameraEvent args)
    {
        args.Cancelled = true;
    }
}
