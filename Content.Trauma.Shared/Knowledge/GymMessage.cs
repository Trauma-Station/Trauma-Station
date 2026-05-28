// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Knowledge;

[Serializable, NetSerializable]
public enum GymUiKey : byte
{
    Key
}

/// <summary>
/// Fired from client to server when client takes stamina damage.
/// </summary>
[Serializable, NetSerializable]
public sealed class GymRepTryMessage(int staminaMultiplier, float timingAccuracy) : BoundUserInterfaceMessage
{
    public int StaminaMultiplier = staminaMultiplier;
    public float TimingAccuracy = timingAccuracy;
}

/// <summary>
/// Fired from client to server when client has done enough for experience.
/// </summary>
[Serializable, NetSerializable]
public sealed class GymRepPerformedMessage(float timingAccuracy) : BoundUserInterfaceMessage
{
    public float TimingAccuracy = timingAccuracy;
}
