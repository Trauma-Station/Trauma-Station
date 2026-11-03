// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Trauma.Shared.Heretic.Components;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.BloodCult.Spells;

/// <summary>
/// Mark applied to victims that were hit by stun spell
/// Triggered by hitting with any cult weapon, deals stamina damage on trigger
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class BloodCultCombatMarkComponent : BaseSpriteOverlayComponent
{
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(15);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan DisappearTime;

    [DataField]
    public ProtoId<EntityEffectPrototype> Effect = "CultCombatMarkTriggerEffect";

    [DataField]
    public override SpriteSpecifier? Sprite { get; set; } =
        new SpriteSpecifier.Rsi(new("_Trauma/BloodCult/Effects/mark.rsi"), "cult-mark");

    public override Enum Key { get; set; } = BloodCultCombatMarkKey.Key;
}

public enum BloodCultCombatMarkKey : byte
{
    Key,
}
