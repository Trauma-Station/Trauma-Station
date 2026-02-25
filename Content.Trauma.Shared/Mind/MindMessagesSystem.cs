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

        SubscribeLocalEvent<MindContainerComponent, EntitySpokeEvent>(OnSpoke);
    }

    private void OnSpoke(Entity<MindContainerComponent> ent, ref EntitySpokeEvent args)
    {
        if (GetMessages(ent.Comp.Mind) is {} comp)
            AddMessage(comp, args.Message);
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
