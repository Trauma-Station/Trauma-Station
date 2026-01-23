using System.Diagnostics.CodeAnalysis;
using Content.Goobstation.Common.Heretic;
using Content.Shared._Shitcode.Heretic.Components;
using Content.Shared.Heretic;
using Content.Shared.Mind;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared._Shitcode.Heretic.Systems;

public abstract class SharedHereticSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    private EntityQuery<HereticComponent> _hereticQuery;
    private EntityQuery<GhoulComponent> _ghoulQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HereticCheckEvent>(OnCheck);

        _hereticQuery = GetEntityQuery<HereticComponent>();
        _ghoulQuery = GetEntityQuery<GhoulComponent>();
    }

    private void OnCheck(ref HereticCheckEvent ev)
    {
        ev.Result = TryGetHereticComponent(ev.Uid, out _, out _);
    }

    public bool TryGetHereticComponent(
        EntityUid uid,
        [NotNullWhen(true)] out HereticComponent? heretic,
        out EntityUid mind)
    {
        heretic = null;
        return _mind.TryGetMind(uid, out mind, out _) && _hereticQuery.TryComp(mind, out heretic);
    }

    public bool IsHereticOrGhoul(EntityUid uid)
    {
        return _ghoulQuery.HasComp(uid) || TryGetHereticComponent(uid, out _, out _);
    }

    public bool TryGetRitual(Entity<HereticComponent> ent,
        string tag,
        [NotNullWhen(true)] out Entity<HereticRitualComponent>? ritual)
    {
        foreach (var rit in ent.Comp.Rituals)
        {
            if (!_tag.HasTag(rit, tag) || !TryComp(rit, out HereticRitualComponent? comp))
                continue;

            ritual = (rit, comp);
            return true;
        }

        ritual = null;
        return false;
    }

    public void RemoveRituals(Entity<HereticComponent> ent, List<ProtoId<TagPrototype>> tags)
    {
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
            QueueDel(ritual);
        }

        Dirty(ent);
    }
}
