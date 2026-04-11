// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Chat.Managers;

namespace Content.Server.Tabletop;

public sealed partial class TabletopSystem
{
    [Dependency] private readonly IChatManager _chat = default!;
}
