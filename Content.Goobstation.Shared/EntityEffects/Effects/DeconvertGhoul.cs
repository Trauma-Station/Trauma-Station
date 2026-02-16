using Content.Shared._Shitcode.Heretic.Components;
using Content.Shared.EntityEffects;
using Content.Shared.Heretic;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.EntityEffects.Effects;

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
