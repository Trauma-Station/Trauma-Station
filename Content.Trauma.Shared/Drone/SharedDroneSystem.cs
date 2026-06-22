// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Shared.Drone;

public abstract class SharedDroneSystem : EntitySystem
{
    [Serializable, NetSerializable]
    public enum DroneVisuals : byte
    {
        Status
    }

    [Serializable, NetSerializable]
    public enum DroneStatus : byte
    {
        Off,
        On
    }
}
