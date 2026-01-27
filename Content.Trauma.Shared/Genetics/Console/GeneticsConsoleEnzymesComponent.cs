// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
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
    /// Subjects with more than this number of genetic damage can't have their unique enzymes changed.
    /// </summary>
    [DataField]
    public FixedPoint2 MaxGeneticDamage = 10; // it's a very delicate process yeah

    /// <summary>
    /// Sound played when saving the target's enzymes to disk.
    /// </summary>
    [DataField]
    public SoundSpecifier? SaveSound = new SoundPathSpecifier("/Audio/Machines/beep.ogg");

    /// <summary>
    /// Sound played when applying enzymes to the target.
    /// </summary>
    [DataField]
    public SoundSpecifier? ApplySound;

    /// <summary>
    /// How long to wait between changing enzymes for someone.
    /// </summary>
    [DataField]
    public TimeSpan ApplyDelay = TimeSpan.FromSeconds(60);

    /// <summary>
    /// How long the apply enzymes doafter takes.
    /// </summary>
    [DataField]
    public TimeSpan ApplyDuration = TimeSpan.FromSeconds(15);

    /// <summary>
    /// When enzymes can next be applied to someone.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField, AutoNetworkedField]
    public TimeSpan NextApply = TimeSpan.Zero;
}

/// <summary>
/// Message to save the scanned mob's enzymes to the current disk.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class GeneticsConsoleSaveEnzymesMessage : BoundUserInterfaceMessage;

/// <summary>
/// Message to apply the current disk's enzymes onto the scanned mob.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class GeneticsConsoleApplyEnzymesMessage : BoundUserInterfaceMessage;
