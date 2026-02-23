using Content.Shared.Damage.Systems;
using Content.Trauma.Shared.Weapons.Ranged.Ammo;
using Robust.Shared.Random;

namespace Content.Trauma.Shared.Weapons.Ranged;

public sealed class BulletholeSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    // Bullethole overlays
    private const int MaxBulletholeState = 10;
    private const int MaxBulletholeCount = 24;

    public override void Initialize()
    {
        SubscribeLocalEvent<BulletholeComponent, DamageChangedEvent>(OnVisualsDamageChangedEvent);
    }

    private void OnVisualsDamageChangedEvent(Entity<BulletholeComponent> ent, ref DamageChangedEvent args)
    {
        if (!TryComp(args.Origin, out BulletholeGeneratorComponent? bulletholeGeneratorComponent))
            return;

        ent.Comp.BulletholeCount++;

        if (!TryComp<AppearanceComponent>(ent, out var app))
            return;

        if (ent.Comp.BulletholeState < 1 || ent.Comp.BulletholeState > MaxBulletholeState)
            ent.Comp.BulletholeState = _random.Next(1, MaxBulletholeState + 1);

        var displayState = ent.Comp.BulletholeState;
        var displayCount = ent.Comp.BulletholeCount >= MaxBulletholeCount ? MaxBulletholeCount : ent.Comp.BulletholeCount;
        var stateString = $"bhole_{displayState}_{displayCount}";

        _appearance.SetData(ent, BulletholeVisuals.State, stateString, app);
    }
}
