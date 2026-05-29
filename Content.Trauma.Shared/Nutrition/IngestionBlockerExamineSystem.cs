// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Examine;
using Content.Shared.Nutrition.Components;

namespace Content.Trauma.Shared.Nutrition;

/// <summary>
/// Adds an examine for all ingestion blocking clothes.
/// </summary>
public sealed class IngestionBlockerExamineSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IngestionBlockerComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<IngestionBlockerComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.BlockSmokeIngestion)
            args.PushMarkup(Loc.GetString("ingestion-blocker-block-smoke-examine"));
    }
}
