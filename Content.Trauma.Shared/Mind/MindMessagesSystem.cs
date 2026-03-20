// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Chat;
using Content.Shared.Mind.Components;

namespace Content.Trauma.Shared.Mind;

public sealed class MindMessagesSystem : EntitySystem
{
    private EntityQuery<MindMessagesComponent> _query;

    public override void Initialize()
    {
        base.Initialize();

        _query = GetEntityQuery<MindMessagesComponent>();

        // relay event to the mind, other systems can use it too
        SubscribeLocalEvent<MindContainerComponent, EntitySpokeEvent>(OnContainerSpoke);
        SubscribeLocalEvent<MindMessagesComponent, EntitySpokeEvent>(OnSpoke);
    }

    private void OnContainerSpoke(Entity<MindContainerComponent> ent, ref EntitySpokeEvent args)
    {
        if (ent.Comp.Mind is {} mind)
            RaiseLocalEvent(mind, args);
    }

    private void OnSpoke(Entity<MindMessagesComponent> ent, ref EntitySpokeEvent args)
    {
        AddMessage(ent.Comp, args.Message);
    }

    public void AddMessage(MindMessagesComponent comp, string message)
    {
        comp.Messages[comp.Index] = message;
        comp.Index++;
        comp.Index %= comp.Messages.Length;
    }

    public MindMessagesComponent? GetMessages(EntityUid? mind)
        => mind != null && _query.TryComp(mind.Value, out var comp)
            ? comp
            : null;

    /// <summary>
    /// Get one of the last messages for a mind, with 0 being the oldest.
    /// </summary>
    public string GetMessage(MindMessagesComponent comp, int i)
        => comp.Messages[(comp.Index + i) % comp.Messages.Length];
}
