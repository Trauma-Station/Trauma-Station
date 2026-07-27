// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Gateway;

[Serializable, NetSerializable]
public enum GatewayVisuals : byte
{
    Active
}

[Serializable, NetSerializable]
public enum GatewayVisualLayers : byte
{
    Portal
}

[Serializable, NetSerializable]
public enum GatewayUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class GatewayBoundUserInterfaceState : BoundUserInterfaceState
{
    /// <summary>
    /// List of enabled destinations and information about them.
    /// </summary>
    public readonly List<GatewayDestinationData> Destinations;

    /// <summary>
    /// Which destination it is currently linked to, if any.
    /// </summary>
    public readonly NetEntity? Current;

    public GatewayBoundUserInterfaceState(List<GatewayDestinationData> destinations, NetEntity? current)
    {
        Destinations = destinations;
        Current = current;
    }
}

[Serializable, NetSerializable]
public record struct GatewayDestinationData
{
    public NetEntity Entity;

    public FormattedMessage Name;

    /// <summary>
    /// Is the portal currently open.
    /// </summary>
    public bool Portal;
}

[Serializable, NetSerializable]
public sealed class GatewayOpenPortalMessage(NetEntity destination) : BoundUserInterfaceMessage
{
    public NetEntity Destination = destination;
}
