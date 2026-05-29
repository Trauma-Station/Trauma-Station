// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Radio.EntitySystems;
using Content.Server.Research.Systems;
using Content.Shared.Radio;
using Content.Shared.Research.Components;
using Content.Trauma.Shared.Genetics.Console;
using Content.Trauma.Shared.Genetics.Mutations;

namespace Content.Trauma.Server.Genetics.Console;

public sealed partial class GeneticsResearchConsoleSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private MutationSystem _mutation = default!;
    [Dependency] private RadioSystem _radio = default!;
    [Dependency] private ResearchSystem _research = default!;
    [Dependency] private EntityQuery<ResearchClientComponent> _clientQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GeneticsResearchConsoleComponent, MutationSequencedEvent>(OnSequenced);
    }

    private void OnSequenced(Entity<GeneticsResearchConsoleComponent> ent, ref MutationSequencedEvent args)
    {
        if (args.Data.Discovered || // no infinite point farming chud
            _clientQuery.CompOrNull(ent)?.Server is not {} server)
            return;

        var difficulty = _mutation.AllMutations[args.Mutation].Difficulty;
        var points = difficulty * ent.Comp.PointsPerDifficulty;
        _research.ModifyServerPoints(server, points);
        var name = _proto.Index(args.Mutation).Name;
        var msg = Loc.GetString("genetics-console-radio-message", ("points", points), ("mutation", name));
        _radio.SendRadioMessage(ent, msg, ent.Comp.Channel, ent);
    }
}
