using Content.Shared._DV.CosmicCult.Components;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;

namespace Content.Shared._DV.CosmicCult.Abilities;

public abstract class SharedCosmicSiphonSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedCosmicCultSystem _cosmicCult = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CosmicCultComponent, EventCosmicSiphon>(OnCosmicSiphon);
        SubscribeLocalEvent<CosmicCultComponent, EventCosmicSiphonDoAfter>(OnCosmicSiphonDoAfter);
    }

    // Doesn't check for DivineIntervention, because there aren't any negative consequences for the target anymore
    private void OnCosmicSiphon(Entity<CosmicCultComponent> ent, ref EventCosmicSiphon args)
    {
        if (ent.Comp.EntropyLocked)
        {
            _popup.PopupClient(Loc.GetString("cosmicability-siphon-full"), ent, ent);
            return;
        }
        if (_cosmicCult.EntityIsCultist(args.Target) || TryComp<MobStateComponent>(args.Target, out var state) && state.CurrentState != MobState.Alive)
        {
            _popup.PopupClient(Loc.GetString("cosmicability-siphon-fail", ("target", Identity.Entity(args.Target, EntityManager))), ent, ent);
            return;
        }
        if (args.Handled)
            return;

        var doargs = new DoAfterArgs(EntityManager, ent, ent.Comp.CosmicSiphonDelay, new EventCosmicSiphonDoAfter(), ent, args.Target)
        {
            DistanceThreshold = 2.5f,
            Hidden = true,
            BreakOnHandChange = false,
            BreakOnDamage = false,
            BreakOnMove = false,
            BreakOnDropItem = false,
        };
        args.Handled = true;
        _doAfter.TryStartDoAfter(doargs);
    }

    protected abstract void OnCosmicSiphonDoAfter(Entity<CosmicCultComponent> ent, ref EventCosmicSiphonDoAfter args);
}
