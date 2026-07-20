using Content.Shared.Chat;

namespace Content.Shared.Speech.Components;

public sealed partial class SpeakOnActionComponent
{
    [DataField]
    public InGameICChatType ChatType = InGameICChatType.Speak;
}
