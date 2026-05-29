// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;

namespace Content.Trauma.Shared.Abductor;

/// <summary>
/// Task tablet which provides the UI used to scan subjects then see and complete their tasks.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(AbductorTaskTabletSystem))]
[AutoGenerateComponentState]
public sealed partial class AbductorTaskTabletComponent : Component
{
    /// <summary>
    /// The mob currently linked.
    /// </summary>
    [DataField, AutoNetworkedField]
    public NetEntity? Target;

    /// <summary>
    /// Range you can scan and complete experiments within.
    /// </summary>
    [DataField]
    public float Range = 10f;

    /// <summary>
    /// Sound played when scanning a new target.
    /// </summary>
    [DataField]
    public SoundSpecifier? ScanSound = new SoundPathSpecifier("/Audio/Machines/high_tech_confirm.ogg");

    /// <summary>
    /// Sound played when all tasks for a subject are completed.
    /// </summary>
    [DataField]
    public SoundSpecifier? FinishSound = new SoundPathSpecifier("/Audio/_Shitmed/Misc/abducted.ogg");
}

[Serializable, NetSerializable]
public enum AbductorTaskTabletUIKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class AbductorTaskScanMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class AbductorTaskCompleteMessage : BoundUserInterfaceMessage;
