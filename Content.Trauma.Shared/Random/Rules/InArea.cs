// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Random.Rules;
using Content.Trauma.Shared.Areas;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Random.Rules;

/// <summary>
/// Returns true if the attached entity is inside an area
/// </summary>
public sealed partial class InAreaRule : RulesRule
{
    [DataField(required: true)]
    public HashSet<EntProtoId> Areas;

    private AreaSystem? _area;

    public override bool Check(EntityManager entManager, EntityUid uid)
    {
        _area ??= entManager.System<AreaSystem>();

        return (_area.GetArea(uid) is { } area
               && _area.GetAreaPrototype(area) is {} areaProto
               && Areas.Contains(areaProto))
               != Inverted;
    }
}
