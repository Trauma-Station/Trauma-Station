// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Disposal.Tube;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Trauma.Shared.Disposals;
using Robust.Shared.Audio.Systems;

namespace Content.Trauma.Server.Disposals;

public sealed partial class TraumaServerDisposalsSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<AddExtraTubeSpeedComponent, AfterInteractEvent>(OnInteract);
        SubscribeLocalEvent<AddExtraTubeSpeedComponent, AddTubeSpeedDoAfter>(OnDoAfter);
    }

    private void OnDoAfter(Entity<AddExtraTubeSpeedComponent> ent, ref AddTubeSpeedDoAfter args)
    {
        ent.Comp.SoundStream = _audio.Stop(ent.Comp.SoundStream);
        if (args.Cancelled)
            return;

        if (args.Handled || args.Target is not { } target)
            return;

        if (!TryComp<DisposalTubeComponent>(target, out var tube))
            return;

        args.Handled = true;
        tube.Speed += ent.Comp.Amount;

        PredictedQueueDel(ent);
    }

    private void OnInteract(Entity<AddExtraTubeSpeedComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        if (!TryComp<DisposalTubeComponent>(args.Target, out var tube) || tube.Speed >= tube.MaxUpgradeSpeed)
            return;

        ent.Comp.SoundStream = _audio.PlayPvs(ent.Comp.UpgradeSound, ent)?.Entity;
        Dirty(ent);
        var ev = new AddTubeSpeedDoAfter();
        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, ent.Comp.Delay, ev, ent, target, ent));
    }

}
