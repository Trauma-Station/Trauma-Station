// <Trauma>
using Content.Medical.Common.Body;
using Content.Shared.Body;
using System.Linq;
// </Trauma>
using Content.Shared.Clothing.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared.Armor;

/// <summary>
///     This handles logic relating to <see cref="ArmorComponent" />
/// </summary>
public abstract class SharedArmorSystem : EntitySystem
{
    // <Trauma>
    [Dependency] private readonly BodySystem _body = default!;
    // </Trauma>
    [Dependency] private readonly ExamineSystemShared _examine = default!;

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArmorComponent, InventoryRelayedEvent<CoefficientQueryEvent>>(OnCoefficientQuery);
        SubscribeLocalEvent<ArmorComponent, InventoryRelayedEvent<DamageModifyEvent>>(OnDamageModify);
        SubscribeLocalEvent<ArmorComponent, BorgModuleRelayedEvent<DamageModifyEvent>>(OnBorgDamageModify);
        SubscribeLocalEvent<ArmorComponent, GetVerbsEvent<ExamineVerb>>(OnArmorVerbExamine);
    }

    /// <summary>
    /// Get the total Damage reduction value of all equipment caught by the relay.
    /// </summary>
    /// <param name="ent">The item that's being relayed to</param>
    /// <param name="args">The event, contains the running count of armor percentage as a coefficient</param>
    private void OnCoefficientQuery(Entity<ArmorComponent> ent, ref InventoryRelayedEvent<CoefficientQueryEvent> args)
    {
        if (TryComp<MaskComponent>(ent, out var mask) && mask.IsToggled)
            return;

        foreach (var armorCoefficient in ent.Comp.Modifiers.Coefficients)
        {
            args.Args.DamageModifiers.Coefficients[armorCoefficient.Key] = args.Args.DamageModifiers.Coefficients.TryGetValue(armorCoefficient.Key, out var coefficient) ? coefficient * armorCoefficient.Value : armorCoefficient.Value;
        }
    }

    private void OnDamageModify(EntityUid uid, ArmorComponent component, InventoryRelayedEvent<DamageModifyEvent> args)
    {
        if (TryComp<MaskComponent>(uid, out var mask) && mask.IsToggled)
            return;

        // <Goob>
        if (args.Args.TargetPart is not {} partType)
            return;

        if (component.ArmorCoverage.Contains(partType))
            args.Args.Damage = DamageSpecifier.ApplyModifierSet(args.Args.Damage,
            DamageSpecifier.PenetrateArmor(component.Modifiers, args.Args.Damage.ArmorPenetration));
        // </Goob>
    }

    private void OnBorgDamageModify(EntityUid uid, ArmorComponent component,
        ref BorgModuleRelayedEvent<DamageModifyEvent> args)
    {
        if (TryComp<MaskComponent>(uid, out var mask) && mask.IsToggled)
            return;

        args.Args.Damage = DamageSpecifier.ApplyModifierSet(args.Args.Damage,
            DamageSpecifier.PenetrateArmor(component.Modifiers, args.Args.Damage.ArmorPenetration)); // Goob edit
    }

    private void OnArmorVerbExamine(EntityUid uid, ArmorComponent component, GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess || !component.ShowArmorOnExamine)
            return;

        // Shitmed Change Start
        if (component is { ArmourCoverageHidden: true, ArmourModifiersHidden: true })
            return;

        var examineMarkup = GetArmorExamine(component);
        // Shitmed Change End
        var ev = new ArmorExamineEvent(examineMarkup);
        RaiseLocalEvent(uid, ref ev);

        _examine.AddDetailedExamineVerb(args, component, examineMarkup,
            Loc.GetString("armor-examinable-verb-text"), "/Textures/Interface/VerbIcons/dot.svg.192dpi.png",
            Loc.GetString("armor-examinable-verb-message"));
    }

    // Shitmed Change: Mostly changed.
    private FormattedMessage GetArmorExamine(ArmorComponent component)
    {
        var msg = new FormattedMessage();
        msg.AddMarkupOrThrow(Loc.GetString("armor-examine"));

        if (!component.Modifiers.Coefficients.Any() && !component.Modifiers.FlatReduction.Any())
            return msg;

        var coverage = component.ArmorCoverage;
        var armorModifiers = component.Modifiers;

        if (!component.ArmourCoverageHidden)
        {
            foreach (var coveragePart in coverage.Where(coveragePart => coveragePart != BodyPartType.Other))
            {
                msg.PushNewline();

                var bodyPartType = Loc.GetString("armor-coverage-type-" + coveragePart.ToString().ToLower());
                msg.AddMarkupOrThrow(Loc.GetString("armor-coverage-value", ("type", bodyPartType)));
            }
        }

        if (!component.ArmourModifiersHidden)
        {
            foreach (var coefficientArmor in armorModifiers.Coefficients)
            {
                msg.PushNewline();
                var armorType = Loc.GetString("armor-damage-type-" + coefficientArmor.Key.ToLower());
                msg.AddMarkupOrThrow(Loc.GetString("armor-coefficient-value",
                    ("type", armorType),
                    ("value", MathF.Round((1f - coefficientArmor.Value) * 100, 1))
                ));
            }

            foreach (var flatArmor in armorModifiers.FlatReduction)
            {
                msg.PushNewline();

                var armorType = Loc.GetString("armor-damage-type-" + flatArmor.Key.ToLower());
                msg.AddMarkupOrThrow(Loc.GetString("armor-reduction-value",
                    ("type", armorType),
                    ("value", flatArmor.Value)
                ));
            }
        }

        return msg;
    }
}
