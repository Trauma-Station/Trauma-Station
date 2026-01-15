using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;

namespace Content.Trauma.Shared.ChangeFactionOnEmag;

public sealed class ChangeFactionOnEmagSystem : EntitySystem
{
    [Dependency] private readonly NpcFactionSystem _factionSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ChangeFactionOnEmagComponent, GotEmaggedEvent>(OnEmagged);
    }

    private void OnEmagged(Entity<ChangeFactionOnEmagComponent> ent, ref GotEmaggedEvent args)
    {
        if(HasComp<EmaggedComponent>(ent) || args.Type == EmagType.Access)
            return;

        args.Handled = true;

        EnsureComp<NpcFactionMemberComponent>(ent, out var factioncomp);
        EnsureComp<EmaggedComponent>(ent);

        _factionSystem.ClearFactions((ent.Owner, factioncomp));
        _factionSystem.AddFaction((ent.Owner, factioncomp), ent.Comp.Faction);

        Dirty(ent, factioncomp);
    }
}
