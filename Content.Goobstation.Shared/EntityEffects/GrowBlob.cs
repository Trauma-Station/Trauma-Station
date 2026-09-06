// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Blob;
using Content.Goobstation.Shared.Blob.Components;
using Content.Shared.Damage.Systems;
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
    [Dependency] private DamageableSystem _damage = default!;

    protected override void Effect(Entity<BlobTileComponent> ent, ref EntityEffectEvent<GrowBlob> args)
    {
        var e = args.Effect;
        _tile.TryGrow(ent, out var tile, e.Attack, e.DoEffects, predicted: args.Predicted);
        if (tile != null)
            _damage.ChangeDamage(tile.Value, _damage.GetAllDamage(ent.Owner), ignoreResistances: true);
    }
}
