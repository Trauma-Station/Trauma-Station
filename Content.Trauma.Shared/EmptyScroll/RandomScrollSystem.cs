// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Paper;
using Robust.Shared.Random;

namespace Content.Trauma.Shared.EmptyScroll;

public sealed class RandomScrollSystem : EntitySystem
{
    [Dependency] private readonly EmptyScrollSystem _scroll = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PaperSystem _paper = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomScrollComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<RandomScrollComponent> ent, ref MapInitEvent args)
    {
        var text = _random.Pick(_scroll.AllPrayerTexts);
        _paper.SetContent(ent.Owner, text);
    }
}
