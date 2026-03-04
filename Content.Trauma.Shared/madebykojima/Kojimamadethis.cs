// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body;
using Content.Shared.Examine;

namespace Content.Trauma.Shared.madebykojima;

/// <summary>
/// This handles...
/// </summary>
public sealed class Kojimamadethis : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<BodyComponent, ExaminedEvent>(Kojimamadeit);
    }

    private void Kojimamadeit(Entity<BodyComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup("[color=red]Original character by: Hideo Kojima[/color]");
        args.PushMarkup("[color=red]Written by: Hideo Kojima[/color]");
    }
}
