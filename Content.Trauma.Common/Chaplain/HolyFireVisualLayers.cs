// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Trauma.Common.Chaplain;

[Serializable, NetSerializable]
public enum HolyFireVisuals : byte
{
    OnFire,
    FireStacks,
    HolyFire
}
