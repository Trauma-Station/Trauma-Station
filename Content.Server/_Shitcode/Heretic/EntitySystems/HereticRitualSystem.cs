// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Heretic.Components;
using Content.Server.Polymorph.Systems;
using Content.Server.Revolutionary.Components;
using Content.Shared._Shitcode.Heretic.Rituals;

namespace Content.Server._Shitcode.Heretic.EntitySystems;

public sealed class HereticRitualSystem : SharedHereticRitualSystem
{
    [Dependency] private readonly PolymorphSystem _polymorph = default!;

    private EntityQuery<CommandStaffComponent> _commandQuery;
    private EntityQuery<SecurityStaffComponent> _secQuery;

    public override void Initialize()
    {
        base.Initialize();

        _commandQuery = GetEntityQuery<CommandStaffComponent>();
        _secQuery = GetEntityQuery<SecurityStaffComponent>();

        SubscribeLocalEvent<HereticRitualComponent, HereticRitualEffectEvent<PolymorphRitualEffect>>(OnPolymorph);
    }

    private void OnPolymorph(Entity<HereticRitualComponent> ent, ref HereticRitualEffectEvent<PolymorphRitualEffect> args)
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
            ent.Comp.Blackboard[args.Effect.Result] = result;
    }

    protected override (bool isCommand, bool isSec) IsCommandOrSec(EntityUid uid)
    {
        return (_commandQuery.HasComp(uid), _secQuery.HasComp(uid));
    }
}
