// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.StatusEffects;

/// <summary>
/// Status effect component that removes the status effect once the owner ignites.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RemoveOnIgniteStatusEffectComponent : Component
{
    /// <summary>
    /// The effect prototype
    /// </summary>
    [DataField(required: true)]
    public EntProtoId EffectProto;
}
