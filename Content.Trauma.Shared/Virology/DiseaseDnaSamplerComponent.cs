// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Goobstation.Shared.Virology;
using Content.Trauma.Shared.Disease;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Trauma.Shared.Virology;

/// <summary>
/// Tool for programming a list of DNAs onto a <c>DnaTargetDiseaseComponent</c> disease.
/// Use it on e.g. a swab to sample the DNA on it, then you can create a disease pen for it.
/// Reset after creating a disease pen.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(DiseaseDnaSamplerSystem))]
[AutoGenerateComponentState]
public sealed partial class DiseaseDnaSamplerComponent : Component
{
    /// <summary>
    /// Disease to create and assign dna to.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId<DnaTargetDiseaseComponent>? Disease;

    /// <summary>
    /// Disease pen to spawn.
    /// </summary>
    [DataField]
    public EntProtoId<DiseasePenComponent> Injector = "LiveInjector";

    /// <summary>
    /// Currently sampled dna.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<string> TargetDnas = new();

    /// <summary>
    /// How long it takes to sample DNA from an object.
    /// </summary>
    [DataField]
    public TimeSpan SampleDelay = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Multiplier on how long it takes to sample DNA from an awake mob.
    /// </summary>
    [DataField]
    public float AwakeModifier = 2f;

    /// <summary>
    /// Sound played after sampling DNA from something.
    /// </summary>
    [DataField]
    public SoundSpecifier? SampleSound;

    /// <summary>
    /// Sound played after creating the disease.
    /// </summary>
    [DataField]
    public SoundSpecifier? CreateSound;
}

[Serializable, NetSerializable]
public enum DiseaseDnaSamplerUiKey : byte
{
    Key
}

/// <summary>
/// Message sent by the client to create the disease pen.
/// </summary>
[Serializable, NetSerializable]
public sealed class DiseaseDnaSamplerCreateMessage : BoundUserInterfaceMessage;
