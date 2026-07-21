using Content.Shared.EntityEffects;

namespace Content.Shared.Chat.Prototypes;

public sealed partial class EmotePrototype
{
    [DataField]
    public object? Event;

    [DataField]
    public EntityEffect[]? EffectsOnEmote;
}
