// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Trauma.Shared.Morph;

/// <summary>
/// Component for the morph antag's mob.
/// Controls disguising and using morph's actions.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class MorphComponent : Component
{
    /// <summary>
    /// How much biomass it has.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 Biomass = 20;

    /// <summary>
    /// How much biomass it costs to replicate.
    /// </summary>
    [DataField]
    public FixedPoint2 ReplicateCost = 150;

    /// <summary>
    /// How much biomass one Morph can store
    /// </summary>
    [DataField]
    public FixedPoint2 MaxBiomass = 1000; // doesnt exist in ss13 but just incase

    /// <summary>
    /// How long it takes to replicate.
    /// </summary>
    [DataField]
    public TimeSpan ReplicationDelay = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Amount of morphs this specific morph has produced by replication.
    /// </summary>
    [DataField]
    public int Children = 0;

    /// <summary>
    /// What mob to spawn on replicate, could be used for some sort of sac to spawn morphs on, just uses morph prototype for now
    /// </summary>
    [DataField]
    public EntProtoId MorphPrototype = "MobMorph";

    /// <summary>
    /// How much damage will revert your disguise in a single attack.
    /// </summary>
    [DataField]
    public FixedPoint2 DamageThreshold = FixedPoint2.New(2);

    /// <summary>
    /// The alert to show biomass with
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype> BiomassAlert = "Biomass";

    /// <summary>
    /// Actions to grant the player.
    /// </summary>
    [DataField(required: true)]
    public List<EntProtoId> Actions = default!;

    /// <summary>
    /// Sound played when replicating.
    /// </summary>
    [DataField]
    public SoundSpecifier ReplicateSound = new SoundPathSpecifier("/Audio/_Trauma/Morph/mutate.ogg");

    /// <summary>
    /// If non-null, whitelist for valid entities that provide biomass when eaten.
    /// </summary>
    [DataField]
    public EntityWhitelist? BiomassWhitelist;

    /// <summary>
    /// If non-null, blacklist for entities that will never give biomass when eaten.
    /// </summary>
    [DataField]
    public EntityWhitelist? BiomassBlacklist;

    /// <summary>
    /// The gamerule that created this morph.
    /// </summary>
    [DataField(serverOnly: true)]
    public EntityUid? Rule;
}

/// <summary>
/// Controls Digits for morph biomass alert
// TODO: unused?
/// </summary>
[Serializable, NetSerializable]
public enum MorphVisualLayers : byte
{
    Digit1,
    Digit2,
    Digit3
}

[ByRefEvent]
public sealed partial class MorphReplicateActionEvent : InstantActionEvent;

[ByRefEvent]
public sealed partial class MorphActionEvent : EntityTargetActionEvent;

[ByRefEvent]
public sealed partial class UnMorphActionEvent : InstantActionEvent;

[Serializable, NetSerializable]
public sealed partial class ReplicateDoAfterEvent : SimpleDoAfterEvent;
