// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Polymorph;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using System.Text;

namespace Content.Trauma.Shared.Genetics.Mutations;

public sealed class ScannedGenomeSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MutationSystem _mutation = default!;

    private EntityQuery<MutatableComponent> _mutatableQuery;
    private EntityQuery<ScannedGenomeComponent> _query;

    private StringBuilder _builder = new();

    public override void Initialize()
    {
        base.Initialize();

        _mutatableQuery = GetEntityQuery<MutatableComponent>();
        _query = GetEntityQuery<ScannedGenomeComponent>();

        SubscribeLocalEvent<ScannedGenomeComponent, PolymorphedEvent>(OnPolymorphed);
        SubscribeLocalEvent<ScannedGenomeComponent, MutationRemovedEvent>(OnMutationRemoved);
    }

    private void OnPolymorphed(Entity<ScannedGenomeComponent> ent, ref PolymorphedEvent args)
    {
        var target = args.NewEntity;
        if (ent.Owner != args.OldEntity || !_mutation.IsMutatable(target))
            return;

        var comp = EnsureComp<ScannedGenomeComponent>(target);
        DebugTools.Assert(comp.Sequences.Count == 0, $"Polymorphed {ToPrettyString(ent)} into a non-empty scanned gnome entity {ToPrettyString(target)}, its sequences would be wiped!");
        TransferSequences(ent, (target, comp));
    }

    private void OnMutationRemoved(Entity<ScannedGenomeComponent> ent, ref MutationRemovedEvent args)
    {
        // check just incase you are VERY evil and have a mutation that is a mob or something crazy
        if (ent.Owner != args.Target.Owner)
            return;

        RemoveSequence(ent, args.Id);
    }

    #region Public API

    /// <summary>
    /// Returns true if a mob had its genome scanned.
    /// </summary>
    public bool IsScanned(EntityUid mob) => _query.HasComp(mob);

    /// <summary>
    /// Scans a mob's genome, adding sequences for every dormant and active mutation it has.
    /// </summary>
    public void ScanGenome(EntityUid mob)
    {
        var scanned = EnsureComp<ScannedGenomeComponent>(mob);
        var mutatable = _mutatableQuery.Comp(mob);
        var ent = (mob, scanned);
        foreach (var id in mutatable.Dormant)
        {
            TryAddSequence(ent, id);
        }
    }

    /// <summary>
    /// Gets a sequence at a given index, or null if it doesn't exist.
    /// Does nothing on clients as they have to use BUI state.
    /// </summary>
    public Sequence? GetSequence(EntityUid mob, uint index)
        => _query.TryComp(mob, out var scanned)
            && index < scanned.Sequences.Count
            ? scanned.Sequences[(int) index]
            : null;

    /// <summary>
    /// Adds a randomly generated sequence for a given mutation to the given genome.
    /// </summary>
    public void TryAddSequence(Entity<ScannedGenomeComponent?> ent, EntProtoId<MutationComponent> id)
    {
        if (!_query.Resolve(ent, ref ent.Comp) ||
            !_mutation.AllMutations.TryGetValue(id, out var mutation) ||
            _mutation.GetRoundData(id) is not {} data)
        {
            return;
        }

        // discovered sequences have no missing bases
        if (data.Discovered)
        {
            ent.Comp.Sequences.Add(new Sequence
            {
                Mutation = id,
                Bases = data.Bases
            });
            return;
        }

        _builder.Clear();
        _builder.Append(data.Bases);

        // give difficulty a random offset so its a bit harder to metagame what a mutation could be
        // you can still generally go off more bases missing = better but not automatically know
        // exactly what it is by grepping the mutations :)
        var difficulty = mutation.Difficulty;
        difficulty += _random.Next(-2, 2);
        difficulty = Math.Clamp(difficulty, 0, MutationData.BaseCount);

        // chance of Xing out a whole pair goes up with difficulty
        // so you are less likely to get free easy fixes
        var pairChance = (float) difficulty / MutationData.BaseCount;

        // randomly X out bases depending on mutation difficulty
        while (difficulty > 0)
        {
            var pair = _random.Next(0, MutationData.PairCount);
            var i = pair * 2;
            // cant X out a whole pair if theres only 1 difficulty left
            if (difficulty >= 2 && _random.Prob(pairChance))
            {
                TryX(i);
                TryX(i + 1);
            }
            else if (_random.Prob(0.5f))
            {
                TryX(i);
            }
            else
            {
                TryX(i + 1);
            }
        }
        ent.Comp.Sequences.Add(new Sequence
        {
            Mutation = id,
            Bases = _builder.ToString()
        });

        void TryX(int i)
        {
            if (_builder[i] == 'X')
                return;

            _builder[i] = 'X';
            difficulty--;
        }
    }

    /// <summary>
    /// Adds all client-facing sequence data for a mob to a list, if it is scanned.
    /// </summary>
    public void AddSequenceStates(EntityUid mob, List<SequenceState> sequences)
    {
        if (!_query.TryComp(mob, out var scanned))
            return;

        foreach (var sequence in scanned.Sequences)
        {
            var id = sequence.Mutation;
            if (_mutation.GetRoundData(id) is not {} data)
            {
                Log.Error($"Sequence of {ToPrettyString(mob)} contains unknown mutation {id}!");
                continue;
            }

            sequences.Add(new SequenceState(sequence.Bases, data.Number, data.Discovered ? id.ToString() : null));
        }
    }

    /// <summary>
    /// Removes a sequence of a specific mutation by swap removing with the last mutation.
    /// </summary>
    public void RemoveSequence(Entity<ScannedGenomeComponent> ent, EntProtoId<MutationComponent> id)
    {
        var sequences = ent.Comp.Sequences;
        var count = sequences.Count;
        if (count == 0)
            return;

        // swap remove, great language
        for (var i = 0; i < count; i++)
        {
            var sequence = sequences[i];
            if (sequence.Mutation != id)
                continue;

            var last = count - 1;
            sequences[i] = sequences[last];
            sequences.RemoveAt(last);
            return;
        }
    }

    /// <summary>
    /// Transfer sequences from a source entity to a target entity.
    /// Clears all sequences on the source entity.
    /// Clears any existing sequences the target may have had.
    /// </summary>
    public void TransferSequences(Entity<ScannedGenomeComponent> ent, Entity<ScannedGenomeComponent> target)
    {
        target.Comp.Sequences.Clear();
        foreach (var sequence in ent.Comp.Sequences)
        {
            target.Comp.Sequences.Add(sequence);
        }
        ent.Comp.Sequences.Clear();
    }

    #endregion
}
