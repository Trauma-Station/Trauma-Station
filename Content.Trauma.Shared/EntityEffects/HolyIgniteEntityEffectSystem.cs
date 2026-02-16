// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.EntityEffects;
using Content.Trauma.Shared.Chaplain;
using Content.Trauma.Shared.Chaplain.Components;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class HolyIgnite : EntityEffectBase<HolyIgnite>
{
    /// <summary>
    ///     Amount of FireStacks improved.
    /// </summary>
    [DataField(required: true)]
    public float Stacks;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("entity-effect-guidebook-extinguish-reaction", ("chance", Probability));
}
