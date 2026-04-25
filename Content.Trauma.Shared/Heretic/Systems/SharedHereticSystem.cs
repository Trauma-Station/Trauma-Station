// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using Content.Goobstation.Common.CCVar;
using Content.Goobstation.Common.Conversion;
using Content.Shared.Actions;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Objectives.Systems;
using Content.Shared.StatusEffectNew;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Content.Shared.Tag;
using Content.Trauma.Shared.Heretic.Components;
using Content.Trauma.Shared.Heretic.Components.Ghoul;
using Content.Trauma.Shared.Heretic.Components.StatusEffects;
using Content.Trauma.Shared.Heretic.Events;
using Content.Trauma.Shared.Heretic.Prototypes;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager;

namespace Content.Trauma.Shared.Heretic.Systems;

public abstract class SharedHereticSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;
    [Dependency] private readonly INetManager _net = default!;

    [Dependency] protected readonly ISharedPlayerManager PlayerMan = default!;
    [Dependency] protected readonly StatusEffectsSystem Status = default!;
    [Dependency] protected readonly SharedContainerSystem Container = default!;

    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SharedObjectivesSystem _objectives = default!;

    [Dependency] private readonly EntityQuery<HereticComponent> _hereticQuery = default!;
    [Dependency] private readonly EntityQuery<GhoulComponent> _ghoulQuery = default!;

    private bool _ascensionRequiresObjectives;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MindContainerComponent, BeforeConversionEvent>(OnConversionAttempt);
        SubscribeLocalEvent<HereticComponent, EventHereticAddKnowledge>(OnAddKnowledge);
        SubscribeLocalEvent<HereticComponent, ComponentInit>(OnInit);

        Subs.CVar(_cfg, GoobCVars.AscensionRequiresObjectives, value => _ascensionRequiresObjectives = value, true);
    }

    private void OnInit(Entity<HereticComponent> ent, ref ComponentInit args)
    {
        ent.Comp.RitualContainer = Container.EnsureContainer<Container>(ent, "rituals");
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
        [NotNullWhen(true)] out Entity<Rituals.HereticRitualComponent>? ritual)
    {
        ritual = null;

        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        foreach (var rit in ent.Comp.RitualContainer.ContainedEntities)
        {
            if (!_tag.HasTag(rit, tag) || !TryComp(rit, out Rituals.HereticRitualComponent? comp))
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
        foreach (var ritual in ent.Comp.RitualContainer.ContainedEntities)
        {
            if (_tag.HasAnyTag(ritual, tags))
                toDelete.Add(ritual);
        }

        foreach (var ritual in toDelete)
        {
            if (ent.Comp.ChosenRitual == ritual)
                ent.Comp.ChosenRitual = null;

            Container.Remove(ritual, ent.Comp.RitualContainer);
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

    public bool ObjectivesAllowAscension(Entity<HereticComponent> ent)
    {
        return !_ascensionRequiresObjectives || ent.Comp.ObjectivesCompleted;
    }

    public bool TryAddKnowledge(Entity<MindComponent?, HereticComponent?> ent,
        ProtoId<HereticKnowledgePrototype> id,
        EntityUid? body = null)
    {
        if (!Resolve(ent, ref ent.Comp1, ref ent.Comp2, false) || ent.Comp1.UserId is not { } userId)
            return false;

        body ??= ent.Comp1.OwnedEntity;

        var data = _proto.Index(id);

        if (data.MindEvent is { } hereticEv)
        {
            RaiseLocalEvent(ent.Owner, hereticEv);
        }

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
        if (ent.Comp2.CurrentPath == null)
        {
            if (!data.SideKnowledge && ent.Comp2.CurrentPath != data.Path)
                ent.Comp2.CurrentPath = data.Path;
        }

        // make sure we only progress when buying current path knowledge
        if (data.Stage > ent.Comp2.PathStage && data.Path == ent.Comp2.CurrentPath)
        {
            ent.Comp2.PathStage = data.Stage;
            UpdateHereticCostModifiers((ent, ent.Comp2));
        }

        Dirty(ent, ent.Comp2);
        return true;
    }

    public void UpdateHereticAura(EntityUid uid)
    {
        if (!TryGetHereticComponent(uid, out var heretic, out _) || !heretic.ShouldShowAura ||
            HasComp<HideHereticAuraComponent>(uid) ||
            Status.HasEffectComp<HideHereticAuraStatusEffectComponent>(uid))
        {
            RemCompDeferred<HereticAuraComponent>(uid);
            return;
        }

        EnsureComp<HereticAuraComponent>(uid);
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
        List<EntProtoId<Rituals.HereticRitualComponent>> rituals,
        ICommonSession session)
    {
    }

    public virtual void UpdateHereticCostModifiers(Entity<HereticComponent?> ent,
        ProtoId<StoreCategoryPrototype>? category = null,
        ListingDataWithCostModifiers? except = null)
    {
    }

    public void UpdateObjectiveProgress(Entity<HereticComponent, MindComponent> ent)
    {
        // Client throws exceptions when trying to call objective system public api
        // That's why this thing exists in the first place
        if (_net.IsClient)
            return;

        Entity<MindComponent> mindEntity = (ent, ent.Comp2);

        var result = true;

        foreach (var objId in ent.Comp1.AllObjectives)
        {
            if (!_mind.TryFindObjective(mindEntity.AsNullable(), objId, out var objective) ||
                _objectives.IsCompleted(objective.Value, mindEntity))
                continue;

            result = false;
            break;
        }

        if (ent.Comp1.ObjectivesCompleted == result)
            return;

        ent.Comp1.ObjectivesCompleted = result;
        Dirty(ent.Owner, ent.Comp1);
    }
}
