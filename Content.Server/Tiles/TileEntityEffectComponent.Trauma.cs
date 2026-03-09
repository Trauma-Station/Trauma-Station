using Content.Shared.EntityConditions;

namespace Content.Server.Tiles;

public sealed partial class TileEntityEffectComponent
{
    [DataField]
    public EntityCondition[]? Conditions;
}
