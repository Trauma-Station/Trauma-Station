// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Heretic.Components.PathSpecific.Void;

[RegisterComponent, NetworkedComponent]
public sealed partial class RemoveOnAttackStatusEffectComponent : Component
{
    /// <summary>
    /// If effect was added recently (below threshold) this effect won't be removed on attack
    /// </summary>
    [DataField]
    public TimeSpan RemoveThreshold;
}
