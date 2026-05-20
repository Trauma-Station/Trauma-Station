// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Weapons.SheathCounterattack;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CounterAttackingStatusEffectComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? ActiveSheath;

    [DataField, AutoNetworkedField]
    public EntityUid? ActiveWeapon;

    [DataField, AutoNetworkedField]
    public EntityUid? Target;

    [DataField, AutoNetworkedField]
    public TimeSpan BlockEffectTime;

    [DataField]
    public EntProtoId<BlockCounterAttackStatusEffectComponent> BlockStatusEffect = "BlockCounterAttackStatusEffect";
}
