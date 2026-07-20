// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Mentor;

[Serializable, NetSerializable]
public sealed class SendMentorHelpMessageEvent(string message) : EntityEventArgs
{
    public readonly string Message = message;
}
