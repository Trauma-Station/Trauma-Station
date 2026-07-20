// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;

namespace Content.Goobstation.Shared.Blob.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), Serializable]
public sealed partial class BlobTileComponent : Component
{
    [DataField, AutoNetworkedField]
    public Color Color = Color.White;

    [DataField, AutoNetworkedField]
    public EntityUid? Core;

    [DataField(required: true)]
    public ProtoId<BlobTilePrototype> Tile;

    /// <summary>
    /// Tile that can spread when pulsed by a node blob.
    /// </summary>
    [DataField]
    public ProtoId<BlobTilePrototype> SpreadTile = "Normal";

    [DataField]
    public DamageSpecifier HealthOfPulse = new()
    {
        DamageDict = new()
        {
            { "Blunt", -4 },
            { "Slash", -4 },
            { "Piercing", -4 },
            { "Ballistic", -4 },
            { "Heat", -4 },
            { "Cold", -4 },
            { "Shock", -4 },
        }
    };

    [DataField]
    public DamageSpecifier FlashDamage = new()
    {
        DamageDict = new()
        {
            { "Heat", 24 },
        }
    };
}
