// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.StatusEffects;

/// <summary>
/// Status effect component that removes the status effect once the owner ignites.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class RemoveOnIgniteStatusEffectComponent : Component
{
    /// <summary>
    /// The owner of the status effect
    /// </summary>
    [AutoNetworkedField]
    public EntityUid? StatusOwner;

    /// <summary>
    /// The effect prototype
    /// </summary>
    [DataField]
    public EntProtoId EffectProto;
}
