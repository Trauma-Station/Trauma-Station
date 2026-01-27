// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Chat;
using Content.Shared.Dataset;
using Content.Shared.EntityEffects;
using Content.Shared.Random.Helpers;
using Content.Shared.Speech;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Makes the target entity say a random line from a localized dataset.
/// It can also have a string prefixed.
/// </summary>
public sealed partial class Speak : EntityEffectBase<Speak>
{
    [DataField(required: true)]
    public ProtoId<LocalizedDatasetPrototype> Id;

    [DataField]
    public LocId? Prefix;

    [DataField]
    public bool HideChat;

    [DataField]
    public LocId GuidebookText = "entity-effect-guidebook-speak";

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString(GuidebookText, ("chance", Probability));
}

public sealed class SpeakEffectSystem : EntityEffectSystem<SpeechComponent, Speak>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedChatSystem _chat = default!;

    protected override void Effect(Entity<SpeechComponent> ent, ref EntityEffectEvent<Speak> args)
    {
        var proto = _proto.Index(args.Effect.Id);
        var picked = _random.Pick(proto); // predicting rng doesn't matter, chat isn't predicted

        // prepend the prefix
        if (args.Effect.Prefix is {} prefix)
            picked = Loc.GetString(prefix) + picked;

        // this is still logged so admins can know e.g. what started a dispute, it would look bad say
        // if you say fuck 8 times to pun pun and he starts attacking you
        // vs you say nothing for 30s and pun pun randomly attacks you according to evil logs
        _chat.TrySendInGameICMessage(ent, picked, InGameICChatType.Speak, hideChat: args.Effect.HideChat);
    }
}
