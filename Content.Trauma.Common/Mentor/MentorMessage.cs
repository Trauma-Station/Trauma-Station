namespace Content.Trauma.Common.Mentor;

[Serializable, NetSerializable]
public readonly record struct MentorMessage(
    NetUserId Destination,
    string DestinationName,
    NetUserId Author,
    string AuthorName,
    string Text,
    DateTime Time,
    bool IsMentor
);
