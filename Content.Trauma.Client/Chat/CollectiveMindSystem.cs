// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Chat.Managers;
using Content.Trauma.Common.CollectiveMind;
using Robust.Client.Player;

namespace Content.Client.Chat
{
    public sealed class CollectiveMindSystem : EntitySystem
    {
        [Dependency] private readonly IChatManager _chat = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<CollectiveMindComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<CollectiveMindComponent, ComponentRemove>(OnRemove);
        }

        private void OnInit(EntityUid uid, CollectiveMindComponent component, ComponentInit args)
        {
            _chat.UpdatePermissions();
        }

        private void OnRemove(EntityUid uid, CollectiveMindComponent component, ComponentRemove args)
        {
            _chat.UpdatePermissions();
        }
    }
}
