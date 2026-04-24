// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DoAfter;

namespace Content.Trauma.Shared.Vampires;

/// <summary>
/// Component that allows user to drain blood from a valid entity by attacking them in combat mode,
/// whilst the head of the target is targeted.
///
/// Convert blood into charges, if draining was successful.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class VampireBloodsuckingComponent : Component;

/// <summary>
/// Raised on the <see cref="VampireBloodsuckingComponent"/> entity, after the bloodsucking process starts.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class BloodSuckDoAfterEvent : SimpleDoAfterEvent;
