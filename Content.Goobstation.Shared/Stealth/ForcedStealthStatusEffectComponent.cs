// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

// god the name
namespace Content.Goobstation.Shared.Stealth;

[RegisterComponent, NetworkedComponent]
public sealed partial class ForcedStealthStatusEffectComponent : Component
{
    [DataField] public float Visibility = 0f;

    /// <summary>
    /// Null if the target entity wasn't stealthed beforehand.
    /// </summary>
    [DataField]
    public float? OldVisibility;
}
