using Content.Shared.Hands.EntitySystems;
using Content.Shared.Trigger;

namespace Content.Trauma.Shared.Trigger.Conditions;

public sealed class HoldingTriggerConditionSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HoldingTriggerConditionComponent, AttemptTriggerEvent>(OnAttemptTrigger);
    }

    private void OnAttemptTrigger(Entity<HoldingTriggerConditionComponent> ent, ref AttemptTriggerEvent args)
    {
        if (args.Key is not {} key || !ent.Comp.Keys.Contains(key))
            return;

        args.Cancelled |= (args.User is {} user && _hands.IsHolding(user, ent.Owner)) != ent.Comp.Holding;
    }
}
