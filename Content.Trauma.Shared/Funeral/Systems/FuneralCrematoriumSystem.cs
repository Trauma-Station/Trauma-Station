// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.Funeral;
using Content.Shared.Morgue.Components;

namespace Content.Trauma.Shared.Funeral;

/// <summary>
/// Determines crematorium output.
/// </summary>
public sealed partial class FuneralCrematoriumSystem : EntitySystem
{
    [Dependency] private EntityQuery<FuneralHolyComponent> _compQuery = default!;

    private static readonly EntProtoId HolyAsh = "HolyAsh";

    [SubscribeLocalEvent]
    private void OnCremationOutput(Entity<CrematoriumComponent> ent, ref CremationOutputEvent args)
    {
        foreach (var entity in args.Contents)
        {
            if (_compQuery.HasComp(entity))
            {
                args.OutputPrototype = HolyAsh;
                return;
            }
        }
    }
}
