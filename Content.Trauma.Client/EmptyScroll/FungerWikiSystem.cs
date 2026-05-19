// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.EmptyScroll;
using Robust.Client.UserInterface;

namespace Content.Trauma.Client.EmptyScroll;

/// <summary>
/// Opens funger wiki when you fail to write an empty scroll.
/// </summary>
public sealed partial class FungerWikiSystem : EntitySystem
{
    [Dependency] private IUriOpener _uri = default!;

    public const string Wiki = "https://fearandhunger.wiki.gg/wiki/Empty_Scroll";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PrayerFailedEvent>(OnPrayerFailed);
    }

    private void OnPrayerFailed(ref PrayerFailedEvent args)
    {
        _uri.OpenUri(Wiki);
    }
}
