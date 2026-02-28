using Content.Shared.Actions;
using Content.Shared.Trigger;
using Content.Shared.Trigger.Systems;
using Content.Trauma.Shared.Genetics.Mutations;

namespace Content.Trauma.Shared.Trigger.Effects;

public sealed class MutationActionOnTriggerSystem : XOnTriggerSystem<MutationActionOnTriggerComponent>
{
    [Dependency] private readonly ActionMutationSystem _actionMutation = default!;
    [Dependency] private readonly MutationSystem _mutation = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    protected override void OnTrigger(Entity<MutationActionOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        if (_mutation.GetMutationTarget(ent.Owner) is not {} user || _actionMutation.GetAction(ent.Owner) is not {} action)
            return;

        _actions.PerformAction(user, action);
        args.Handled = true;
    }
}
