using System.Linq;
using Content.Shared._Shitcode.Heretic.Components;
using Content.Shared.Heretic;
using Content.Shared.Mind;
using Content.Shared.Stacks;
using Content.Shared.Store.Components;

namespace Content.Shared._Shitcode.Heretic.Rituals;

public abstract partial class SharedHereticRitualSystem
{
    private void SubscribeEffects()
    {
        SubscribeLocalEvent<TransformComponent, HereticRitualEffectEvent<LookupRitualEffect>>(OnLookup);
        SubscribeLocalEvent<TransformComponent, HereticRitualEffectEvent<SacrificeEffect>>(OnSacrifice);
        SubscribeLocalEvent<TransformComponent, HereticRitualEffectEvent<SpawnRitualEffect>>(OnSpawn);
        SubscribeLocalEvent<TransformComponent, HereticRitualEffectEvent<PathBasedSpawnEffect>>(OnPathSpawn);
        SubscribeLocalEvent<TransformComponent, HereticRitualEffectEvent<GhoulifyEffect>>(OnGhoulify);
        SubscribeLocalEvent<TransformComponent, HereticRitualEffectEvent<TeleportToRuneEffect>>(OnTeleport);
        SubscribeLocalEvent<TransformComponent, HereticRitualEffectEvent<FindLostLimitedOutputEffect>>(OnFindLimited);
        SubscribeLocalEvent<TransformComponent, HereticRitualEffectEvent<OpenRuneBuiEffect>>(OnBui);
        SubscribeLocalEvent<TransformComponent, HereticRitualEffectEvent<EffectsRitualEffect>>(OnEffects);
        SubscribeLocalEvent<HereticComponent, HereticRitualEffectEvent<AddKnowledgeEffect>>(OnAddKnowledge);
        SubscribeLocalEvent<HereticComponent, HereticRitualEffectEvent<UpdateKnowledgeEffect>>(OnUpdateKnowledge);
        SubscribeLocalEvent<HereticComponent, HereticRitualEffectEvent<RemoveRitualsEffect>>(OnRemoveRituals);
        SubscribeLocalEvent<HereticRitualComponent, HereticRitualEffectEvent<SplitIngredientsRitualEffect>>(OnSplit);
    }

    private void OnEffects(Entity<TransformComponent> ent, ref HereticRitualEffectEvent<EffectsRitualEffect> args)
    {
        if (!TryGetValue(args.Ritual, Performer, out EntityUid performer))
            return;

        _effects.ApplyEffects(ent, args.Effect.Effects, args.Ritual, performer);
    }

    private void OnSplit(Entity<HereticRitualComponent> ent,
        ref HereticRitualEffectEvent<SplitIngredientsRitualEffect> args)
    {
        if (args.Effect.ApplyOn == string.Empty)
            return;

        foreach (var (stackEnt, amount) in
                 args.Ritual.Comp.Raiser.GetTargets<KeyValuePair<Entity<StackComponent>, int>>(args.Effect.ApplyOn))
        {
            _stack.SetCount(stackEnt.AsNullable(), stackEnt.Comp.Count - amount);
        }
    }

    private void OnBui(Entity<TransformComponent> ent, ref HereticRitualEffectEvent<OpenRuneBuiEffect> args)
    {
        if (!TryGetValue(args.Ritual, Platform, out EntityUid platform))
            return;

        _uiSystem.OpenUi(platform, args.Effect.Key, ent);
    }

    private void OnTeleport(Entity<TransformComponent> ent, ref HereticRitualEffectEvent<TeleportToRuneEffect> args)
    {
        if (!TryGetValue(args.Ritual, Platform, out EntityUid platform))
            return;

        var coords = _transform.GetMapCoordinates(platform);
        _transform.SetMapCoordinates(ent, coords);
    }

    private void OnRemoveRituals(Entity<HereticComponent> ent,
        ref HereticRitualEffectEvent<RemoveRitualsEffect> args)
    {
        _heretic.RemoveRituals(ent.AsNullable(), args.Effect.RitualTags);
    }

    private void OnUpdateKnowledge(Entity<HereticComponent> ent,
        ref HereticRitualEffectEvent<UpdateKnowledgeEffect> args)
    {
        if (!TryComp(ent, out MindComponent? mind) ||
            !TryComp(ent, out StoreComponent? store))
            return;

        _heretic.UpdateMindKnowledge((ent, ent, store, mind), null, args.Effect.Amount);
    }

    private void OnGhoulify(Entity<TransformComponent> ent, ref HereticRitualEffectEvent<GhoulifyEffect> args)
    {
        if (!TryGetValue(args.Ritual, Performer, out EntityUid performer))
            return;

        var minion = Factory.GetComponent<HereticMinionComponent>();
        minion.BoundHeretic = performer;
        AddComp(ent, minion, true);

        var ghoul = Factory.GetComponent<GhoulComponent>();
        ghoul.TotalHealth = args.Effect.Health;
        ghoul.GiveBlade = args.Effect.GiveBlade;
        AddComp(ent, ghoul, true);
    }

