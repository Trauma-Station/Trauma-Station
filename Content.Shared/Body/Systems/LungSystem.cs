// <Trauma>
using Content.Shared.Inventory;
using Robust.Shared.Containers;
// </Trauma>
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Inventory.Events;
using Robust.Shared.Prototypes;
using BreathToolComponent = Content.Shared.Atmos.Components.BreathToolComponent;
using InternalsComponent = Content.Shared.Body.Components.InternalsComponent;

namespace Content.Shared.Body.Systems;

public sealed partial class LungSystem : EntitySystem
{
    // <Trauma>
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    // </Trauma>
    [Dependency] private SharedAtmosphereSystem _atmos = default!;
    [Dependency] private SharedInternalsSystem _internals = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LungComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<BreathToolComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<BreathToolComponent, GotUnequippedEvent>(OnGotUnequipped);
    }

    private void OnGotUnequipped(Entity<BreathToolComponent> ent, ref GotUnequippedEvent args)
    {
        _atmos.DisconnectInternals(ent);
    }

    private void OnGotEquipped(Entity<BreathToolComponent> ent, ref GotEquippedEvent args)
    {
        if ((args.SlotFlags & ent.Comp.AllowedSlots) == 0)
        {
            return;
        }

        if (TryComp(args.EquipTarget, out InternalsComponent? internals))
        {
            ent.Comp.ConnectedInternalsEntity = args.EquipTarget;
            _internals.ConnectBreathTool((args.EquipTarget, internals), ent);
        }
    }

    private void OnMapInit(Entity<LungComponent> entity, ref MapInitEvent args)
    {
        _solutionContainerSystem.EnsureSolution(entity.Owner, entity.Comp.SolutionName, out var solution);

        solution.Comp.Solution.MaxVolume = 100.0f;
        solution.Comp.Solution.CanReact = false; // No dexalin lungs
    }

    // Goobstation - Update component state on component toggle TODO move this shit out
    [SubscribeLocalEvent]
    private void OnBreathToolInit(Entity<BreathToolComponent> ent, ref ComponentInit args)
    {
        var comp = ent.Comp;

        if (!_inventory.TryGetContainingSlot(ent.Owner, out var slot) ||
            !_container.TryGetContainingContainer(ent.Owner, out var container))
            return;

        if ((slot.SlotFlags & comp.AllowedSlots) == 0)
            return;

        var parent = container.Owner;
        if (TryComp(parent, out InternalsComponent? internals))
        {
            ent.Comp.ConnectedInternalsEntity = parent;
            _internals.ConnectBreathTool((parent, internals), ent);
        }
    }

    // TODO: JUST METABOLIZE GASES DIRECTLY DON'T CONVERT TO REAGENTS!!! (Needs Metabolism refactor :B)
    public void GasToReagent(EntityUid uid, LungComponent lung)
    {
        if (!_solutionContainerSystem.ResolveSolution(uid, lung.SolutionName, ref lung.Solution, out var solution))
            return;

        GasToReagent(lung.Air, solution);
        _solutionContainerSystem.UpdateChemicals(lung.Solution.Value);
    }

    /* This should really be moved to somewhere in the atmos system and modernized,
     so that other systems, like CondenserSystem, can use it.
     */
    private void GasToReagent(GasMixture gas, Solution solution)
    {
        foreach (var gasId in Enum.GetValues<Gas>())
        {
            var i = (int) gasId;
            var moles = gas[i];
            if (moles <= 0)
                continue;

            var reagent = _atmos.GasReagents[i];
            if (reagent is null)
                continue;

            var amount = moles * Atmospherics.BreathMolesToReagentMultiplier;
            amount = MathF.Min(amount, 15); // Goobstation - Prevent absurd amounts of reagent from being added. The maximum is arbitrary and as once wise Wizden contributor said Suck my Dick.
            solution.AddReagent(reagent, amount);
        }
    }

    public Solution GasToReagent(GasMixture gas)
    {
        var solution = new Solution();
        GasToReagent(gas, solution);
        return solution;
    }
}
