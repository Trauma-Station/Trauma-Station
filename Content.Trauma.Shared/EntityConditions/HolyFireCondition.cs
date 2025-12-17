using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Goobstation.Maths.FixedPoint;
using Content.Shared._Shitmed.Damage;
using Content.Shared._Shitmed.EntityEffects.Effects;
using Content.Shared._Shitmed.Targeting;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.Localizations;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityConditions;

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class HolyFire : EntityEffectBase<HolyFire>
{
    /// <summary>
    /// Damage to apply every cycle. Damage Ignores resistances.
    /// </summary>
    [DataField(required: true)]
    public float Stacks = default!;

    [DataField]
    public bool IgnoreResistances = true;

    // <Shitmed>
    /// <summary>
    /// How to scale the effect based on the temperature of the target entity.
    /// </summary>
    [DataField]
    public TemperatureScaling? ScaleByTemperature;

    [DataField]
    public SplitDamageBehavior SplitDamage = SplitDamageBehavior.SplitEnsureAllOrganic;

    [DataField]
    public bool UseTargeting = true;

    [DataField]
    public TargetBodyPart TargetPart = TargetBodyPart.All;

    [DataField]
    public bool IgnoreBlockers = true;
    // </Shitmed>

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var damages = new List<string>();
        var heals = false;
        var deals = false;

        var stacks = Stacks;

        // <Shitmed>
        /* Trauma - disabled until Temperature is networked in shared
        if (ScaleByTemperature.HasValue)
        {
            if (!args.EntityManager.TryGetComponent<TemperatureComponent>(args.TargetEntity, out var temp))
                scale = FixedPoint2.Zero;
            else
                scale *= ScaleByTemperature.Value.GetEfficiencyMultiplier(temp.CurrentTemperature, scale, false);
        }
        */
        // </Shitmed>

        var universalReagentDamageModifier = entSys.GetEntitySystem<Damage.Systems.DamageableSystem>().UniversalReagentDamageModifier;
        var universalReagentHealModifier = entSys.GetEntitySystem<Damage.Systems.DamageableSystem>().UniversalReagentHealModifier;

        var healsordeals = heals ? (deals ? "both" : "heals") : (deals ? "deals" : "none");

        return Loc.GetString("entity-effect-guidebook-health-change",
            ("chance", Probability),
            ("changes", ContentLocalizationManager.FormatList(damages)),
            ("healsordeals", healsordeals));
    }
}
