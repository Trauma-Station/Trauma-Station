// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Blob;
using Content.Goobstation.Shared.Blob.Components;
using Content.Shared.EntityEffects;

namespace Content.Goobstation.Shared.EntityEffects;

/// <summary>
/// Grows a blob tile to a random adjacent tile.
/// </summary>
public sealed partial class GrowBlob : EntityEffectBase<GrowBlob>
{
    /// <summary>
    /// Whether to attack blocking entities.
    /// </summary>
    [DataField]
    public bool Attack;

    /// <summary>
    /// Whether to do the chem's GrowthEffects.
    /// </summary>
    [DataField]
    public bool DoEffects;
}

public sealed partial class GrowBlobEffectSystem : EntityEffectSystem<BlobTileComponent, GrowBlob>
{
    [Dependency] private BlobTileSystem _tile = default!;

    protected override void Effect(Entity<BlobTileComponent> ent, ref EntityEffectEvent<GrowBlob> args)
    {
        var e = args.Effect;
        _tile.TryGrow(ent, e.Attack, e.DoEffects);
    }
}
