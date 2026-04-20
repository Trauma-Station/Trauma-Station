// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Common.Wizard;

[Serializable, NetSerializable]
public sealed class ChargeSpellRaysEffectEvent(NetEntity uid) : EntityEventArgs
{
    public NetEntity Uid = uid;
}
