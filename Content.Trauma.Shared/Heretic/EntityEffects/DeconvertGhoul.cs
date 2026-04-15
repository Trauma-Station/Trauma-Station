// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Trauma.Shared.Heretic.Components.Ghoul;

namespace Content.Trauma.Shared.Heretic.EntityEffects;

/// <summary>
/// Deconverts ghoulified person
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T, TEffect}"/>
public sealed class DeconvertGhoulEntityEffectSystem : EntityEffectSystem<MetaDataComponent, DeconvertGhoul>
{
    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<DeconvertGhoul> args)
    {
        if (!TryComp(entity, out GhoulComponent? ghoul) || !ghoul.CanDeconvert)
            return;

        EnsureComp<GhoulDeconvertComponent>(entity);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class DeconvertGhoul : EntityEffectBase<DeconvertGhoul>
{
    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("reagent-effect-guidebook-deconvert-ghoul");
    }
}
