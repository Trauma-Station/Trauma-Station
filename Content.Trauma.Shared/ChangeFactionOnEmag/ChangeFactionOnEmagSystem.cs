using Content.Shared.Emag.Systems;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;

namespace Content.Trauma.Shared.ChangeFactionOnEmag;

public sealed class ChangeFactionOnEmagSystem : EntitySystem
{
    [Dependency] private readonly NpcFactionSystem _factionSystem = default!;
    [Dependency] private readonly EmagSystem _emag = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ChangeFactionOnEmagComponent, GotEmaggedEvent>(OnEmagged);
    }

    private void OnEmagged(Entity<ChangeFactionOnEmagComponent> ent, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (_emag.CheckFlag(ent, EmagType.Interaction))
            return;

        args.Handled = true;

        EnsureComp<NpcFactionMemberComponent>(ent, out var factioncomp);

        _factionSystem.ClearFactions((ent.Owner, factioncomp));
        _factionSystem.AddFaction((ent.Owner, factioncomp), ent.Comp.Faction);

        Dirty(ent, factioncomp);
    }
}
