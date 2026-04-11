// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DoAfter;

namespace Content.Goobstation.Shared.Keyring;

[Serializable, NetSerializable]
public sealed partial class KeyringDoAfterEvent : SimpleDoAfterEvent;
