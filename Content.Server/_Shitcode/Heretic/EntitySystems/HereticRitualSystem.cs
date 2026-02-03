// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Heretic.Components;
using Content.Server.Revolutionary.Components;
using Content.Shared._Shitcode.Heretic.Components;
using Content.Shared._Shitcode.Heretic.Rituals;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Shitcode.Heretic.EntitySystems;

public sealed class HereticRitualSystem : SharedHereticRitualSystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _rand = default!;

    private EntityQuery<CommandStaffComponent> _commandQuery;
    private EntityQuery<SecurityStaffComponent> _secQuery;

    public override void Initialize()
    {
        base.Initialize();

        _commandQuery = GetEntityQuery<CommandStaffComponent>();
        _secQuery = GetEntityQuery<SecurityStaffComponent>();

        SubscribeLocalEvent<HereticKnowledgeRitualComponent, ComponentStartup>(OnKnowledgeStartup);
    }

    protected override (bool isCommand, bool isSec) IsCommandOrSec(EntityUid uid)
    {
        return (_commandQuery.HasComp(uid), _secQuery.HasComp(uid));
    }

    private void OnKnowledgeStartup(Entity<HereticKnowledgeRitualComponent> ent, ref ComponentStartup args)
    {
        var dataset = _proto.Index(ent.Comp.KnowledgeDataset);
        for (var i = 0; i < ent.Comp.TagAmount; i++)
        {
            ent.Comp.KnowledgeRequiredTags.Add(_rand.Pick(dataset.Values));
        }

        Dirty(ent);
    }
}
