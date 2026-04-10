// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Shared.Heretic.Components.StatusEffects;

[RegisterComponent, NetworkedComponent]
public sealed partial class StarTouchedStatusEffectComponent : Component
{
    [DataField]
    public TimeSpan SleepTime = TimeSpan.FromSeconds(8);

    [DataField]
    public EntProtoId CosmicCloud = "EffectCosmicCloud";
}
