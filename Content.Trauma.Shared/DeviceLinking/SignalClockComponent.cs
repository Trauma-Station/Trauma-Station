// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DeviceLinking;

namespace Content.Trauma.Shared.DeviceLinking;

/// <summary>
/// Sends an alternating high/low signal at the server's tickrate.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SignalClockComponent : Component
{
    [DataField]
    public ProtoId<SourcePortPrototype> Port = "Clock";
}
