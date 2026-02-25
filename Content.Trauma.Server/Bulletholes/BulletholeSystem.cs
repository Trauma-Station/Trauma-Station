using Content.Trauma.Common.Bulletholes;
using Content.Trauma.Shared.Weapons.Ranged;
using Content.Trauma.Shared.Weapons.Ranged.Ammo;
using Robust.Shared.Random;

namespace Content.Trauma.Server.Bulletholes;

public sealed class BulletholeSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    // Bullethole overlays
    private const int MaxBulletholeState = 10;
    private const int MaxBulletholeCount = 34;

    public override void Initialize()
    {
        SubscribeLocalEvent<BulletholeComponent, GotHitByProjectileEvent>(OnVisualsDamageChangedEvent);
    }

    private void OnVisualsDamageChangedEvent(Entity<BulletholeComponent> ent, ref GotHitByProjectileEvent args)
    {
        if (!HasComp<BulletholeGeneratorComponent>(args.Projectile)
            || !TryComp<AppearanceComponent>(ent, out var app))
            return;

        ent.Comp.BulletholeCount++;

        if (ent.Comp.BulletholeState < 1 || ent.Comp.BulletholeState > MaxBulletholeState)
            ent.Comp.BulletholeState = _random.Next(1, MaxBulletholeState + 1);

        var displayState = ent.Comp.BulletholeState;
        var displayCount = ent.Comp.BulletholeCount >= MaxBulletholeCount ? MaxBulletholeCount : ent.Comp.BulletholeCount;
        var stateString = $"bhole_{displayState}_{displayCount}";

        _appearance.SetData(ent, BulletholeVisuals.State, stateString, app);
    }
}
