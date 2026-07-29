// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Stacks;
using Content.Trauma.Shared.Nuclear.Reactor;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Nuclear.Centrifuge;

/// <summary>
/// Handles centrifuge interaction and fuel processing.
/// </summary>
public sealed partial class NuclearCentrifugeSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedPowerReceiverSystem _power = default!;
    [Dependency] private SharedStackSystem _stack = default!;
    [Dependency] private EntityQuery<NuclearPropertiesComponent> _propsQuery = default!;
    [Dependency] private EntityQuery<ReactorPartComponent> _partQuery = default!;
    [Dependency] private EntityQuery<ReactorFuelRodComponent> _fuelQuery = default!;
    [Dependency] private EntityQuery<StackComponent> _stackQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActiveNuclearCentrifugeComponent, ComponentInit>(OnActiveInit);
        SubscribeLocalEvent<NuclearCentrifugeComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<NuclearCentrifugeComponent, PowerChangedEvent>(OnPowerChanged);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<ActiveNuclearCentrifugeComponent, NuclearCentrifugeComponent>();
        var now = _timing.CurTime;
        while (query.MoveNext(out var uid, out var active, out var comp))
        {
            if (now < active.NextExtract)
                continue;

            // keep spawning stacks if there are more than it can fit in 1 item
            var failed = true;
            while (comp.FuelToExtract > 1)
            {
                failed = false;
                var item = PredictedSpawnNextToOrDrop(comp.Result, uid);
                var stack = _stackQuery.Comp(item);
                _stack.SetCount((item, stack), (int) Math.Floor(comp.FuelToExtract));
                _stack.TryMergeToContacts((item, stack, null));
                comp.FuelToExtract -= stack.Count;
            }
            Dirty(uid, comp);

            RemComp(uid, active);

            // clients will predict it so server doesnt care about the sound
            if (_net.IsClient && _timing.IsFirstTimePredicted)
                _audio.PlayPvs(failed ? comp.SoundFail : comp.SoundSucceed, uid);
        }
    }

    private void OnActiveInit(Entity<ActiveNuclearCentrifugeComponent> ent, ref ComponentInit args)
    {
        ent.Comp.NextExtract = _timing.CurTime;
        if (_net.IsServer)
            ent.Comp.AudioProcess = _audio.PlayPvs(ent.Comp.SoundProcess, ent)?.Entity;
        Dirty(ent);

        _appearance.SetData(ent.Owner, NuclearCentrifugeVisuals.Processing, true);
    }

    private void OnActiveShutdown(Entity<ActiveNuclearCentrifugeComponent> ent, ref ComponentShutdown args)
    {
        ent.Comp.AudioProcess = _audio.Stop(ent.Comp.AudioProcess);
        _appearance.SetData(ent.Owner, NuclearCentrifugeVisuals.Processing, false);
    }

    private void OnInteractUsing(Entity<NuclearCentrifugeComponent> ent, ref InteractUsingEvent args)
    {
        if (!_power.IsPowered(ent.Owner))
            return;

        var item = args.Used;
        if (!_partQuery.HasComp(item))
            return;

        args.Handled = true;

        var user = args.User;
        if (!_fuelQuery.HasComp(item) || !_propsQuery.TryComp(item, out var props))
        {
            _popup.PopupEntity(Loc.GetString("nuclear-centrifuge-wrong-item", ("item", item)), ent, user);
            return;
        }

        if (props.SpentFuel < 0.1)
        {
            if (_net.IsServer) // its not networked
                _popup.PopupEntity(Loc.GetString("nuclear-centrifuge-unfit-item", ("item", item)), ent, user);
            return;
        }

        var ident = Identity.Entity(user, EntityManager);
        _popup.PopupEntity(Loc.GetString("nuclear-centrifuge-insert-item", ("user", ident), ("machine", ent.Owner), ("item", item)), ent, user);
        _audio.PlayPredicted(ent.Comp.SoundLoad, ent, user);

        ent.Comp.FuelToExtract += props.SpentFuel;
        Dirty(ent);

        var active = EnsureComp<ActiveNuclearCentrifugeComponent>(ent);
        active.NextExtract += ent.Comp.ExtractTime * props.SpentFuel;
        Dirty(ent, active);

        props.SpentFuel = 0; // bye bye
        PredictedQueueDel(item);
    }

    private void OnPowerChanged(Entity<NuclearCentrifugeComponent> ent, ref PowerChangedEvent args)
    {
        if (!args.Powered)
        {
            // stop when unpowered
            RemComp<ActiveNuclearCentrifugeComponent>(ent);
        }
        else if (ent.Comp.FuelToExtract > 0)
        {
            // restart when repowered
            var active = EnsureComp<ActiveNuclearCentrifugeComponent>(ent);
            active.NextExtract = _timing.CurTime + ent.Comp.ExtractTime * ent.Comp.FuelToExtract;
            Dirty(ent, active);
        }
    }
}
