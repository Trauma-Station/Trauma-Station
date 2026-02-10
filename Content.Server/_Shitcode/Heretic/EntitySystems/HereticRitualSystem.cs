// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Heretic.Components;
using Content.Server.Revolutionary.Components;
using Content.Shared._Shitcode.Heretic.Rituals;

namespace Content.Server._Shitcode.Heretic.EntitySystems;

public sealed class HereticRitualSystem : SharedHereticRitualSystem
{
    private EntityQuery<CommandStaffComponent> _commandQuery;
    private EntityQuery<SecurityStaffComponent> _secQuery;

    public override void Initialize()
    {
        base.Initialize();

        _commandQuery = GetEntityQuery<CommandStaffComponent>();
        _secQuery = GetEntityQuery<SecurityStaffComponent>();
    }

    protected override (bool isCommand, bool isSec) IsCommandOrSec(EntityUid uid)
    {
        return (_commandQuery.HasComp(uid), _secQuery.HasComp(uid));
    }
}
