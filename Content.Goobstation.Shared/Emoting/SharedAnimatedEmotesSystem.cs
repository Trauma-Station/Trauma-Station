// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.EntityConditions;
using Content.Shared.EntityEffects;

namespace Content.Goobstation.Shared.Emoting;

public abstract partial class SharedAnimatedEmotesSystem : EntitySystem
{
    [Dependency] private SharedEntityEffectsSystem _effects = default!;
    [Dependency] private SharedEntityConditionsSystem _conditions = default!;

    [SubscribeLocalEvent]
    private void OnBeforeEmote(Entity<AnimatedEmotesComponent> ent, ref BeforeEmoteEvent args)
    {
        var emote = ProtoMan.Index<EmotePrototype>(args.Emote);
        if (!_conditions.TryConditions(ent, emote.Conditions, ent))
            args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnEmote(Entity<AnimatedEmotesComponent> ent, ref EmoteEvent args)
    {
        PlayEmoteAnimation(ent, args.Emote);
    }

    public void PlayEmoteAnimation(EntityUid ent, ProtoId<EmotePrototype> prot)
    {
        PlayEmoteAnimation(ent, ProtoMan.Index(prot));
    }

    public void PlayEmoteAnimation(EntityUid ent, EmotePrototype prot)
    {
        if (!_conditions.TryConditions(ent, prot.Conditions, ent))
            return;

        if (prot.EffectsOnEmote is { } effects)
            _effects.ApplyEffects(ent, effects, 1f, ent);
    }
}
