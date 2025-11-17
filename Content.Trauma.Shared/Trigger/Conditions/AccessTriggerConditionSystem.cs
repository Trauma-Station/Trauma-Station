using Content.Shared.Access.Systems;
using Content.Shared.Trigger;

namespace Content.Trauma.Shared.Trigger.Conditions;

public sealed class AccessTriggerConditionSystem : EntitySystem
{
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AccessTriggerConditionComponent, AttemptTriggerEvent>(OnAttemptTrigger);
    }

    private void OnAttemptTrigger(Entity<AccessTriggerConditionComponent> ent, ref AttemptTriggerEvent args)
    {
        if (args.Key is {} key && !ent.Comp.Keys.Contains(key))
            return;

        args.Cancelled |= args.User is not {} user || !_accessReader.IsAllowed(user, ent.Owner);
    }
}
