// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Emoting;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.Emoting;

public abstract class SharedAnimatedEmotesSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnimatedEmotesComponent, EmoteEvent>(OnEmote);
    }

    private void OnEmote(Entity<AnimatedEmotesComponent> ent, ref EmoteEvent args)
    {
        PlayEmoteAnimation((ent, ent.Comp), args.Emote.ID);
    }

    public void PlayEmoteAnimation(Entity<AnimatedEmotesComponent?> ent, ProtoId<EmotePrototype> emote)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.Emote = emote;
        Dirty(ent, ent.Comp);
    }
}
