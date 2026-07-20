// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Shared.Heretic.Components.PathSpecific.Lock;

[RegisterComponent, NetworkedComponent]
public sealed partial class LastRefugeActionComponent : Component
{
    [DataField]
    public float OtherMindsCheckRange = 5f;
}
