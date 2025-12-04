// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Revert the target entity's polymorph.
/// </summary>
public sealed partial class RevertPolymorph : EntityEffectBase<RevertPolymorph>
{
    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-revert-polymorph", ("chance", Probability));
}
