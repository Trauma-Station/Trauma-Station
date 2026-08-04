// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Server.RandomMetadata;
using Content.Shared.Objectives.Components;
using Robust.Shared.Random;

namespace Content.Trauma.Server.Spy;

public sealed partial class RandomObjectiveSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private RandomMetadataSystem _randomMeta = default!;
    [Dependency] private MetaDataSystem _metaData = default!;

    [Dependency] private EntityQuery<RandomObjectiveComponent> _objectiveQuery = default!;

    [SubscribeLocalEvent]
    private void OnAfterAssign(Entity<RandomObjectiveComponent> ent, ref ObjectiveAfterAssignEvent args)
    {
        var values = ProtoMan.Index(ent.Comp.NameFormats).Values;
        var copy = values.ToList();

        // Look through existing random objectives and try to pick name format that hasn't been picked already
        foreach (var obj in args.Mind.Objectives)
        {
            if (!_objectiveQuery.TryComp(obj, out var comp))
                continue;

            copy.Remove(comp.PickedFormat);
        }

        ent.Comp.PickedFormat = copy.Count > 0 ? _random.Pick(copy) : _random.Pick(values);
        var result = _randomMeta.GetRandomFromSegments(ent.Comp.NameSegments, ent.Comp.PickedFormat);
        _metaData.SetEntityName(ent, result, args.Meta);
    }
}
