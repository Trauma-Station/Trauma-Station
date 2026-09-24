// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.BloodCult.Components;

/// <summary>
/// Lets a cultist transmute this item into a cult one with the Twisted Construction action.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class TwistedConstructionTargetComponent : Component
{
    [DataField(required: true)]
    public EntProtoId ReplacementProto;

    [DataField]
    public TimeSpan DoAfterDelay = TimeSpan.FromSeconds(2);
}
