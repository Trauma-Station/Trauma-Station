// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Goobstation.Shared.Chemistry.GunApplySolution;

public sealed partial class GunApplySolutionSystem : EntitySystem
{
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GunApplySolutionComponent, GunShotEvent>(OnGunShot);
    }

    private void OnGunShot(EntityUid uid, GunApplySolutionComponent comp, ref GunShotEvent args)
    {
        if (!_solutionContainer.TryGetSolution(uid, comp.SourceSolution, out var ent, out var source))
            return;

        foreach (var (ammo, _) in args.Ammo) // This gives wrong uid on client
        {
            if (ammo == null)
                continue;

            if (!_solutionContainer.TryGetSolution(ammo.Value, comp.TargetSolution, out var target, out _))
                continue;

            _solutionContainer.TryTransferSolution(target.Value, source, comp.Amount);
        }

        _solutionContainer.UpdateChemicals(ent.Value);  // So we call this
    }
}
