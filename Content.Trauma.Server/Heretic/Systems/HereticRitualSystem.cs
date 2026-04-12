// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Polymorph.Systems;
using Content.Server.Revolutionary.Components;
using Content.Trauma.Server.Heretic.Components;
using Content.Trauma.Shared.Heretic.Components.Side;
using Content.Trauma.Shared.Heretic.Rituals;
using Robust.Shared.Random;

namespace Content.Trauma.Server.Heretic.Systems;

public sealed class HereticRitualSystem : SharedHereticRitualSystem
{
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _rand = default!;

    [Dependency] private readonly EntityQuery<CommandStaffComponent> _commandQuery = default!;
    [Dependency] private readonly EntityQuery<SecurityStaffComponent> _secQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HereticRitualComponent, HereticRitualEffectEvent<PolymorphRitualEffect>>(OnPolymorph);

        SubscribeLocalEvent<HereticKnowledgeRitualComponent, ComponentStartup>(OnKnowledgeStartup);
    }

    private void OnPolymorph(Entity<HereticRitualComponent> ent,
        ref HereticRitualEffectEvent<PolymorphRitualEffect> args)
    {
        if (args.Effect.ApplyOn == string.Empty)
            return;

        HashSet<EntityUid> result = new();
        foreach (var uid in args.Ritual.Comp.Raiser.GetTargets<EntityUid>(args.Effect.ApplyOn))
        {
            if (_polymorph.PolymorphEntity(uid, args.Effect.Polymorph) is { } newUid)
                result.Add(newUid);
        }

        if (result.Count > 0)
            args.Ritual.Comp.Blackboard[args.Effect.Result] = result;
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
