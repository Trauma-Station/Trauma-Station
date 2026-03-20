using Robust.Shared.Serialization;

namespace Content.Shared.Heretic.Messages;

[Serializable, NetSerializable]
public sealed class FeastOfOwlsMessage(bool accepted) : BoundUserInterfaceMessage
{
    public readonly bool Accepted = accepted;
}

[Serializable, NetSerializable]
public enum FeastOfOwlsUiKey : byte
{
    Key
}
