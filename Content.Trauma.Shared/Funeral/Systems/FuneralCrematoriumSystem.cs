// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Funeral.Components;

/// <summary>
/// Determines crematorium output.
/// </summary>

namespace Content.Trauma.Common.Funeral;

public sealed class FuneralCrematoriumSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CremationOutputEvent>(OnCremationOutput);
    }

    private void OnCremationOutput(CremationOutputEvent args)
    {
        foreach (var entity in args.Contents)
        {
            if (!HasComp<FuneralHolyComponent>(entity))
                continue;

            args.OutputPrototype = "HolyAsh";
            return;
        }
    }
}
