using Content.Shared.EntityConditions;
using Content.Shared.EntityEffects;

namespace Content.Shared.Chat.Prototypes;

public sealed partial class EmotePrototype
{
    [DataField]
    public EntityEffect[]? EffectsOnEmote;

    [DataField]
    public EntityCondition[]? Conditions;
}
