// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Server.BloodCult.Gamerule;
using Content.Trauma.Shared.BloodCult;

namespace Content.Trauma.Server.BloodCult;

public sealed partial class ServerBloodCultSystem : BloodCultSystem
{
    [Dependency] private BloodCultRuleSystem _rule = default!;

    public override void Convert(EntityUid member, EntityUid target)
        => _rule.Convert(member, target);
}
