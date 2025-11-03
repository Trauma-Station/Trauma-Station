using Content.Shared.Inventory;
using Content.Shared.StepTrigger.Components;
using Content.Shared.StepTrigger.Systems;

namespace Content.Trauma.Shared.StepTrigger;

/// <summary>
/// Makes goob shitcode <see cref="StepTriggerImmuneComponent"/> work on clothing via inventory relay.
/// Relies on enchanting relaying StepTriggerAttemptEvent, if that's ever removed this will silently break!
/// </summary>
public sealed class StepTriggerRelaySystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StepTriggerImmuneComponent, InventoryRelayedEvent<StepTriggerAttemptEvent>>(OnStepTriggerAttempt);
    }

    private void OnStepTriggerAttempt(Entity<StepTriggerImmuneComponent> ent, ref InventoryRelayedEvent<StepTriggerAttemptEvent> args)
    {
        if (!TryComp<StepTriggerComponent>(args.Args.Source, out var comp))
            return;

        if (comp.TriggerGroups?.IsValid(ent.Comp) == true)
            args.Args.Cancelled = true;
    }
}
