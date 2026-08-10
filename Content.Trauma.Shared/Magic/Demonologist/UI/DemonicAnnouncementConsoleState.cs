namespace Content.Trauma.Shared.Magic.Demonologist.UI;

[Serializable, NetSerializable]
public sealed class DemonicAnnouncementConsoleState : BoundUserInterfaceState
{
    public readonly bool CanAnnounce;

    public DemonicAnnouncementConsoleState(bool canAnnounce)
    {
        CanAnnounce = canAnnounce;
    }
}

[Serializable, NetSerializable]
public sealed class DemonicAnnouncementMessage : BoundUserInterfaceMessage
{
    public readonly string Message;
    public DemonicAnnouncementMessage(string message)
    {
        Message = message;
    }
}

[Serializable, NetSerializable]
public enum DemonicAnnouncementUiKey
{
    Key
}
