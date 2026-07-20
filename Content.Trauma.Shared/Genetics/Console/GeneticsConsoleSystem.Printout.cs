// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Genetics.Mutations;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Paper;

namespace Content.Trauma.Shared.Genetics.Console;

public sealed partial class GeneticsConsoleSystem
{
    [Dependency] private PaperSystem _paper = default!;
    [Dependency] private SharedHandsSystem _hands = default!;

    private void InitializePrintout()
    {
        Subs.BuiEvents<GeneticsPrintoutComponent>(GeneticsConsoleUiKey.Key, subs =>
        {
            subs.Event<GeneticsPrintScanMessage>(OnPrintScan);
        });
    }

    private void OnPrintScan(Entity<GeneticsPrintoutComponent> ent, ref GeneticsPrintScanMessage args)
    {
        if (GetWorkableMob(ent.Owner) is not {} mob ||
            !_genome.IsScanned(mob) ||
            !ResetPrintout(ent))
            return;

        var paper = PredictedSpawnAtPosition(ent.Comp.Paper, Transform(ent).Coordinates);
        _transform.SetLocalRotation(paper, 0); // chud engine

        var text = args.Index is {} index
            ? (_genome.GetSequence(mob, index) is {} sequence ? GetSequenceText(sequence) : string.Empty)
            : GetScanText(mob);
        _paper.SetContent(paper, text);

        _hands.TryPickupAnyHand(args.Actor, paper);
    }

    private bool ResetPrintout(Entity<GeneticsPrintoutComponent> ent)
    {
        var now = _timing.CurTime;
        if (now < ent.Comp.NextPrint)
            return false;

        ent.Comp.NextPrint = now + ent.Comp.PrintDelay;
        Dirty(ent);
        return true;
    }

    private string GetScanText(EntityUid mob)
    {
        _sequences.Clear();
        _genome.AddSequenceStates(mob, _sequences);
        _builder.Clear();
        _builder.AppendLine(Loc.GetString("genetics-printout-title"));
        _builder.AppendLine(Loc.GetString("genetics-printout-subject", ("name", Name(mob))));
        _builder.AppendLine(Loc.GetString("genetics-printout-sequences", ("count", _sequences.Count)));
        foreach (var s in _sequences)
        {
            var rarity = s.Rarity.RarityChar();
            _builder.AppendLine(Loc.GetString("genetics-printout-sequence", ("rarity", rarity), ("number", s.Number)));
        }
        return _builder.ToString();
    }

    private string GetSequenceText(Sequence sequence)
    {
        if (_mutation.GetRoundData(sequence.Mutation) is not {} data)
            return string.Empty;

        var rarity = _mutation.GetRarity(sequence.Mutation);
        _builder.Clear();
        _builder.AppendLine(Loc.GetString("genetics-printout-title"));
        _builder.AppendLine(Loc.GetString("genetics-printout-sequence-title", ("number", data.Number)));
        _builder.AppendLine(Loc.GetString("genetics-printout-sequence-rarity", ("rarity", rarity)));
        // format it similar to the UI, 2 rows split on 4 bases per group
        var n = MutationData.PairCount;
        for (int o = 0; o <= n; o += n)
        {
            _builder.Append("| ");
            for (int i = 0; i < n; i += 4)
            {
                var first = o + i;
                var last = first + 4;
                _builder.Append(sequence.Bases[first..last]);
                _builder.Append(' ');
            }
            _builder.AppendLine("|");
        }
        return _builder.ToString();
    }
}
