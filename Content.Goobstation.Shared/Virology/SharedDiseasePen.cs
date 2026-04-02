// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Goobstation.Shared.Virology;

[Serializable, NetSerializable]
public enum DiseasePenVisuals : byte
{
    Used,
}

[Serializable, NetSerializable]
public sealed partial class DiseasePenInjectEvent : SimpleDoAfterEvent;
