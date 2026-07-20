// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Lavaland.Shared.Anger.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class AdjustAngerOnHitComponent : Component
{
    [DataField]
    public float AdjustAngerOnAttack = 0.1f;
}
