// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Server.Mind;
using Content.Server.Roles.Jobs;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Roles;
using Content.Trauma.Common.Grudge;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.Grudges;

public sealed partial class GrudgeObjectiveSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly JobSystem _job = default!;

    private List<EntityUid> _players = new();
    private TimeSpan _lastTime = TimeSpan.Zero;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerAdded);
    }

    private void OnPlayerAdded(PlayerSpawnCompleteEvent ev)
    {
        AddPlayer(ev.Mob);
    }

    public void AddPlayer(EntityUid player)
    {
        if (!HasComp<HumanoidProfileComponent>(player))
            return;

        if (!_mind.TryGetMind(player, out var mind, out var mindComp))
            return;

        if (!_job.MindTryGetJobId(mind, out var job) || job is not { })
            return;

        _players.Add(player);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime <= _lastTime)
            return;

        _lastTime = _timing.CurTime + TimeSpan.FromSeconds(1);

        if (_players.Count < 2)
            return;

        var participants = _players.ToList();
        var randomParticipants = participants.Shuffle();

        for (int i = 0; i < participants.Count; i++)
        {
            var current = participants[i];

            var next = participants[(i + 1) % participants.Count];

            if (AssignGrudge(current, next))
                Log.Info($"Paired {Name(current)} and {Name(next)} for mutual grudges.");
        }

        _players.Clear();
    }

    public bool AssignGrudge(EntityUid playerA, EntityUid playerB)
    {
        if (!_mind.TryGetMind(playerA, out var mindA, out var mindCompA) || !_mind.TryGetMind(playerB, out var mindB, out var mindCompB))
            return false;

        if (SelectGrudges(playerA, playerB) is not { } proto)
            return false;

        var (proto1, proto2) = proto;

        _mind.TryAddObjective(mindA, mindCompA, proto1);
        _mind.TryAddObjective(mindB, mindCompB, proto2);

        if (!_mind.TryFindObjective(mindA, proto1, out var grudge1) || !_mind.TryFindObjective(mindB, proto2, out var grudge2))
            return false;

        var addedEv = new GrudgeAddedEvent(playerA, playerB, grudge1.Value, grudge2.Value);
        RaiseLocalEvent(grudge1.Value, ref addedEv);
        RaiseLocalEvent(grudge2.Value, ref addedEv);

        var raisedEv = new GrudgeUpdateEvent();
        RaiseLocalEvent(grudge1.Value, ref raisedEv);
        RaiseLocalEvent(grudge2.Value, ref raisedEv);

        return true;
    }

    public (EntProtoId, EntProtoId)? SelectGrudges(EntityUid playerA, EntityUid playerB)
    {
        var allGrudges = _proto.EnumeratePrototypes<GrudgePrototype>().ToList();
        var randomGrudges = allGrudges.Shuffle();

        if (!TryComp<HumanoidProfileComponent>(playerA, out var humanoidA) || !TryComp<HumanoidProfileComponent>(playerB, out var humanoidB))
            return null;

        if (!_mind.TryGetMind(playerA, out var mindA, out var mindCompA) || !_mind.TryGetMind(playerB, out var mindB, out var mindCompB))
            return null;

        if (!_job.MindTryGetJobId(mindA, out var jobAA) || jobAA is not { } jobA || !_job.MindTryGetJobId(mindB, out var jobBB) || jobBB is not { } jobB)
            return null;

        var speciesA = humanoidA.Species;
        var speciesB = humanoidB.Species;

        foreach (var grudge in randomGrudges)
        {
            if (!IsSpeciesValid(speciesA, grudge.AllowedAccuserSpecies, grudge.InvertAccuserSpecies))
                continue;

            if (!IsSpeciesValid(speciesB, grudge.AllowedAccusedSpecies, grudge.InvertAccusedSpecies))
                continue;

            if (!IsJobValid(jobA, grudge.AllowedAccuserJob, grudge.InvertAccuserJob))
                continue;

            if (!IsJobValid(jobB, grudge.AllowedAccusedJob, grudge.InvertAccusedJob))
                continue;

            if (_mind.TryFindObjective(mindA, grudge.AccuserObjective, out _) || _mind.TryFindObjective(mindB, grudge.AccusedObjective, out _))
                continue;

            return (grudge.AccuserObjective, grudge.AccusedObjective);
        }

        return null;
    }

    private bool IsSpeciesValid(string species, List<ProtoId<SpeciesPrototype>>? allowed, bool inverted)
    {
        if (allowed == null || allowed.Count == 0)
            return !inverted;

        var contains = allowed.Contains(species);

        return inverted ? !contains : contains;
    }

    private bool IsJobValid(string species, List<ProtoId<JobPrototype>>? allowed, bool inverted)
    {
        if (allowed == null || allowed.Count == 0)
            return !inverted;

        var contains = allowed.Contains(species);

        return inverted ? !contains : contains;
    }
}
