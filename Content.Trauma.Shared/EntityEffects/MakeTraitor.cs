// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Makes the target entity a traitor, if it has a player controlling it.
/// </summary>
public sealed partial class MakeTraitor : EntityEffectBase<MakeTraitor>
{
    [DataField]
    public EntProtoId Rule = "Traitor";

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-make-traitor", ("chance", Probability));
}
