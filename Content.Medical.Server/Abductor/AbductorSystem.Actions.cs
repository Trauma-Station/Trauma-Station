// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Medical.Shared.Abductor;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.DoAfter;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Spawners;
using Robust.Shared.Utility;

namespace Content.Medical.Server.Abductor;

public sealed partial class AbductorSystem : SharedAbductorSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;

    private static readonly EntProtoId<ActionComponent> SendYourself = "ActionSendYourself";
    private static readonly EntProtoId<ActionComponent> ExitAction = "ActionExitConsole";
    private static readonly EntProtoId TeleportationEffect = "EffectTeleportation";
    private static readonly EntProtoId TeleportationEffectEntity = "EffectTeleportationEntity";
    private static readonly EntProtoId TeleportationEffectShort = "EffectTeleportationShort";
    private static readonly EntProtoId TeleportationEffectEntityShort = "EffectTeleportationEntityShort";

    public static readonly SoundSpecifier TeleportSound = new SoundPathSpecifier(new ResPath("/Audio/_Shitmed/Misc/alien_teleport.ogg"));

    public void InitializeActions()
    {
        SubscribeLocalEvent<AbductorScientistComponent, ComponentStartup>(AbductorScientistComponentStartup);

        SubscribeLocalEvent<ExitConsoleEvent>(OnExit);

        SubscribeLocalEvent<AbductorReturnToShipEvent>(OnReturn);
        SubscribeLocalEvent<AbductorScientistComponent, AbductorReturnDoAfterEvent>(OnDoAfterAbductorReturn);

        SubscribeLocalEvent<SendYourselfEvent>(OnSendYourself);
        SubscribeLocalEvent<AbductorScientistComponent, AbductorSendYourselfDoAfterEvent>(OnDoAfterSendYourself);
    }

    private void AbductorScientistComponentStartup(Entity<AbductorScientistComponent> ent, ref ComponentStartup args)
        => ent.Comp.SpawnPosition = EnsureComp<TransformComponent>(ent).Coordinates;

    private void OnReturn(AbductorReturnToShipEvent ev)
    {
        var user = ev.Performer;
        if (!TryComp<AbductorScientistComponent>(user, out var comp))
            return;

        AddTeleportationEffect(user, TeleportationEffectEntityShort);

        if (comp.SpawnPosition is {} pos)
        {
            var effect = Spawn(TeleportationEffectShort, pos);
            _audio.PlayPvs(TeleportSound, effect);
        }

        var doAfter = new DoAfterArgs(EntityManager, ev.Performer, TimeSpan.FromSeconds(3), new AbductorReturnDoAfterEvent(), ev.Performer)
        {
            MultiplyDelay = false,
        };
        _doAfter.TryStartDoAfter(doAfter);
        ev.Handled = true;
    }

    private void OnDoAfterAbductorReturn(Entity<AbductorScientistComponent> ent, ref AbductorReturnDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        _color.RaiseEffect(Color.FromHex("#BA0099"), new List<EntityUid>(1) { ent }, Filter.Pvs(ent, entityManager: EntityManager));
        StopPulls(ent);
        if (ent.Comp.SpawnPosition is {} pos)
            _xform.SetCoordinates(ent, pos);
        OnCameraExit(ent);
    }

    private void OnSendYourself(SendYourselfEvent ev)
    {
        // no sound so you can jump people
        var user = ev.Performer;
        AddTeleportationEffect(user, TeleportationEffectEntity, playAudio: false);
        Spawn(TeleportationEffect, ev.Target);

        var @event = new AbductorSendYourselfDoAfterEvent(GetNetCoordinates(ev.Target));
        var doAfter = new DoAfterArgs(EntityManager, user, TimeSpan.FromSeconds(5), @event, user);
        _doAfter.TryStartDoAfter(doAfter);
        ev.Handled = true;
    }

    private void OnDoAfterSendYourself(Entity<AbductorScientistComponent> ent, ref AbductorSendYourselfDoAfterEvent args)
    {
        _color.RaiseEffect(Color.FromHex("#BA0099"), new List<EntityUid>(1) { ent }, Filter.Pvs(ent, entityManager: EntityManager));
        StopPulls(ent);
        _xform.SetCoordinates(ent, GetCoordinates(args.TargetCoordinates));
        OnCameraExit(ent);
    }

    private void OnExit(ExitConsoleEvent ev) => OnCameraExit(ev.Performer);

    private void AddActions(AbductorBeaconChosenBuiMsg args)
    {
        EnsureComp<AbductorsAbilitiesComponent>(args.Actor, out var comp);
        comp.HiddenActions = _actions.HideActions(args.Actor);
        _actions.AddAction(args.Actor, ref comp.ExitConsole, ExitAction);
        _actions.AddAction(args.Actor, ref comp.SendYourself, SendYourself);
    }

    private void RemoveActions(EntityUid actor)
    {
        if (!TryComp<AbductorsAbilitiesComponent>(actor, out var comp))
            return;

        _actions.RemoveAction(actor, comp.ExitConsole);
        _actions.RemoveAction(actor, comp.SendYourself);
        _actions.UnHideActions(actor, comp.HiddenActions);
    }

    private void StopPulls(EntityUid ent)
    {
        if (_pulling.IsPulling(ent))
        {
            if (!TryComp<PullerComponent>(ent, out var pullerComp)
                || pullerComp.Pulling is not {} pulling
                || !TryComp<PullableComponent>(pulling, out var pullableComp)
                || !_pulling.TryStopPull(pulling, pullableComp)) return;
        }

        if (_pulling.IsPulled(ent))
        {
            if (!TryComp<PullableComponent>(ent, out var pullableComp)
                || !_pulling.TryStopPull(ent, pullableComp)) return;
        }
    }

    private void AddTeleportationEffect(EntityUid target,
        EntProtoId proto,
        bool applyColor = true,
        bool playAudio = true)
    {
        if (applyColor)
            _color.RaiseEffect(Color.FromHex("#BA0099"), new List<EntityUid>(1) { target }, Filter.Pvs(target, entityManager: EntityManager));

        var effect = Spawn(proto, new EntityCoordinates(target, 0, 0));

        if (playAudio)
            _audio.PlayPvs(TeleportSound, effect);
    }
}
