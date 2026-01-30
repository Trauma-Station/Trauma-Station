// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Trauma.Common.Botany;
using Content.Trauma.Common.Lathe;

namespace Content.Goobstation.Shared.NTR.Scan;

/// <summary>
/// Prevent some ways to farm infinite points.
/// </summary>
public sealed class AntiCheeseSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ScannableForPointsComponent, ProducedByLatheEvent>(OnProducedByLathe);
        SubscribeLocalEvent<ScannableForPointsComponent, ProduceGrownEvent>(OnProduceGrown);
    }

    private void OnProducedByLathe(Entity<ScannableForPointsComponent> ent, ref ProducedByLatheEvent args)
    {
        // no emagged lathe
        ent.Comp.Points = 0;
    }

    private void OnProduceGrown(Entity<ScannableForPointsComponent> ent, ref ProduceGrownEvent args)
    {
        // no gatfruit farm
        ent.Comp.Points = 0;
    }
}
