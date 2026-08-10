// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Speech;

namespace Content.Trauma.Shared.Speech;

public sealed partial class CavemanAccentSystem : EntitySystem
{
    [Dependency] private SimpleAccentSystem _accent = default!;

    private static readonly ProtoId<SimpleAccentPrototype> Accent = "Caveman";

    [SubscribeLocalEvent]
    private void OnAccentGet(Entity<CavemanAccentComponent> ent, ref AccentGetEvent args)
    {
        _accent.ApplyAccent(Accent, ref args);
    }
}
