// <Trauma>
using Content.Trauma.Common.Language;
using Robust.Shared.Prototypes;
// </Trauma>
namespace Content.Shared.Speech;

public sealed class ListenEvent : EntityEventArgs
{
    public readonly string Message;
    public readonly EntityUid Source;
    public readonly ProtoId<LanguagePrototype> Language; // Trauma

    public ListenEvent(string message, EntityUid source,
        ProtoId<LanguagePrototype> language) // Trauma
    {
        Message = message;
        Source = source;
        Language = language; // Trauma
    }
}

public sealed class ListenAttemptEvent : CancellableEntityEventArgs
{
    public readonly EntityUid Source;

    public ListenAttemptEvent(EntityUid source)
    {
        Source = source;
    }
}
