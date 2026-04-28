// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Dataset;
using Content.Shared.Random.Helpers;
using Content.Shared.Roles;
using Content.Shared.StationRecords;
using Robust.Shared.Random;
using System.Text;

namespace Content.Trauma.Shared.Language;

/// <summary>
/// Codespeak text generation.
/// Based on tg's generate_code_phrase logic.
/// </summary>
public sealed class CodespeakSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public static ProtoId<LocalizedDatasetPrototype> NamesFirstMale = "NamesFirstMale";
    public static ProtoId<LocalizedDatasetPrototype> NamesFirstFemale = "NamesFirstFemale";
    public static ProtoId<LocalizedDatasetPrototype> NamesLast = "NamesLast";

    public static readonly List<ProtoId<DatasetPrototype>> SpecificDatasets = new()
    {
        "IonStormAreas",
        "IonStormAllergies", // drinks is too bizarre, this is a bit better
        "IonStormFoods"
    };

    public static readonly List<ProtoId<DatasetPrototype>> AbstractDatasets = new()
    {
        "IonStormAdjectives",
        "IonStormConcepts",
        "IonStormObjects",
        "IonStormThreats"
    };

    private List<string> _jobs = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        LoadPrototypes();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<JobPrototype>())
            LoadPrototypes();
    }

    private void LoadPrototypes()
    {
        _jobs.Clear();
        foreach (var job in _proto.EnumeratePrototypes<JobPrototype>())
        {
            // no deathsquad or borg?
            if (!job.SetPreference || !job.ApplyTraits)
                continue;

            _jobs.Add(job.LocalizedName.ToLower());
        }
    }

    /// <summary>
    /// Adds a random codespeak phrase to the string builder.
    /// </summary>
    public void GenerateCodePhrase(StringBuilder builder, IRobustRandom rand, List<string> people)
    {
        var picks = new List<CodePick> { CodePick.Person, CodePick.Specific, CodePick.Abstract };

        // non goida weighted random
        var words = rand.NextFloat() switch
        {
            < 50f / 325f => 2, // 50 weight
            < 250f / 325f => 3, // 200 weight
            < 300f / 325f => 4, // 50 weight
            _ => 5, // 25 weight
        };

        var maxWords = words;

        for (; words > 0; words--)
        {
            // for final word...
            if (words == 1)
            {
                // never pick abstract thing if a specific/person hasn't been picked yet
                if (picks.Count == 3)
                {
                    picks.Remove(CodePick.Abstract);
                }
                // otherwise, always pick abstract thing for the last word if only picking 2
                else if (maxWords == 2)
                {
                    picks.Remove(CodePick.Person);
                    picks.Remove(CodePick.Specific);
                }
            }

            var picked = rand.Pick(picks);
            if (picked != CodePick.Abstract)
                picks.Remove(picked); // usually allow repeating abstract things, but not specific/person
            ProtoId<DatasetPrototype>? dataset = null;
            switch (picked)
            {
                case CodePick.Person:
                    if (rand.Prob(0.5f))
                    {
                        // random person name, from the manifest or randomly generated
                        if (rand.Prob(0.7f) && people.Count > 0)
                        {
                            builder.Append(rand.Pick(people));
                        }
                        else
                        {
                            builder.Append(rand.Pick(_proto.Index(rand.Prob(0.5f) ? NamesFirstMale : NamesFirstFemale)));
                            builder.Append(" ");
                            builder.Append(rand.Pick(_proto.Index(NamesLast)));
                        }
                    }
                    else
                    {
                        // random job name
                        builder.Append(rand.Pick(_jobs));
                    }
                    break;
                case CodePick.Specific:
                    dataset = rand.Pick(SpecificDatasets);
                    break;
                case CodePick.Abstract:
                default:
                    dataset = rand.Pick(AbstractDatasets);
                    break;
            }

            if (dataset != null)
                builder.Append(rand.Pick(_proto.Index(dataset)).ToLower());

            builder.Append(words == 1 ? "." : ", ");
        }
        builder.Append(" ");
    }

    /// <summary>
    /// Get the name of every person from every station's manifest.
    /// </summary>
    public List<string> GetAllPeople()
    {
        var people = new List<string>();
        var query = EntityQueryEnumerator<StationRecordsComponent>();
        while (query.MoveNext(out var comp))
        {
            var records = comp.Records;
            foreach (var key in records.Keys)
            {
                if (!records.TryGetRecordEntry<GeneralStationRecord>(key, out var entry))
                    continue;

                // cant wait for this to get abused :)
                people.Add(entry.Name);
            }
        }

        return people;
    }

    private enum CodePick : byte
    {
        Person,
        Specific,
        Abstract
    }
}
