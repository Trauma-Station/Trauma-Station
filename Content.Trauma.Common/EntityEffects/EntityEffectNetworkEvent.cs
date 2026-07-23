// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.EntityEffects;

[Serializable, NetSerializable, ImplicitDataDefinitionForInheritors]
public abstract partial class EntityEffectNetworkEvent : EntityEventArgs
{
    [DataField]
    public NetEntity? Entity;
}
