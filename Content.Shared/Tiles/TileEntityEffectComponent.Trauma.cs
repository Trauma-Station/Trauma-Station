// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityConditions;

namespace Content.Shared.Tiles;

public sealed partial class TileEntityEffectComponent
{
    [DataField]
    public EntityCondition[]? Conditions;
}
