// SPDX-License-Identifier: AGPL-3.0-or-later

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
