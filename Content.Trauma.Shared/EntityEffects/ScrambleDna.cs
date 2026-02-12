// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.EntityEffects;
using Content.Shared.Humanoid;
using Content.Shared.Trigger.Systems;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Scrambles the target entity's DNA.
/// Does the same thing as the DNA scrambler implant etc.
/// </summary>
public sealed partial class ScrambleDna : EntityEffectBase<ScrambleDna>
{
    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-scramble-dna", ("chance", Probability));
}

// fuck you mocho
public sealed class ScrambleDnaEntityEffectSystem : EntityEffectSystem<HumanoidProfileComponent, ScrambleDna>
{
    [Dependency] private readonly DnaScrambleOnTriggerSystem _scramble = default!;

    protected override void Effect(Entity<HumanoidProfileComponent> ent, ref EntityEffectEvent<ScrambleDna> args)
    {
        _scramble.Scramble(ent, ent.Comp);
    }
}
