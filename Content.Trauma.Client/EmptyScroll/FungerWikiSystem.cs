// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.EmptyScroll;
using Robust.Client.UserInterface;
using Robust.Shared.Player;

namespace Content.Trauma.Client.EmptyScroll;

/// <summary>
/// Opens funger wiki when you fail to write an empty scroll.
/// </summary>
public sealed class FungerWikiSystem : EntitySystem
{
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly IUriOpener _uri = default!;

    public const string Wiki = "https://fearandhunger.fandom.com/wiki/Empty_Scroll";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PrayerFailedEvent>(OnPrayerFailed);
    }

    private void OnPrayerFailed(ref PrayerFailedEvent args)
    {
        if (args.User == _player.LocalEntity)
            _uri.OpenUri(Wiki);
    }
}
