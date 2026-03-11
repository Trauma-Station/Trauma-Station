using System.Linq;
using Content.Shared._Shitcode.Heretic.Components;
using Content.Shared.Bed.Sleep;
using Content.Shared.Body.Events;
using Content.Shared.Body.Systems;
using Content.Shared.Rejuvenate;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Network;

namespace Content.Shared._Shitcode.Heretic.Systems;

public sealed class EldritchSleepStatusEffectSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedBloodstreamSystem _bloodstream = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EldritchSleepStatusEffectComponent, StatusEffectAppliedEvent>(OnApply, before: new[] {typeof(SleepingSystem)});
        SubscribeLocalEvent<EldritchSleepStatusEffectComponent, StatusEffectRemovedEvent>(OnRemove);

        SubscribeLocalEvent<MetabolismModifierComponent, GetMetabolicMultiplierEvent>(OnGetMultiplier);
    }

    private void OnGetMultiplier(Entity<MetabolismModifierComponent> ent, ref GetMetabolicMultiplierEvent args)
    {
        args.Multiplier *= ent.Comp.Modifier;
    }

    private void OnRemove(Entity<EldritchSleepStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (_net.IsClient)
            return;

        EntityManager.RemoveComponents(args.Target, ent.Comp.ComponentDifference);
        _bloodstream.FlushChemicals(args.Target, 200);
    }

    private void OnApply(Entity<EldritchSleepStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        // Fuckass status effect system throws exceptions clientside
        if (_net.IsClient)
            return;

        var ev = new RejuvenateEvent(false, false);
        RaiseLocalEvent(args.Target, ev);

        var difference =
            ent.Comp.ComponentsToAdd.ExceptBy(EntityManager.GetComponents(args.Target), x => x.Value.Component)
                .ToDictionary();

        ent.Comp.ComponentDifference = new(difference);
        EntityManager.AddComponents(args.Target, ent.Comp.ComponentsToAdd);
    }
}
