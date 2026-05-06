// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Chat.Managers;
using Content.Trauma.Common.CollectiveMind;
using Robust.Shared.Player;

namespace Content.Trauma.Client.Chat;

public sealed class CollectiveMindSystem : EntitySystem
{
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CollectiveMindComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<CollectiveMindComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<CollectiveMindComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<CollectiveMindComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);
    }

    private void OnInit(EntityUid uid, CollectiveMindComponent component, ComponentInit args)
    {
        UpdatePermissions(uid);
    }

    private void OnRemove(EntityUid uid, CollectiveMindComponent component, ComponentRemove args)
    {
        UpdatePermissions(uid);
    }

    private void OnPlayerAttached(Entity<CollectiveMindComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        _chat.UpdatePermissions();
    }

    private void OnPlayerDetached(Entity<CollectiveMindComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        _chat.UpdatePermissions();
    }

    private void UpdatePermissions(EntityUid uid)
    {
        if (uid == _player.LocalEntity)
            _chat.UpdatePermissions();
    }
}
