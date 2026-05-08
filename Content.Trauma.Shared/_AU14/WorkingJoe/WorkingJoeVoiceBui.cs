using Robust.Shared.Serialization;

namespace Content.Trauma.Shared._AU14.WorkingJoe;

[Serializable, NetSerializable]
public enum WorkingJoeVoiceUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class WorkingJoePlayLineMessage : BoundUserInterfaceMessage
{
    public string EmoteId;

    public WorkingJoePlayLineMessage(string emoteId)
    {
        EmoteId = emoteId;
    }
}
