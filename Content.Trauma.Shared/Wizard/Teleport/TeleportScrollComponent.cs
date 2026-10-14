// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Shared.Wizard.Teleport;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TeleportScrollComponent : Component
{
    [DataField, AutoNetworkedField]
    public int UsesLeft = 4; // TODO: what the FUCK is limited charges
}

[Serializable, NetSerializable]
public sealed class WizardTeleportLocationSelectedMessage(NetEntity location) : BoundUserInterfaceMessage
{
    public NetEntity Location = location;
}

[Serializable, NetSerializable]
public sealed class WizardTeleportState(List<WizardWarp> warps) : BoundUserInterfaceState
{
    public List<WizardWarp> Warps = warps;
}

[Serializable, NetSerializable]
public struct WizardWarp(NetEntity entity, string displayName)
{
    public NetEntity Entity = entity;

    public string DisplayName = displayName;
}

[Serializable, NetSerializable]
public enum WizardTeleportUiKey : byte
{
    Key
}
