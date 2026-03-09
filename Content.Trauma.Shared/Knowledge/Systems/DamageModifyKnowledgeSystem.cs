// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage.Systems;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Shared.Knowledge.Components;

namespace Content.Trauma.Shared.Knowledge.Systems;

public sealed class DamageModifyKnowledgeSystem : EntitySystem
{
    [Dependency] private readonly SharedKnowledgeSystem _knowledge = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KnowledgeHolderComponent, BeforeDamageChangedEvent>(_knowledge.RelayActiveEvent);
        SubscribeLocalEvent<DamageModifyKnowledgeComponent, BeforeDamageChangedEvent>(OnBeforeDamageChanged);
    }

    private void OnBeforeDamageChanged(Entity<DamageModifyKnowledgeComponent> ent, ref BeforeDamageChangedEvent args)
    {
        // most environment things like radiation should have no origin?
        if (args.Damage.GetTotal() <= 0 || args.Origin == null)
            return;

        var level = _knowledge.GetLevel(ent.Owner);
        args.Damage *= ent.Comp.Curve.GetCurve(level);
    }
}
