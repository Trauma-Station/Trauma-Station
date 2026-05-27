// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DeviceLinking;
using Content.Shared.Tools;
using Robust.Shared.Audio;

namespace Content.Trauma.Shared.Access;

/// <summary>
/// Sends signals based on id cards that enter a set radius of this entity while powered.
/// Can be configured to check against some accesses with a multitool like door electronics can.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(AccessScannerSystem))]
[AutoGenerateComponentState]
public sealed partial class AccessScannerComponent : Component
{
    /// <summary>
    /// Port to set to high while there is at least 1 id scanned.
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> ActivePort = "AccessActive";

    /// <summary>
    /// Port to send a logic string with the name of the latest id that was scanned.
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> NamePort = "AccessName";

    /// <summary>
    /// Port to send a logic string with the job title of the latest id that was scanned.
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> JobPort = "AccessJob";

    /// <summary>
    /// All scanned IDs currently in range that have access.
    /// </summary>
    [DataField]
    public HashSet<EntityUid> Scanned = new();

    /// <summary>
    /// Whether <see cref="ActivePort"/> should be high or low.
    /// </summary>
    [DataField]
    public bool Active;

    /// <summary>
    /// Index of the <see cref="Settings"/> currently being used.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Setting;

    /// <summary>
    /// The possible range and power draw settings.
    /// </summary>
    [DataField(required: true)]
    public List<ScannerSetting> Settings = new();

    /// <summary>
    /// Tool quality needed to cycle <see cref="Settings"/>.
    /// </summary>
    [DataField]
    public ProtoId<ToolQualityPrototype> SettingTool = "Screwing";

    /// <summary>
    /// Sound played when cycling the range setting.
    /// </summary>
    [DataField]
    public SoundSpecifier? CycleSound = new SoundPathSpecifier("/Audio/Machines/lightswitch.ogg");
}

[DataRecord]
public partial record struct ScannerSetting(float Range, float Power);
