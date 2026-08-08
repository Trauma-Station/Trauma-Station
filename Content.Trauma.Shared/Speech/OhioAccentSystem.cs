// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Speech;
using Content.Shared.Speech;

namespace Content.Trauma.Shared.Speech;

public sealed partial class OhioAccentSystem : EntitySystem
{
    [Dependency] private SimpleAccentSystem _accent = default!;

    private static readonly ProtoId<SimpleAccentPrototype> Accent = "Ohio";

    [SubscribeLocalEvent]
    private void OnAccentGet(Entity<OhioAccentComponent> ent, ref AccentGetEvent args)
    {
        _accent.ApplyAccent(Accent, ref args);
    }
}
