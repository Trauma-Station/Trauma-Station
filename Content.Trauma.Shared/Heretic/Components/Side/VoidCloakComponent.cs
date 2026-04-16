// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Shared.Heretic.Components.Side;

[RegisterComponent, NetworkedComponent]
public sealed partial class VoidCloakComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public bool Transparent;
}

[Serializable, NetSerializable]
public enum VoidCloakVisuals : byte
{
    Transparent,
}
