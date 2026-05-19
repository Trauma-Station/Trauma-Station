// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.Input;
using Content.Medical.Common.Targeting;
using Content.Medical.Shared.Body;
using Content.Medical.Shared.Targeting;
using Robust.Client.Player;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;

namespace Content.Medical.Client.Targeting;

public sealed partial class TargetingSystem : SharedTargetingSystem
{
    [Dependency] private IPlayerManager _player = default!;

    public event Action<TargetingComponent>? TargetingStartup;
    public event Action? TargetingShutdown;
    public event Action<TargetBodyPart>? TargetChange;
    public event Action<BodyStatusComponent>? PartStatusStartup;
    public event Action<BodyStatusComponent>? PartStatusUpdate;
    public event Action? PartStatusShutdown;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TargetingComponent, LocalPlayerAttachedEvent>(OnTargetingAttached);
        SubscribeLocalEvent<TargetingComponent, LocalPlayerDetachedEvent>(OnTargetingDetached);
        SubscribeLocalEvent<TargetingComponent, ComponentStartup>(OnTargetingStartup);
        SubscribeLocalEvent<TargetingComponent, ComponentShutdown>(OnTargetingShutdown);

        SubscribeLocalEvent<BodyStatusComponent, LocalPlayerAttachedEvent>(OnStatusAttached);
        SubscribeLocalEvent<BodyStatusComponent, LocalPlayerDetachedEvent>(OnStatusDetached);
        SubscribeLocalEvent<BodyStatusComponent, ComponentStartup>(OnStatusStartup);
        SubscribeLocalEvent<BodyStatusComponent, ComponentShutdown>(OnStatusShutdown);

        SubscribeAllEvent<TargetIntegrityChangedMessage>(OnTargetIntegrityChanged);

        // TODO SHITMED: change this to scrolling "height" and symmetry
        CommandBinds.Builder
        .Bind(TraumaKeyFunctions.TargetHead,
            InputCmdHandler.FromDelegate((session) => HandleTargetChange(session, TargetBodyPart.Head)))
        .Bind(TraumaKeyFunctions.TargetChest,
            InputCmdHandler.FromDelegate((session) => HandleTargetChange(session, TargetBodyPart.Chest)))
        .Bind(TraumaKeyFunctions.TargetGroin,
            InputCmdHandler.FromDelegate((session) => HandleTargetChange(session, TargetBodyPart.Groin)))
        .Bind(TraumaKeyFunctions.TargetLeftArm,
            InputCmdHandler.FromDelegate((session) => HandleTargetChange(session, TargetBodyPart.LeftArm)))
        .Bind(TraumaKeyFunctions.TargetLeftHand,
            InputCmdHandler.FromDelegate((session) => HandleTargetChange(session, TargetBodyPart.LeftHand)))
        .Bind(TraumaKeyFunctions.TargetRightArm,
            InputCmdHandler.FromDelegate((session) => HandleTargetChange(session, TargetBodyPart.RightArm)))
        .Bind(TraumaKeyFunctions.TargetRightHand,
            InputCmdHandler.FromDelegate((session) => HandleTargetChange(session, TargetBodyPart.RightHand)))
        .Bind(TraumaKeyFunctions.TargetLeftLeg,
            InputCmdHandler.FromDelegate((session) => HandleTargetChange(session, TargetBodyPart.LeftLeg)))
        .Bind(TraumaKeyFunctions.TargetLeftFoot,
            InputCmdHandler.FromDelegate((session) => HandleTargetChange(session, TargetBodyPart.LeftFoot)))
        .Bind(TraumaKeyFunctions.TargetRightLeg,
            InputCmdHandler.FromDelegate((session) => HandleTargetChange(session, TargetBodyPart.RightLeg)))
        .Bind(TraumaKeyFunctions.TargetRightFoot,
            InputCmdHandler.FromDelegate((session) => HandleTargetChange(session, TargetBodyPart.RightFoot)))
        .Register<SharedTargetingSystem>();
    }

    private void OnTargetingAttached(Entity<TargetingComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        TargetingStartup?.Invoke(ent.Comp);
    }

    private void OnStatusAttached(Entity<BodyStatusComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        PartStatusStartup?.Invoke(ent.Comp);
    }

    private void OnTargetingDetached(Entity<TargetingComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        TargetingShutdown?.Invoke();
    }

    private void OnStatusDetached(Entity<BodyStatusComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        PartStatusShutdown?.Invoke();
    }

    private void OnTargetingStartup(Entity<TargetingComponent> ent, ref ComponentStartup args)
    {
        if (_player.LocalEntity == ent.Owner)
            TargetingStartup?.Invoke(ent.Comp);
    }

    private void OnTargetingShutdown(Entity<TargetingComponent> ent, ref ComponentShutdown args)
    {
        if (_player.LocalEntity == ent.Owner)
            TargetingShutdown?.Invoke();
    }

    private void OnStatusStartup(Entity<BodyStatusComponent> ent, ref ComponentStartup args)
    {
        if (_player.LocalEntity == ent.Owner)
            PartStatusStartup?.Invoke(ent.Comp);
    }

    private void OnStatusShutdown(Entity<BodyStatusComponent> ent, ref ComponentShutdown args)
    {
        if (_player.LocalEntity == ent.Owner)
            PartStatusShutdown?.Invoke();
    }

    private void OnTargetIntegrityChanged(TargetIntegrityChangedMessage args)
    {
        if (_player.LocalEntity is not {} uid
            || !TryComp<BodyStatusComponent>(uid, out var comp))
            return;

        PartStatusUpdate?.Invoke(comp);
    }

    private void HandleTargetChange(ICommonSession? session, TargetBodyPart target)
    {
        if (session == null
            || session.AttachedEntity is not { } uid
            || !TryComp<TargetingComponent>(uid, out var targeting))
            return;

        TargetChange?.Invoke(target);
    }
}
