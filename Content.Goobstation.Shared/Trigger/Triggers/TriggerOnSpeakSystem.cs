// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Speech;
using Content.Shared.Speech.Components;
using Content.Shared.Trigger.Systems;
using Robust.Shared.Containers;

namespace Content.Goobstation.Shared.Trigger.Triggers;

public sealed class TriggerOnSpeakSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly TriggerSystem _trigger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerOnSpeakComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<TriggerOnSpeakComponent, ListenEvent>(OnListen);
    }

    private void OnMapInit(Entity<TriggerOnSpeakComponent> ent, ref MapInitEvent args)
    {
        var listener = EnsureComp<ActiveListenerComponent>(ent);
        listener.Range = ent.Comp.ListenRange;
        //Dirty(ent, listener); // uncomment if speech ever gets predicted...
    }

    private void OnListen(Entity<TriggerOnSpeakComponent> ent, ref ListenEvent args)
    {
        var speaker = args.Source;
        if (speaker == ent.Owner)
        {
            _trigger.Trigger(ent, speaker, ent.Comp.KeyOut);
            return;
        }

        if (_container.TryGetContainingContainer(ent.Owner, out var container) && container.Owner == speaker)
            _trigger.Trigger(ent, speaker, ent.Comp.KeyOut);
    }
}
