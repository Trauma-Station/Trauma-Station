// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.EntityEffects;

/// <summary>
/// Events raised by RaiseNetworkEvents entity effect
/// Contains Entity that entity effect is applied to
/// </summary>
[Serializable, NetSerializable, ImplicitDataDefinitionForInheritors]
public abstract partial class EntityEffectNetworkEvent : EntityEventArgs
{
    [DataField]
    public NetEntity? Entity;
}
