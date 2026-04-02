// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Medical;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.Emoting;

public abstract class SharedAnimatedEmotesSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly VomitSystem _vomit = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnimatedEmotesComponent, EmoteEvent>(OnEmote);
        SubscribeLocalEvent<AnimatedEmotesComponent, BeforeEmoteEvent>(OnBeforeEmote);
    }

    private void OnBeforeEmote(Entity<AnimatedEmotesComponent> ent, ref BeforeEmoteEvent args)
    {
        var emote = _proto.Index<EmotePrototype>(args.Emote);
        if (emote.Event is not AnimationEmoteEvent { CausesVomit: true })
            return;

        if (_status.HasStatusEffect(ent, ent.Comp.BlockVomitEmoteStatus))
            args.Cancel();
    }

    private void OnEmote(Entity<AnimatedEmotesComponent> ent, ref EmoteEvent args)
    {
        PlayEmoteAnimation(ent.AsNullable(), args.Emote.ID);

        var emote = _proto.Index<EmotePrototype>(args.Emote);
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
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.Emote = prot;
        Dirty(ent);
    }
}
