// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;
using Content.Shared.Maps;
using Robust.Shared.Audio;

namespace Content.Trauma.Shared.BloodCult.Pylon;

/// <summary>
/// Slowly heals cultists and spreads cult tiles when active.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(PylonSystem))]
public sealed partial class PylonComponent : Component
{
    [DataField]
    public float HealingAuraRange = 5;

    [DataField]
    public float CorruptionRadius = 5;

    /// <summary>
    /// How long to wait between corrupting random tiles.
    /// </summary>
    [DataField]
    public TimeSpan CorruptCooldown = TimeSpan.FromSeconds(5);

    /// <summary>
    /// How long to wait between healing cultists.
    /// </summary>
    [DataField]
    public TimeSpan HealCooldown = TimeSpan.FromSeconds(20);

    /// <summary>
    /// Tile to randomly convert everything nearby into.
    /// </summary>
    [DataField]
    public ProtoId<ContentTileDefinition> CultTile = "CultFloor";

    /// <summary>
    /// Clientside effect spawned when corrupting a tile.
    /// </summary>
    [DataField]
    public EntProtoId TileCorruptEffect = "CultTileSpawnEffect";

    [DataField]
    public SoundSpecifier? BurnHandSound = new SoundPathSpecifier("/Audio/Effects/lightburn.ogg");

    [DataField]
    public SoundSpecifier? CorruptTileSound = new SoundPathSpecifier("/Audio/_Trauma/BloodCult/curse.ogg");

    [DataField]
    public DamageSpecifier? Healing;

    [DataField]
    public DamageSpecifier? DamageOnInteract;
}
