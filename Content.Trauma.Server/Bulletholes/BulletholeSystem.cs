using System.Numerics;
using Content.Trauma.Common.Bulletholes;
using Content.Trauma.Shared.Weapons.Ranged;
using Content.Trauma.Shared.Weapons.Ranged.Ammo;
using Robust.Shared.Random;

namespace Content.Trauma.Server.Bulletholes;

/// <summary>
/// Handles giving bullet holes a position and sending it to the client
/// </summary>
public sealed class BulletholeSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;


    public override void Initialize()
    {
        SubscribeLocalEvent<BulletholeComponent, GotHitByProjectileEvent>(OnHit);
    }

    private void OnHit(Entity<BulletholeComponent> ent, ref GotHitByProjectileEvent args)
    {
        if (!HasComp<BulletholeGeneratorComponent>(args.Projectile))
            return;

        if (ent.Comp.HolePositions.Count >= BulletholeComponent.MaxHoles)
            return;

        var offset = new Vector2(
            _random.NextFloat() * 0.8f + 0.1f,
            _random.NextFloat() * 0.8f + 0.1f);

        ent.Comp.HolePositions.Add(offset);
        Dirty(ent);
    }
}
