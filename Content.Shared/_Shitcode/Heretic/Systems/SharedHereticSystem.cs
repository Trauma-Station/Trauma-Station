using System.Diagnostics.CodeAnalysis;
using Content.Goobstation.Common.CCVar;
using Content.Goobstation.Common.Conversion;
using Content.Goobstation.Common.Heretic;
using Content.Shared._Shitcode.Heretic.Rituals;
using Content.Shared.Actions;
using Content.Shared.Heretic;
using Content.Shared.Heretic.Prototypes;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Objectives.Systems;
using Content.Shared.Store.Components;
using Content.Shared.Tag;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Content.Shared._Shitcode.Heretic.Systems;

public abstract class SharedHereticSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;

    [Dependency] protected readonly ISharedPlayerManager PlayerMan = default!;

    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SharedObjectivesSystem _objectives = default!;

    private EntityQuery<HereticComponent> _hereticQuery;
    private EntityQuery<GhoulComponent> _ghoulQuery;

    private bool _ascensionRequiresObjectives;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MindContainerComponent, BeforeConversionEvent>(OnConversionAttempt);
        SubscribeLocalEvent<HereticComponent, EventHereticAddKnowledge>(OnAddKnowledge);

        Subs.CVar(_cfg, GoobCVars.AscensionRequiresObjectives, value => _ascensionRequiresObjectives = value, true);

        _hereticQuery = GetEntityQuery<HereticComponent>();
        _ghoulQuery = GetEntityQuery<GhoulComponent>();
    }

    private void OnAddKnowledge(Entity<HereticComponent> ent, ref EventHereticAddKnowledge args)
    {
        foreach (var knowledge in args.Knowledge)
        {
            TryAddKnowledge((ent, null, ent.Comp), knowledge);
        }
    }

    private void OnConversionAttempt(Entity<MindContainerComponent> ent, ref BeforeConversionEvent args)
    {
        if (TryGetHereticComponent(ent.AsNullable(), out _, out _))
            args.Blocked = true;
    }

    public bool TryGetHereticComponent(
        Entity<MindContainerComponent?> ent,
        [NotNullWhen(true)] out HereticComponent? heretic,
        out EntityUid mind)
    {
        heretic = null;
        return _mind.TryGetMind(ent, out mind, out _, ent.Comp) && _hereticQuery.TryComp(mind, out heretic);
    }

    public bool IsHereticOrGhoul(EntityUid uid)
    {
        return _ghoulQuery.HasComp(uid) || TryGetHereticComponent(uid, out _, out _);
    }

    public bool TryGetRitual(Entity<HereticComponent?> ent,
        string tag,
        [NotNullWhen(true)] out Entity<HereticRitualComponent>? ritual)
    {
        ritual = null;

        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        foreach (var rit in ent.Comp.Rituals)
        {
            if (!_tag.HasTag(rit, tag) || !TryComp(rit, out HereticRitualComponent? comp))
                continue;

            ritual = (rit, comp);
            return true;
        }

        return false;
    }

    public void RemoveRituals(Entity<HereticComponent?> ent, List<ProtoId<TagPrototype>> tags)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        var toDelete = new List<EntityUid>();
        foreach (var ritual in ent.Comp.Rituals)
        {
            if (_tag.HasAnyTag(ritual, tags))
                toDelete.Add(ritual);
        }

        foreach (var ritual in toDelete)
        {
            if (ent.Comp.ChosenRitual == ritual)
                ent.Comp.ChosenRitual = null;

            ent.Comp.Rituals.Remove(ritual);
            PredictedQueueDel(ritual);
        }

        Dirty(ent);
    }

    public void UpdateKnowledge(EntityUid uid,
        float amount,
        bool showText = true,
        bool playSound = true,
        MindContainerComponent? mindContainer = null)
    {
        if (!_mind.TryGetMind(uid, out var mindId, out var mind, mindContainer) ||
            !TryComp(mindId, out StoreComponent? store) || !TryComp(mindId, out HereticComponent? heretic))
            return;

        UpdateMindKnowledge((mindId, heretic, store, mind), uid, amount, showText, playSound);
    }

    public bool ObjectivesAllowAscension(Entity<HereticComponent?, MindComponent?> ent)
    {
        if (!_ascensionRequiresObjectives)
            return true;

        if (!Resolve(ent, ref ent.Comp1, ref ent.Comp2))
            return false;

        Entity<MindComponent> mindEnt = (ent, ent.Comp2);

        foreach (var objId in ent.Comp1.AllObjectives)
        {
            if (_mind.TryFindObjective(mindEnt.AsNullable(), objId, out var obj) &&
                !_objectives.IsCompleted(obj.Value, mindEnt))
                return false;
        }

        return true;
    }

    public bool TryAddKnowledge(Entity<MindComponent?, HereticComponent?> ent,
        ProtoId<HereticKnowledgePrototype> id,
        EntityUid? body = null)
    {
        if (!Resolve(ent, ref ent.Comp1, ref ent.Comp2, false) || ent.Comp1.UserId is not { } userId)
            return false;

        body ??= ent.Comp1.OwnedEntity;

        var data = _proto.Index(id);

        if (data.Event != null && body != null)
        {
            var ev = _serialization.CreateCopy(data.Event, notNullableOverride: true);
            RaiseKnowledgeEvent(body.Value, ev, false);
            ent.Comp2.KnowledgeEvents.Add(ev);
        }

        if (data.ActionPrototypes is { Count: > 0 })
        {
            foreach (var act in data.ActionPrototypes)
            {
                _actionContainer.AddAction(ent.Owner, act);
            }
        }

        if (data.RitualPrototypes is { Count: > 0 })
            SpawnRituals(ent.Comp2, data.RitualPrototypes, PlayerMan.GetSessionById(userId));

        // set path if out heretic doesn't have it, or if it's different from whatever he has atm
        if (string.IsNullOrWhiteSpace(ent.Comp2.CurrentPath))
        {
            if (!data.SideKnowledge && ent.Comp2.CurrentPath != data.Path)
                ent.Comp2.CurrentPath = data.Path;
        }

        // make sure we only progress when buying current path knowledge
        if (data.Stage > ent.Comp2.PathStage && data.Path == ent.Comp2.CurrentPath)
            ent.Comp2.PathStage = data.Stage;

        Dirty(ent, ent.Comp2);
        return true;
    }

    public virtual void UpdateMindKnowledge(Entity<HereticComponent, StoreComponent, MindComponent> ent,
        EntityUid? user,
        float amount,
        bool showText = true,
        bool playSound = true)
    {
    }

    public virtual void RaiseKnowledgeEvent(EntityUid uid, HereticKnowledgeEvent ev, bool negative) { }

    protected virtual void SpawnRituals(HereticComponent heretic,
        List<EntProtoId<HereticRitualComponent>> rituals,
        ICommonSession session)
    {
    }
}
