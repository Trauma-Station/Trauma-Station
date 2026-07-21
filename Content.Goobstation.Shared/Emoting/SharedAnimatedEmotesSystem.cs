// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.Medical;
using Content.Shared.StatusEffectNew;

namespace Content.Goobstation.Shared.Emoting;

public abstract partial class SharedAnimatedEmotesSystem : EntitySystem
{
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private VomitSystem _vomit = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;

    [SubscribeLocalEvent]
    private void OnBeforeEmote(Entity<AnimatedEmotesComponent> ent, ref BeforeEmoteEvent args)
    {
        var emote = ProtoMan.Index<EmotePrototype>(args.Emote);
        if (emote.Event is not AnimationEmoteEvent { CausesVomit: true })
            return;

        if (_status.HasStatusEffect(ent, ent.Comp.BlockVomitEmoteStatus))
            args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnEmote(Entity<AnimatedEmotesComponent> ent, ref EmoteEvent args)
    {
        PlayEmoteAnimation(ent.AsNullable(), args.Emote);

        var emote = ProtoMan.Index<EmotePrototype>(args.Emote);
        if (emote.Event is not AnimationEmoteEvent { CausesVomit: true })
            return;

        if (_status.HasStatusEffect(ent, ent.Comp.BlockVomitEmoteStatus))
            return;

        if (!_status.TryUpdateStatusEffectDuration(ent,
                ent.Comp.VomitStatus,
                out var effect,
                ent.Comp.VomitStatusTime))
            return;

        var counter = EnsureComp<CounterStatusEffectComponent>(effect.Value);
        counter.Count++;
        if (counter.Count < ent.Comp.EmotesToVomit)
            return;

        _vomit.Vomit(ent);
        _status.TryAddStatusEffect(ent, ent.Comp.BlockVomitEmoteStatus, out _, ent.Comp.BlockVomitStatusTime);
    }

    public void PlayEmoteAnimation(Entity<AnimatedEmotesComponent?> ent, ProtoId<EmotePrototype> prot)
    {
        PlayEmoteAnimation(ent, ProtoMan.Index(prot));
    }

    public void PlayEmoteAnimation(Entity<AnimatedEmotesComponent?> ent, EmotePrototype prot)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.Emote = prot.ID;
        Dirty(ent);

        if (prot.EffectsOnEmote is { } effects)
            _effects.ApplyEffects(ent, effects, 1f, ent);
    }
}
