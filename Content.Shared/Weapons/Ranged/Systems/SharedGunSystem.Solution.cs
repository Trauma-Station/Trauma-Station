// <Trauma>
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
// </Trauma>
using Content.Shared.Chemistry.Components;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Map;

namespace Content.Shared.Weapons.Ranged.Systems;

public partial class SharedGunSystem
{
    protected virtual void InitializeSolution()
    {
        SubscribeLocalEvent<SolutionAmmoProviderComponent, TakeAmmoEvent>(OnSolutionTakeAmmo);
        SubscribeLocalEvent<SolutionAmmoProviderComponent, GetAmmoCountEvent>(OnSolutionAmmoCount);
    }

    [Dependency] private SharedSolutionContainerSystem _solution = default!; // Trauma

    private void OnSolutionTakeAmmo(Entity<SolutionAmmoProviderComponent> ent, ref TakeAmmoEvent args)
    {
        var shots = Math.Min(args.Shots, ent.Comp.Shots);

        // Don't dirty if it's an empty fire.
        if (shots == 0)
            return;

        for (var i = 0; i < shots; i++)
        {
            args.Ammo.Add(GetSolutionShot(ent, args.Coordinates));
            ent.Comp.Shots--;
        }

        // <Trauma>
        // Replaced 20 year old shitcode with this so it actually consumes reagents when firing
        // Yes, it was just two lines all this time. No need to go through 5 different server systems
        if (TryComp<SolutionComponent>(ent, out var solution))
        {
            _solution.RemoveEachReagent((ent.Owner, solution), ent.Comp.FireCost);
        }
        // </Trauma>

        UpdateSolutionShots(ent);
        UpdateSolutionAppearance(ent);
    }

    private void OnSolutionAmmoCount(Entity<SolutionAmmoProviderComponent> ent, ref GetAmmoCountEvent args)
    {
        args.Count = ent.Comp.Shots;
        args.Capacity = ent.Comp.MaxShots;
    }

    protected virtual void UpdateSolutionShots(Entity<SolutionAmmoProviderComponent> ent, Solution? solution = null) { }

    protected virtual (EntityUid Entity, IShootable) GetSolutionShot(Entity<SolutionAmmoProviderComponent> ent, EntityCoordinates position)
    {
        var shot = PredictedSpawnAtPosition(ent.Comp.Prototype, position); // Trauma - predict this shit
        return (shot, EnsureShootable(shot));
    }

    protected void UpdateSolutionAppearance(Entity<SolutionAmmoProviderComponent> ent)
    {
        if (!Timing.IsFirstTimePredicted) return; // Trauma - fix visual mispredict
        if (!TryComp<AppearanceComponent>(ent, out var appearance))
            return;

        Appearance.SetData(ent, AmmoVisuals.HasAmmo, ent.Comp.Shots != 0, appearance);
        Appearance.SetData(ent, AmmoVisuals.IsFull, ent.Comp.Shots == ent.Comp.MaxShots, appearance);
        Appearance.SetData(ent, AmmoVisuals.AmmoCount, ent.Comp.Shots, appearance);
        Appearance.SetData(ent, AmmoVisuals.AmmoMax, ent.Comp.MaxShots, appearance);
    }
}
