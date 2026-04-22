// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Cargo.Components;
using Content.Trauma.Shared.Knowledge.Quality;
using Content.Trauma.Shared.Knowledge.Systems;

namespace Content.Trauma.Server.Knowledge;

public sealed class KnowledgeSystem : SharedKnowledgeSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StaticPriceComponent, ApplyQualityEvent>(OnApplyStaticPriceQuality);
    }

    private void OnApplyStaticPriceQuality(Entity<StaticPriceComponent> ent, ref ApplyQualityEvent args)
    {
        ent.Comp.Price *= args.Modifier(args.Proto.Price);
    }
}
