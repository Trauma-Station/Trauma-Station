// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Trauma.Shared.SpeechPro;

[RegisterComponent, NetworkedComponent]
public sealed partial class SpeechProComponent : Component
{
}

[Serializable, NetSerializable]
public enum SpeechProUiKey : byte
{
    Key,
}
