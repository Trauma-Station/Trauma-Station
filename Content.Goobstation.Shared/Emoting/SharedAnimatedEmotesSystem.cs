// SPDX-FileCopyrightText: 2024 username <113782077+whateverusername0@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 whateverusername0 <whateveremail>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
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

    private void OnEmote(EntityUid uid, AnimatedEmotesComponent component, ref EmoteEvent args)
    {
        PlayEmoteAnimation(uid, component, args.Emote.ID);

        var emote = _proto.Index<EmotePrototype>(args.Emote);
        if (emote.Event is not AnimationEmoteEvent { CausesVomit: true })
            return;

        if (_status.HasStatusEffect(uid, component.BlockVomitEmoteStatus))
            return;

        if (!_status.TryUpdateStatusEffectDuration(uid,
                component.VomitStatus,
                out var effect,
                component.VomitStatusTime))
            return;

        var counter = EnsureComp<CounterStatusEffectComponent>(effect.Value);
        counter.Count++;
        if (counter.Count < component.EmotesToVomit)
            return;

        _vomit.Vomit(uid);
        _status.TryAddStatusEffect(uid, component.BlockVomitEmoteStatus, out _, component.BlockVomitStatusTime);
    }

    public void PlayEmoteAnimation(EntityUid uid, AnimatedEmotesComponent component, ProtoId<EmotePrototype> prot)
    {
        component.Emote = prot;
        Dirty(uid, component);
    }
}
