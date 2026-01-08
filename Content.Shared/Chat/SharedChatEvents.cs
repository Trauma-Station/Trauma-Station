using Content.Shared._EinsteinEngines.Language; // Trauma
using Content.Shared.Inventory;
using Content.Shared.Radio;
using Content.Shared.Speech;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chat;

/// <summary>
/// This event should be sent everytime an entity talks (Radio, local chat, etc...).
/// The event is sent to both the entity itself, and all clothing (For stuff like voice masks).
/// </summary>
public sealed class TransformSpeakerNameEvent : EntityEventArgs, IInventoryRelayEvent
{
    public SlotFlags TargetSlots { get; } = SlotFlags.WITHOUT_POCKET;
    public EntityUid Sender;
    public string VoiceName;
    public ProtoId<SpeechVerbPrototype>? SpeechVerb;

    public TransformSpeakerNameEvent(EntityUid sender, string name)
    {
        Sender = sender;
        VoiceName = name;
        SpeechVerb = null;
    }
}

/// <summary>
/// Raised broadcast in order to transform speech.transmit
/// </summary>
public sealed class TransformSpeechEvent : CancellableEntityEventArgs, IInventoryRelayEvent
{
    public SlotFlags TargetSlots { get; } = SlotFlags.WITHOUT_POCKET;
    public EntityUid Sender;
    public string Message;

    public TransformSpeechEvent(EntityUid sender, string message)
    {
        Sender = sender;
        Message = message;
    }
}

public sealed class CheckIgnoreSpeechBlockerEvent : EntityEventArgs
{
    public EntityUid Sender;
    public bool IgnoreBlocker;

    public CheckIgnoreSpeechBlockerEvent(EntityUid sender, bool ignoreBlocker)
    {
        Sender = sender;
        IgnoreBlocker = ignoreBlocker;
    }
}

/// <summary>
/// Raised on an entity when it speaks, either through 'say' or 'whisper'.
/// </summary>
public sealed class EntitySpokeEvent : EntityEventArgs
{
    public readonly EntityUid Source;
    public readonly string Message;
    public readonly bool IsWhisper; // EE - replaces ObfuscatedMessage
    public readonly LanguagePrototype Language; // EE

    /// <summary>
    /// If the entity was trying to speak into a radio, this was the channel they were trying to access. If a radio
    /// message gets sent on this channel, this should be set to null to prevent duplicate messages.
    /// </summary>
    public RadioChannelPrototype? Channel;

    // EE - added isWhisper, Language
    public EntitySpokeEvent(EntityUid source, string message, RadioChannelPrototype? channel, bool isWhisper, LanguagePrototype language)
    {
        Source = source;
        Message = message;
        Channel = channel;
        IsWhisper = isWhisper; // EE
        Language = language; // EE
    }
}

/// <summary>
/// Raised on a speaking entity to allow overriding the chat type.
/// Can't be put in common because it uses <see cref="InGameICChatType"/>.
/// </summary>
[ByRefEvent]
public record struct SpeechTypeOverrideEvent(InGameICChatType DesiredType);
