// SPDX-License-Identifier: AGPL-3.0-or-later

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
