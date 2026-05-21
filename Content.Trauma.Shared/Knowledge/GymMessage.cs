namespace Content.Trauma.Shared.Knowledge;

[Serializable, NetSerializable]
public enum GymUiKey : byte
{
    Key
}

/// <summary>
/// Fired from client to server when client does a rep.
/// </summary>
[Serializable, NetSerializable]
public sealed class GymRepPerformedMessage(float timingAccuracy) : BoundUserInterfaceMessage
{
    public float TimingAccuracy = timingAccuracy;
}