    private void OnFindLimited(Entity<TransformComponent> ent,
        ref HereticRitualEffectEvent<FindLostLimitedOutputEffect> args)
    {
        if (args.Ritual.Comp.LimitedOutput.Count == 0)
            return;

        var coords = _transform.GetMapCoordinates(ent);
        EntityUid? selected = null;
        var maxDist = args.Effect.MinRange;

        foreach (var output in args.Ritual.Comp.LimitedOutput)
        {
            var outCoords = _transform.GetMapCoordinates(output);
            if (outCoords.MapId != coords.MapId)
            {
                selected = output;
                break;
            }

            var dist = (coords.Position - outCoords.Position).Length();

            if (dist < args.Effect.MinRange)
                continue;

            if (dist < maxDist)
                continue;

            maxDist = dist;
            selected = output;
        }

        if (selected is not { } uid)
            return;

        args.Ritual.Comp.Blackboard[args.Effect.Result] = uid;
    }

    private void OnAddKnowledge(Entity<HereticComponent> ent, ref HereticRitualEffectEvent<AddKnowledgeEffect> args)
    {
        _heretic.TryAddKnowledge((ent, null, ent), args.Effect.Knowledge);
    }

    private void OnPathSpawn(Entity<TransformComponent> ent, ref HereticRitualEffectEvent<PathBasedSpawnEffect> args)
    {
        if (!TryGetValue(args.Ritual, Mind, out EntityUid mind) || !TryComp(mind, out HereticComponent? heretic))
            return;

        var coords = ent.Comp.Coordinates;

        EntityUid spawned;
        if (heretic.CurrentPath is { } path && args.Effect.Output.TryGetValue(path, out var toSpawn))
            spawned = PredictedSpawnAtPosition(toSpawn, coords);
        else
            spawned = PredictedSpawnAtPosition(args.Effect.FallbackOutput, coords);

        if (args.Ritual.Comp.Limit <= 0)
            return;

        args.Ritual.Comp.LimitedOutput.Add(spawned);
    }

    private void OnSpawn(Entity<TransformComponent> ent, ref HereticRitualEffectEvent<SpawnRitualEffect> args)
    {
        if (!TryGetValue(args.Ritual, Performer, out EntityUid performer))
            return;

        var coords = Transform(ent).Coordinates;
        foreach (var (obj, amount) in args.Effect.Output)
        {
            for (var i = 0; i < amount; i++)
            {
                var spawned = PredictedSpawnAtPosition(obj, coords);

                if (_ghoulQuery.HasComp(spawned))
                {
                    var ev = new SetGhoulBoundHereticEvent(performer);
                    RaiseLocalEvent(spawned, ref ev);
                }

                if (args.Ritual.Comp.Limit <= 0)
                    continue;

                args.Ritual.Comp.LimitedOutput.Add(spawned);
                if (args.Ritual.Comp.LimitedOutput.Count >= args.Ritual.Comp.Limit)
                    break;
            }
        }
    }


    private void OnSacrifice(Entity<TransformComponent> ent, ref HereticRitualEffectEvent<SacrificeEffect> args)
    {
        if (!TryGetValue(args.Ritual, Mind, out EntityUid mind) ||
            !TryComp(mind, out MindComponent? mindComp) || !TryComp(mind, out StoreComponent? store) ||
            !TryComp(mind, out HereticComponent? heretic))
            return;

        var knowledgeGain = 0f;
        var (isCommand, isSec) = IsCommandOrSec(ent);
        var isHeretic = _heretic.TryGetHereticComponent(ent.Owner, out _, out _);
        knowledgeGain += isHeretic || IsSacrificeTarget((mind, heretic), ent)
            ? isCommand || isSec || isHeretic ? 3f : 2f
            : 0f;

        _gibbing.Gib(ent);

        var ev = new IncrementHereticObjectiveProgressEvent(args.Effect.SacrificeObjective);
        RaiseLocalEvent(mind, ref ev);

        if (!isCommand)
        {
            var ev2 = new IncrementHereticObjectiveProgressEvent(args.Effect.SacrificeHeadObjective);
            RaiseLocalEvent(mind, ref ev2);
        }

        if (knowledgeGain > 0)
            _heretic.UpdateMindKnowledge((mind, heretic, store, mindComp), null, knowledgeGain);
    }

    private void OnLookup(Entity<TransformComponent> ent, ref HereticRitualEffectEvent<LookupRitualEffect> args)
    {
        var look = _lookup.GetEntitiesInRange(ent, args.Effect.Range, args.Effect.Flags);
        args.Ritual.Comp.Blackboard[args.Effect.Result] = look;
    }

}
