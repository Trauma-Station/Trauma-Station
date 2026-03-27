// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Mind;
using Content.Shared.Stacks;
using Content.Shared.Store.Components;
using Content.Shared.Timing;
using Content.Trauma.Shared.Heretic.Components;
using Content.Trauma.Shared.Heretic.Components.Ghoul;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Rust;
using Content.Trauma.Shared.Heretic.Events;

namespace Content.Trauma.Shared.Heretic.Rituals;

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
        SubscribeLocalEvent<HereticComponent, HereticRitualEffectEvent<UpdateKnowledgeEffect>>(OnUpdateKnowledge);
        SubscribeLocalEvent<HereticComponent, HereticRitualEffectEvent<RemoveRitualsEffect>>(OnRemoveRituals);
        SubscribeLocalEvent<HereticRitualComponent, HereticRitualEffectEvent<SplitIngredientsRitualEffect>>(OnSplit);
        SubscribeLocalEvent<HereticRitualComponent, HereticRitualEffectEvent<IfElseRitualEffect>>(OnIfElse);

        SubscribeLocalEvent<TransformComponent, HereticRitualEffectEvent<NestedRitualEffect>>(OnNested);
        SubscribeLocalEvent<TransformComponent, HereticRitualEffectEvent<SpawnCosmicField>>(OnCosmicField);
        SubscribeLocalEvent<TransformComponent, HereticRitualEffectEvent<SetBlackboardValuesRitualEffect>>(OnBlackboard);
        SubscribeLocalEvent<RustGraspComponent, HereticRitualEffectEvent<ResetRustGraspDelayEffect>>(OnRustDelay);
    }

    private void OnBlackboard(Entity<TransformComponent> ent, ref HereticRitualEffectEvent<SetBlackboardValuesRitualEffect> args)
    {
        foreach (var (key, value) in args.Effect.Values)
        {
            args.Ritual.Comp.Blackboard[key] = value;
        }
    }

    private void OnRustDelay(Entity<RustGraspComponent> ent, ref HereticRitualEffectEvent<ResetRustGraspDelayEffect> args)
    {
        if (!TryComp(ent, out UseDelayComponent? delay))
            return;

        if (!TryGetValue(args.Ritual, Mind, out EntityUid mind) || !TryComp(mind, out HereticComponent? heretic))
            return;

        _grasp.ResetRustGraspDelay((ent, ent.Comp, delay), heretic.PathStage, args.Effect.Multiplier);
    }

    private void OnCosmicField(Entity<TransformComponent> ent, ref HereticRitualEffectEvent<SpawnCosmicField> args)
    {
        if (!TryGetValue(args.Ritual, Mind, out EntityUid mind) || !TryComp(mind, out HereticComponent? heretic))
            return;

        _starMark.SpawnCosmicField(ent.Comp.Coordinates, heretic.PathStage, args.Effect.Lifetime);
    }

    private void OnNested(Entity<TransformComponent> ent, ref HereticRitualEffectEvent<NestedRitualEffect> args)
    {
        _effects.TryApplyEffect(ent, args.Effect.Proto, args.Ritual, args.User);
    }

    private void OnIfElse(Entity<HereticRitualComponent> ent, ref HereticRitualEffectEvent<IfElseRitualEffect> args)
    {
        bool result;
        if (_effects.TryConditions(ent, args.Effect.IfConditions, args.Ritual))
            result = _effects.TryEffects(ent, args.Effect.EffectsA, args.Ritual, args.User);
        else if (args.Effect.EffectsB != null)
            result = _effects.TryEffects(ent, args.Effect.EffectsB, args.Ritual, args.User);
        else
            return;

        if (args.Effect.SaveResultKey is { } key)
            args.Ritual.Comp.Blackboard[key] = result;
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
        if (!TryGetValue(args.Ritual, Performer, out EntityUid performer) ||
            !TryGetValue(args.Ritual, Mind, out EntityUid mind))
            return;

        var ghoul = Factory.GetComponent<GhoulComponent>();
        ghoul.TotalHealth = args.Effect.Health;
        ghoul.GiveBlade = args.Effect.GiveBlade;
        ghoul.ChangeHumanoidProfile = args.Effect.ChangeAppearance;
        ghoul.CanDeconvert = args.Effect.CanDeconvert;
        ghoul.DeathBehavior = args.Effect.DeathBehavior;
        AddComp(ent, ghoul, true);

        var ev = new SetGhoulBoundHereticEvent(performer, mind, args.Ritual);
        RaiseLocalEvent(ent, ref ev);
    }

    private void OnFindLimited(Entity<TransformComponent> ent,
        ref HereticRitualEffectEvent<FindLostLimitedOutputEffect> args)
    {
        var ritual = Comp<HereticRitualComponent>(args.Ritual);
        if (ritual.LimitedOutput.Count == 0)
            return;

        var coords = _transform.GetMapCoordinates(ent);
        EntityUid? selected = null;
        var maxDist = args.Effect.MinRange;

        foreach (var output in ritual.LimitedOutput)
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

        if (!TryComp(args.Ritual, out HereticRitualComponent? ritual) || ritual.Limit <= 0)
            return;

        ritual.LimitedOutput.Add(spawned);
    }

    private void OnSpawn(Entity<TransformComponent> ent, ref HereticRitualEffectEvent<SpawnRitualEffect> args)
    {
        if (!TryGetValue(args.Ritual, Performer, out EntityUid performer) ||
            !TryGetValue(args.Ritual, Mind, out EntityUid mind) || !TryComp(mind, out HereticComponent? heretic))
            return;

        var ritual = CompOrNull<HereticRitualComponent>(args.Ritual);

        var coords = Transform(ent).Coordinates;
        foreach (var (obj, amount) in args.Effect.Output)
        {
            for (var i = 0; i < amount; i++)
            {
                var spawned = PredictedSpawnAtPosition(obj, coords);

                if (_ghoulQuery.HasComp(spawned) || _tag.HasTag(spawned, args.Effect.ForceMinionTag))
                {
                    heretic.Minions.Add(spawned);
                    var ev = new SetGhoulBoundHereticEvent(performer, mind, args.Ritual);
                    RaiseLocalEvent(spawned, ref ev);
                }

                if (ritual is not { Limit: > 0 })
                    continue;

                ritual.LimitedOutput.Add(spawned);
                if (ritual.LimitedOutput.Count >= ritual.Limit)
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

        if (isCommand)
        {
            var ev2 = new IncrementHereticObjectiveProgressEvent(args.Effect.SacrificeHeadObjective);
            RaiseLocalEvent(mind, ref ev2);
        }

        if (knowledgeGain == 0)
            return;

        _heretic.UpdateMindKnowledge((mind, heretic, store, mindComp), null, knowledgeGain);

        heretic.SacrificeTracker++;
        if (!heretic.InfluenceSpawnPerSacrificeAmount.TryGetValue(heretic.SacrificeTracker, out var influence))
            return;

        var influenceEv = new SpawnHereticInfluenceEvent(influence);
        RaiseLocalEvent(ref influenceEv);
    }

    private void OnLookup(Entity<TransformComponent> ent, ref HereticRitualEffectEvent<LookupRitualEffect> args)
    {
        var look = _lookup.GetEntitiesInRange(ent, args.Effect.Range, args.Effect.Flags);
        args.Ritual.Comp.Blackboard[args.Effect.Result] = look;
    }
}
