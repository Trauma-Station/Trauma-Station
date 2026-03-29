// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Common.CosmicCult.Components;

/// <summary>
/// A component for the cosmic cult's rifts
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class CosmicRiftComponent : Component
{
    [DataField]
    public EntProtoId PurgeVFX = "CleanseEffectVFX";

    [DataField]
    public SoundSpecifier PurgeSound = new SoundPathSpecifier("/Audio/_Trauma/CosmicCult/cleanse_deconversion.ogg");

    [DataField]
    public SoundSpecifier UseSound = new SoundPathSpecifier("/Audio/_Trauma/CosmicCult/ability_shift_in.ogg"); // Yes, the sounds are the other way around. This is intentional. I feel like it.

    /// <summary>
    /// How long does it take for a non-chaplain to purge this rift with a bible. If null, non-chaplains cannot purge this.
    /// </summary>
    [DataField]
    public TimeSpan? BibleTime;

    /// <summary>
    /// How long does it take for a chaplain to purge this rift. If null, <see cref="BibleTime"/> is used instead. If it is also null, the rift cannot be purged.
    /// </summary>
    [DataField]
    public TimeSpan? ChaplainTime;

    /// <summary>
    /// How long does it take for a cosmic cultist to upgrade this rift.
    /// </summary>
    [DataField]
    public TimeSpan UpgradeTime = TimeSpan.FromSeconds(15);

    /// <summary>
    /// How long does it take for a cosmic cultist to travel through this rift.
    /// </summary>
    [DataField]
    public TimeSpan TravelTime = TimeSpan.FromSeconds(10);

    /// <summary>
    /// How long does it take for a cultist to close this rift. If null, the rift cannot be closed by a cultist.
    /// </summary>
    [DataField]
    public TimeSpan? CloseTime;

    /// <summary>
    /// Entity prototype to spawn when a cultist tries to upgrade the rift. If null, the rift cannot be upgraded.
    /// </summary>
    [DataField]
    public EntProtoId? UpgradeProto;

    /// <summary>
    /// The level of influences this rift unlocks for it's creator.
    /// </summary>
    [DataField]
    public int InfluenceLevel;

    /// <summary>
    /// How much entropy can this rift hold at once.
    /// </summary>
    [DataField]
    public int EntropyCap = 10;

    /// <summary>
    /// How much entropy this rift currently holds.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int EntropyStored;

    /// <summary>
    /// How often this rift generates entropy.
    /// </summary>
    [DataField]
    public TimeSpan EntropyTime = TimeSpan.FromSeconds(60);

    [AutoPausedField, DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan EntropyTimer = default!;

    /// <summary>
    /// The cultist who created this rift.
    /// </summary>
    [DataField]
    public Entity<CosmicCultistComponent>? Creator;

    /// <summary>
    /// List of entities that have used this rift to travel to the cosmic void. If the rift is destroyed, those entities will be forced out of it.
    /// </summary>
    [DataField]
    public List<EntityUid> TravelingEntities = [];
}
