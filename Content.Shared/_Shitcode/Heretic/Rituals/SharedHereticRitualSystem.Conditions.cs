using System.Linq;
using System.Text;
using Content.Shared._Shitcode.Heretic.Components;
using Content.Shared.Heretic;
using Content.Shared.Stacks;

namespace Content.Shared._Shitcode.Heretic.Rituals;

public abstract partial class SharedHereticRitualSystem
{
    public void SubscribeConditions()
    {
        SubscribeLocalEvent<TransformComponent, HereticRitualConditionEvent<IsTargetCondition>>(OnTargetCheck);
        SubscribeLocalEvent<TransformComponent, HereticRitualConditionEvent<ConditionsRitualCondition>>(
            OnApplyConditions);
        SubscribeLocalEvent<TransformComponent, HereticRitualConditionEvent<IsLimitedOutputCondition>>(
            OnLimitedCondition);
        SubscribeLocalEvent<HereticRitualComponent, HereticRitualConditionEvent<ProcessIngredientsCondition>>(
            OnProcessIngredients);
        SubscribeLocalEvent<HereticComponent, HereticRitualConditionEvent<CanAscendCondition>>(OnCanAscend);
        SubscribeLocalEvent<HereticComponent, HereticRitualConditionEvent<ObjectivesCompleteCondition>>(
            OnObjectivesComplete);
        SubscribeLocalEvent<HereticKnowledgeRitualComponent, HereticRitualConditionEvent<FilterKnowledgeTagsCondition>>(
            OnKnowledge);
        SubscribeLocalEvent<HereticRitualComponent, HereticRitualConditionEvent<TryApplyEffectSequenceCondition>>(
            OnApplySequence);
    }

    private void OnLimitedCondition(Entity<TransformComponent> ent,
        ref HereticRitualConditionEvent<IsLimitedOutputCondition> args)
    {
        args.Result = args.Ritual.Comp.LimitedOutput.Contains(ent.Owner);
    }

    private void OnApplyConditions(Entity<TransformComponent> ent,
        ref HereticRitualConditionEvent<ConditionsRitualCondition> args)
    {
        args.Result = args.Condition.RequireAll
            ? _effects.TryConditions(ent, args.Condition.Conditions, args.Ritual)
            : _effects.AnyCondition(ent, args.Condition.Conditions, args.Ritual);
    }

    private void OnApplySequence(Entity<HereticRitualComponent> ent,
        ref HereticRitualConditionEvent<TryApplyEffectSequenceCondition> args)
    {
        TryGetValue(ent, Performer, out EntityUid? user);

        args.Result = _effects.TryEffects(ent,
            ent.Comp.Effects.Skip(args.Condition.From).Take(args.Condition.To - args.Condition.From),
            ent,
            user);
    }

    private void OnObjectivesComplete(Entity<HereticComponent> ent,
        ref HereticRitualConditionEvent<ObjectivesCompleteCondition> args)
    {
        args.Result = _heretic.ObjectivesAllowAscension((ent, ent, null));
    }

    private void OnCanAscend(Entity<HereticComponent> ent, ref HereticRitualConditionEvent<CanAscendCondition> args)
    {
        args.Result = ent.Comp.CanAscend;
    }

    private void OnProcessIngredients(Entity<HereticRitualComponent> ent,
        ref HereticRitualConditionEvent<ProcessIngredientsCondition> args)
    {
        if (args.Condition.ApplyOn == string.Empty)
            return;

        var missingList = new Dictionary<LocId, int>();
        var toDelete = new HashSet<EntityUid>();
        var toSplit = new Dictionary<Entity<StackComponent>, int>();

        var ingredientAmounts = Enumerable.Repeat(0, args.Condition.Ingredients.Length).ToList();

        foreach (var look in ent.Comp.Raiser.GetTargets<EntityUid>(args.Condition.ApplyOn))
        {
            for (var i = 0; i < args.Condition.Ingredients.Length; i++)
            {
                var ritIng = args.Condition.Ingredients[i];
                var compAmount = ingredientAmounts[i];

                if (compAmount >= ritIng.Amount)
                    continue;

                if (_whitelist.IsWhitelistFail(ritIng.Whitelist, look))
                    continue;

                var stack = _stackQuery.CompOrNull(look);
                var amount = stack == null ? 1 : Math.Min(stack.Count, ritIng.Amount - compAmount);

                ingredientAmounts[i] += amount;

                if (stack == null || stack.Count <= amount)
                    toDelete.Add(look);
                else
                    toSplit.Add((look, stack), amount);
            }
        }

        for (var i = 0; i < args.Condition.Ingredients.Length; i++)
        {
            var ritIng = args.Condition.Ingredients[i];
            var difference = ritIng.Amount - ingredientAmounts[i];
            if (difference > 0)
                missingList.Add(ritIng.Name, difference);
        }

        if (missingList.Count == 0)
        {
            args.Result = true;
            ent.Comp.Blackboard[args.Condition.DeleteEntitiesKey] = toDelete;
            ent.Comp.Blackboard[args.Condition.SplitEntitiesKey] = toSplit;
            return;
        }

        var sb = new StringBuilder();
        foreach (var (name, amount) in missingList)
        {
            sb.Append($"{Loc.GetString(name)} x{amount} ");
        }

        sb.Remove(sb.Length - 1, 1);

        var str = Loc.GetString("heretic-ritual-fail-items", ("itemlist", sb.ToString()));
        CancelCondition(ent, ref args, str);
    }

    private void OnTargetCheck(Entity<TransformComponent> ent, ref HereticRitualConditionEvent<IsTargetCondition> args)
    {
        if (!TryGetValue(args.Ritual, Mind, out EntityUid mind) || !TryComp(mind, out HereticComponent? heretic))
        {
            CancelCondition(args.Ritual, ref args);
            return;
        }

        args.Result = IsSacrificeTarget((mind, heretic), ent);
    }

    private void OnKnowledge(Entity<HereticKnowledgeRitualComponent> ent,
        ref HereticRitualConditionEvent<FilterKnowledgeTagsCondition> args)
    {
        if (args.Condition.ApplyOn == string.Empty)
            return;

        var output = new HashSet<EntityUid>();
        var missingTags = ent.Comp.KnowledgeRequiredTags.ToHashSet();
        foreach (var uid in args.Ritual.Comp.Raiser.GetTargets<EntityUid>(args.Condition.ApplyOn))
        {
            if (!_tagQuery.TryComp(uid, out var tags))
                continue;

            missingTags.RemoveWhere(tag =>
            {
                if (!_tag.HasTag(tags, tag))
                    return false;

                output.Add(uid);
                return true;
            });
        }

        if (missingTags.Count > 0)
        {
            var missing = string.Join(", ", missingTags);
            var cancelString = Loc.GetString("heretic-ritual-fail-items", ("itemlist", missing));
            CancelCondition(args.Ritual, ref args, cancelString);
            return;
        }

        args.Ritual.Comp.Blackboard[args.Condition.Result] = output;
        args.Result = true;
    }
}
