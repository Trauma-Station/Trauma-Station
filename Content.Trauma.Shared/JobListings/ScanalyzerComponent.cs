// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Objectives;

namespace Content.Trauma.Shared.JobListings;

/// <summary>
/// Component on a tool given to progtot traitors to scan grand theft items.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ScanalyzerComponent : Component
{
    /// <summary>
    /// The grand theft item that this scanalyzer is attuned for.
    /// </summary>
    [DataField]
    public ProtoId<StealTargetGroupPrototype> StealTarget;

    /// <summary>
    /// If this scanalyzer has already been used.
    /// In theory you wouldn't need this and would just check the mind if the <see cref="StealTarget"/> has been scanned.
    /// But that is sever-side only so we need this field on the client to predict things.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Used = false;

    /// <summary>
    /// How long it takes to scan.
    /// </summary>
    [DataField]
    public TimeSpan ScanDuration;
}
