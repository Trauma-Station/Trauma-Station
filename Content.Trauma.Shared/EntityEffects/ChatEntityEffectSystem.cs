using Content.Shared.Chat;
using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Forces target to say message in IC chat
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed class ChatEntityEffectSystem : EntityEffectSystem<MetaDataComponent, Chat>
{
    [Dependency] private readonly SharedChatSystem _chat = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<Chat> args)
    {
        _chat.TrySendInGameICMessage(entity, Loc.GetString(args.Effect.Message), args.Effect.Type, args.Effect.Range);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class Chat : EntityEffectBase<Chat>
{
    [DataField]
    public InGameICChatType Type = InGameICChatType.Speak;

    [DataField(required: true)]
    public LocId Message;

    [DataField]
    public ChatTransmitRange Range = ChatTransmitRange.Normal;
}
