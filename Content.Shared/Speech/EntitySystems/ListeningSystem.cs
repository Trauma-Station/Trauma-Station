// <Trauma>
using Content.Shared.Random.Helpers;
using Robust.Shared.Timing;
// </Trauma>
using Content.Shared.Chat;
using Content.Shared.Speech.Components;

namespace Content.Shared.Speech.EntitySystems;

/// <summary>
/// This system redirects local chat messages to listening entities (e.g., radio microphones).
/// </summary>
public sealed partial class ListeningSystem : EntitySystem
{
    // <Trauma>
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedChatSystem _chat = default!;
    // </Trauma>
    [Dependency] private SharedTransformSystem _xforms = default!;

    [SubscribeLocalEvent]
    private void OnSpeak(EntitySpokeEvent ev)
    {
        PingListeners(ev.Source, ev.Message, ev.IsWhisper, ev.Language.ID); // Trauma - change obfuscated to whisper, add language
    }

    /// <summary>
    /// Sends a speech message to entities listening within range.
    /// </summary>
    public void PingListeners(EntityUid source, string message, bool isWhisper, string language) // Trauma - change obfuscated to whisper, add language
    {
        // TODO whispering / audio volume? Microphone sensitivity?
        // for now, whispering just arbitrarily reduces the listener's max range.

        var sourceXform = Transform(source);
        var sourcePos = _xforms.GetWorldPosition(sourceXform);

        var attemptEv = new ListenAttemptEvent(source);
        // <Trauma> - use language and obfuscate the message here instead of just setting a bool
        var ev = new ListenEvent(message, source, language);
        var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(source));
        var obfuscatedEv = !isWhisper ? null : new ListenEvent(_chat.ObfuscateMessageReadability(message, rand), source, language);
        // </Trauma>
        var query = EntityQueryEnumerator<ActiveListenerComponent, TransformComponent>();

        while(query.MoveNext(out var listenerUid, out var listener, out var xform))
        {
            if (xform.MapID != sourceXform.MapID)
                continue;

            // range checks
            // TODO proper speech occlusion
            var distance = (sourcePos - _xforms.GetWorldPosition(xform)).LengthSquared();
            if (distance > listener.Range * listener.Range)
                continue;

            RaiseLocalEvent(listenerUid, attemptEv);
            if (attemptEv.Cancelled)
            {
                attemptEv.Uncancel();
                continue;
            }

            if (obfuscatedEv != null && distance > SharedChatSystem.WhisperClearRange)
                RaiseLocalEvent(listenerUid, obfuscatedEv);
            else
                RaiseLocalEvent(listenerUid, ev);
        }
    }
}
