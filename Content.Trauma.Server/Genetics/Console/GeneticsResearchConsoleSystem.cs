// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Server.Radio.EntitySystems;
using Content.Server.Research.Systems;
using Content.Shared.Radio;
using Content.Shared.Research.Components;
using Content.Trauma.Shared.Genetics.Console;
using Content.Trauma.Shared.Genetics.Mutations;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Server.Genetics.Console;

public sealed class GeneticsResearchConsoleSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly MutationSystem _mutation = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly ResearchSystem _research = default!;

    private EntityQuery<ResearchClientComponent> _clientQuery;

    public override void Initialize()
    {
        base.Initialize();

        _clientQuery = GetEntityQuery<ResearchClientComponent>();

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
