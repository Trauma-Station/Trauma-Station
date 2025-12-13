// SPDX-License-Identifier: AGPL-3.0-or-later
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.Genetics.Console;

/// <summary>
/// Part of genetics console specific to handling unique enzymes.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(GeneticsConsoleSystem))]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class GeneticsConsoleEnzymesComponent : Component
{
    /// <summary>
    /// Sound played when saving the target's enzymes.
    /// </summary>
    [DataField]
    public SoundSpecifier? SaveSound = new SoundPathSpecifier("/Audio/Machines/beep.ogg");

    /// <summary>
    /// Sound played when printing an incubator.
    /// </summary>
    [DataField]
    public SoundSpecifier? PrintSound = new SoundPathSpecifier("/Audio/Machines/printer.ogg");

    /// <summary>
    /// How long to wait between printing incubators.
    /// </summary>
    [DataField]
    public TimeSpan PrintDelay = TimeSpan.FromSeconds(45);

    /// <summary>
    /// When an incubator can next be printed.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField, AutoNetworkedField]
    public TimeSpan NextPrint = TimeSpan.Zero;

    /// <summary>
    /// Incubator entity to spawn.
    /// </summary>
    [DataField]
    public EntProtoId Incubator = "GeneticsEnzymeIncubator";
}

/// <summary>
/// Message to save the scanned mob's enzymes to the current disk.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class GeneticsConsoleSaveEnzymesMessage : BoundUserInterfaceMessage;

/// <summary>
/// Message to print an incubator with the current disk's enzymes.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class GeneticsConsolePrintIncubatorMessage : BoundUserInterfaceMessage;
