// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Shitcode.Common.Wizard;

[Serializable, NetSerializable]
public readonly struct ChargeSpellRaysEffectEvent(NetEntity uid)
{
    public readonly NetEntity Uid = uid;
}
