// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DoAfter;

namespace Content.Trauma.Shared.DeadmanSwitch;

/// <summary>
/// Raised when a user finishes toggling the deadman's switch in their hands.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class DeadmanSwitchDoAfterEvent : SimpleDoAfterEvent;
