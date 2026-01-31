// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Toggles a component from the target.
/// Removes it if present, adds if missing.
/// </summary>
public sealed partial class ToggleComponent : EntityEffectBase<ToggleComponent>
{
    /// <summary>
    /// Name of the component to toggle.
    /// </summary>
    [DataField(required: true)]
    public string CompName = string.Empty;

    /// <summary>
    /// Cached type for the component.
    /// </summary>
    internal Type? Comp;

    /// <summary>
    /// Text to use for the guidebook entry for reagents.
    /// </summary>
    [DataField]
    public LocId? GuidebookText;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => GuidebookText is {} loc ? Loc.GetString(loc, ("chance", Probability)) : null;
}

public sealed class ToggleComponentEffectSystem : EntityEffectSystem<MetaDataComponent, ToggleComponent>
{
    protected override void Effect(Entity<MetaDataComponent> ent, ref EntityEffectEvent<ToggleComponent> args)
    {
        var effect = args.Effect;
        if (effect.Comp == null)
        {
            var reg = Factory.GetRegistration(effect.CompName);
            effect.Comp = reg.Type;
        }
        var type = effect.Comp;
        if (HasComp(ent, type))
            RemComp(ent, type);
        else
            AddComp(ent, Factory.GetComponent(type));
    }
}
