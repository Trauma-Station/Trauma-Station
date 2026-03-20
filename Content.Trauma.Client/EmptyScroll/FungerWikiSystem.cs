// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.EmptyScroll;
using Robust.Client.UserInterface;

namespace Content.Trauma.Client.EmptyScroll;

/// <summary>
/// Opens funger wiki when you fail to write an empty scroll.
/// </summary>
public sealed class FungerWikiSystem : EntitySystem
{
    [Dependency] private readonly IUriOpener _uri = default!;

    public const string Wiki = "https://fearandhunger.wiki.gg/wiki/Empty_Scroll";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<PrayerFailedEvent>(OnPrayerFailed);
    }

    private void OnPrayerFailed(PrayerFailedEvent args)
    {
        _uri.OpenUri(Wiki);
    }
}
