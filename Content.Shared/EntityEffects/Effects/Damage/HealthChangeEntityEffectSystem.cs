// <Trauma>
using Content.Medical.Common.Damage;
using Content.Medical.Common.EntityEffects;
using Content.Medical.Common.Targeting;
using Content.Shared.Heretic;
using Content.Shared.Temperature.Components;
// </Trauma>
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Localizations;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Damage;

/// <summary>
/// Adjust the damages on this entity by specified amounts.
/// Amounts are modified by scale.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class HealthChangeEntityEffectSystem : EntityEffectSystem<DamageableComponent, HealthChange>
{
    [Dependency] private readonly DamageableSystem _damageable = default!;

    protected override void Effect(Entity<DamageableComponent> entity, ref EntityEffectEvent<HealthChange> args)
    {
        var damageSpec = new DamageSpecifier(args.Effect.Damage);

        damageSpec *= args.Scale;

        // <Goob>
        if (args.Effect.ScaleByTemperature is {} scaleTemp)
        {
            damageSpec *= TryComp<TemperatureComponent>(entity, out var temp)
                ? scaleTemp.GetEfficiencyMultiplier(temp.CurrentTemperature, args.Scale, false)
                : FixedPoint2.Zero;
        }

        // heretics can heal instead of being poisoned
        var ev = new ImmuneToPoisonDamageEvent();
        RaiseLocalEvent(entity, ref ev);
        if (ev.Immune)
        {
            damageSpec = DamageSpecifier.GetNegative(damageSpec);
            if (damageSpec.GetTotal() == FixedPoint2.Zero)
                return;
        }
        // </Goob>

        _damageable.TryChangeDamage(
                entity.AsNullable(),
                damageSpec,
                args.Effect.IgnoreResistances,
                interruptsDoAfters: false,
                targetPart: args.Effect.UseTargeting ? args.Effect.TargetPart : null,
                ignoreBlockers: args.Effect.IgnoreBlockers,
                splitDamage: args.Effect.SplitDamage,
                increaseOnly: args.Effect.IncreaseOnly); // Trauma
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class HealthChange : EntityEffectBase<HealthChange>
{
    /// <summary>
    /// Damage to apply every cycle. Damage Ignores resistances.
    /// </summary>
    [DataField(required: true)]
    public DamageSpecifier Damage = default!;

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

    [DataField]
    public bool IncreaseOnly;
    // </Trauma>

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        {
            var damages = new List<string>();
            var heals = false;
            var deals = false;

            var damageSpec = new DamageSpecifier(Damage);

            var universalReagentDamageModifier = entSys.GetEntitySystem<DamageableSystem>().UniversalReagentDamageModifier;
            var universalReagentHealModifier = entSys.GetEntitySystem<DamageableSystem>().UniversalReagentHealModifier;

            damageSpec = entSys.GetEntitySystem<DamageableSystem>().ApplyUniversalAllModifiers(damageSpec);

            foreach (var (kind, amount) in damageSpec.DamageDict)
            {
                var sign = FixedPoint2.Sign(amount);
                float mod;

                switch (sign)
                {
                    case < 0:
                        heals = true;
                        mod = universalReagentHealModifier;
                        break;
                    case > 0:
                        deals = true;
                        mod = universalReagentDamageModifier;
                        break;
                    default:
                        continue; // Don't need to show damage types of 0...
                }

                damages.Add(
                    Loc.GetString("health-change-display",
                        ("kind", prototype.Index<DamageTypePrototype>(kind).LocalizedName),
                        ("amount", MathF.Abs(amount.Float() * mod)),
                        ("deltasign", sign)
                    ));
            }

            var healsordeals = heals ? (deals ? "both" : "heals") : (deals ? "deals" : "none");

            return Loc.GetString("entity-effect-guidebook-health-change",
                ("chance", Probability),
                ("changes", ContentLocalizationManager.FormatList(damages)),
                ("healsordeals", healsordeals));
        }
}
