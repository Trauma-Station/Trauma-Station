namespace Content.Trauma.Common.Mentor;

[Serializable, NetSerializable]
public sealed class SendMentorHelpMessageEvent(string message) : EntityEventArgs
{
    public readonly string Message = message;
}
