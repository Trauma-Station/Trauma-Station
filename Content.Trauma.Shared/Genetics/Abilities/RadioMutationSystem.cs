// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Radio.Components;
using Content.Trauma.Shared.Genetics.Mutations;

namespace Content.Trauma.Shared.Genetics.Abilities;

public sealed class RadioMutationSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RadioMutationComponent, MutationAddedEvent>(OnAdded);
        SubscribeLocalEvent<RadioMutationComponent, MutationRemovedEvent>(OnRemoved);
    }

    private void OnAdded(Entity<RadioMutationComponent> ent, ref MutationAddedEvent args)
    {
        var mob = args.Target;
        EnsureComp<IntrinsicRadioReceiverComponent>(mob);
        var active = EnsureComp<ActiveRadioComponent>(mob);
        var transmitter = EnsureComp<IntrinsicRadioTransmitterComponent>(mob);
        foreach (var channel in ent.Comp.Channels)
        {
            if (active.Channels.Add(channel))
                ent.Comp.AddedActive.Add(channel);
            if (transmitter.Channels.Add(channel))
                ent.Comp.AddedTransmitters.Add(channel);
        }
        Dirty(ent);
    }

    private void OnRemoved(Entity<RadioMutationComponent> ent, ref MutationRemovedEvent args)
    {
        var mob = args.Target;
        if (!TryComp<ActiveRadioComponent>(mob, out var active) ||
            !TryComp<IntrinsicRadioTransmitterComponent>(mob, out var transmitter))
            return;

        // remove the channels
        foreach (var channel in ent.Comp.AddedActive)
        {
            active.Channels.Remove(channel);
        }

        foreach (var channel in ent.Comp.AddedTransmitters)
        {
            transmitter.Channels.Remove(channel);
        }

        ent.Comp.AddedActive.Clear();
        ent.Comp.AddedTransmitters.Clear();
        Dirty(ent);

        // clean up unused components now
        if (active.Channels.Count == 0)
        {
            RemComp(mob, active);
            RemComp<IntrinsicRadioReceiverComponent>(mob);
        }
        if (transmitter.Channels.Count == 0)
            RemComp(mob, transmitter);
    }
}
