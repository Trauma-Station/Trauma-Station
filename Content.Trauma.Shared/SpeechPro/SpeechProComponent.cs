// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.SpeechPro;

/// <summary>
/// Allows an item to speak predefined phrase prototypes through its UI.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SpeechProComponent : Component;

[Serializable, NetSerializable]
public enum SpeechProUiKey : byte
{
    Key,
}
