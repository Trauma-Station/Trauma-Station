// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DeviceLinking;

namespace Content.Trauma.Shared.Radio;

/// <summary>
/// Sends a signal string with message text when this entity receives a radio message.
/// Integrated circuits can then work with the string.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SignalRadioReceiverComponent : Component
{
    [DataField(required: true)]
    public ProtoId<SourcePortPrototype> Port;
}
