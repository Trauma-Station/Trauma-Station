using Content.Server.Medical.Components;
using Content.Shared._Shitmed.Medical.HealthAnalyzer;
using Content.Medical.Common.Body;
using Content.Medical.Common.Traumas;
using Content.Medical.Shared.Wounds;
using Content.Medical.Shared.Pain;
using Content.Medical.Shared.Traumas;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Damage.Components;
using Content.Shared.MedicalScanner;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Server.Medical;

/// <summary>
/// Trauma - multi-modal health analyzer stuff
/// </summary>
public sealed partial class HealthAnalyzerSystem
{
    [Dependency] private readonly MobThresholdSystem _threshold = default!;
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly CommonBodyPartSystem _part = default!;
    [Dependency] private readonly TraumaSystem _trauma = default!;
    [Dependency] private readonly WoundSystem _wound = default!;

    private EntityQuery<BodyComponent> _bodyQuery;
    private EntityQuery<DamageableComponent> _damageQuery;

    private void InitializeTrauma()
    {
        _bodyQuery = GetEntityQuery<BodyComponent>();
        _damageQuery = GetEntityQuery<DamageableComponent>();

        // not using BuiEvents so it works for cryo pods too for free
        SubscribeLocalEvent<HealthAnalyzerComponent, HealthAnalyzerPartMessage>(OnHealthAnalyzerPartSelected);
        SubscribeLocalEvent<HealthAnalyzerComponent, HealthAnalyzerModeSelectedMessage>(OnHealthAnalyzerModeSelected);
    }

    /// <summary>
    /// Handle the selection of a body part on the health analyzer
    /// </summary>
    /// <param name="healthAnalyzer">The health analyzer that's receiving the updates</param>
    /// <param name="args">The message containing the selected part</param>
    private void OnHealthAnalyzerPartSelected(Entity<HealthAnalyzerComponent> healthAnalyzer, ref HealthAnalyzerPartMessage args)
    {
        if (!TryGetEntity(args.Owner, out var owner))
            return;

        healthAnalyzer.Comp.CurrentMode = HealthAnalyzerMode.Body; // If you press a part ye get redirected bozo.
        if (args.Category is not {} category)
            BeginAnalyzingEntity(healthAnalyzer, owner.Value, null);
        else if (_body.GetOrgan(owner.Value, category) is {} organ)
            BeginAnalyzingEntity(healthAnalyzer, owner.Value, organ);
    }

    /// <summary>
    /// Handle the selection of a different health analyzer mode
    /// </summary>
    /// <param name="healthAnalyzer">The health analyzer that's receiving the updates</param>
    /// <param name="args">The message containing the selected mode</param>
    private void OnHealthAnalyzerModeSelected(Entity<HealthAnalyzerComponent> healthAnalyzer, ref HealthAnalyzerModeSelectedMessage args)
    {
        if (!TryGetEntity(args.Owner, out var owner))
            return;

        healthAnalyzer.Comp.CurrentMode = args.Mode; // If you press a part ye get redirected bozo.
        BeginAnalyzingEntity(healthAnalyzer, owner.Value);
    }

    // can't keep scanning a deleted or detached part
    private bool IsPartInvalid(EntityUid? uid)
        => Deleted(uid) || _body.GetBody(uid.Value) == null;

    public HealthAnalyzerUiState GetHealthAnalyzerUiState(Entity<HealthAnalyzerComponent?> ent, EntityUid? target)
    {
        if (!Resolve(ent, ref ent.Comp))
            return new HealthAnalyzerUiState();

        return GetHealthAnalyzerUiState(target, ent.Comp.CurrentMode, ent.Comp.CurrentBodyPart);
    }

    private void FetchBodyData(EntityUid target,
        out Dictionary<NetEntity, List<WoundableTraumaData>> traumas,
        out Dictionary<NetEntity, FixedPoint2> pain,
        out HashSet<ProtoId<OrganCategoryPrototype>> bleeding)
    {
        traumas = new();
        pain = new();
        bleeding = new();

        // TODO SHITMED: all of this shit should just be networked
        foreach (var part in _body.GetOrgans<WoundableComponent>(target))
        {
            var ent = GetNetEntity(part);
            traumas.Add(ent, FetchTraumaData(part, part.Comp));
            pain.Add(ent, FetchPainData(part));
            if (part.Comp.Bleeds > 0 && _body.GetCategory(part.Owner) is {} category)
                bleeding.Add(category);
        }
    }

    private HashSet<ProtoId<OrganCategoryPrototype>> FetchBleedData(Entity<BodyComponent?> body)
    {
        var bleeding = new HashSet<ProtoId<OrganCategoryPrototype>>();
        foreach (var part in _body.GetOrgans<WoundableComponent>(body))
        {
            if (part.Comp.Bleeds > 0 && _body.GetCategory(part.Owner) is {} category)
                bleeding.Add(category);
        }

        return bleeding;
    }

    private List<WoundableTraumaData> FetchTraumaData(EntityUid target, WoundableComponent woundable)
    {
        var traumasList = new List<WoundableTraumaData>();
        if (!_trauma.TryGetWoundableTrauma(target, out var traumasFound))
            return traumasList;

        foreach (var trauma in traumasFound)
        {
            if (trauma.Comp.TraumaType == TraumaType.BoneDamage
                && trauma.Comp.TraumaTarget is { } boneWoundable
                && TryComp(boneWoundable, out BoneComponent? boneComp))
            {
                traumasList.Add(new WoundableTraumaData(ToPrettyString(target),
                    trauma.Comp.TraumaType.ToString(), trauma.Comp.TraumaSeverity, boneComp.BoneSeverity.ToString(), trauma.Comp.TargetType));

                continue;
            }

            traumasList.Add(new WoundableTraumaData(ToPrettyString(trauma),
                    trauma.Comp.TraumaType.ToString(), trauma.Comp.TraumaSeverity, targetType: trauma.Comp.TargetType));
        }

        return traumasList;
    }

    private FixedPoint2 FetchPainData(EntityUid target)
        => CompOrNull<NerveComponent>(target)?.PainFeels ?? FixedPoint2.Zero;

    private Dictionary<NetEntity, OrganTraumaData> FetchOrganData(EntityUid target)
    {
        var organs = new Dictionary<NetEntity, OrganTraumaData>();
        if (!_bodyQuery.TryComp(target, out var body))
            return organs;

        foreach (var organ in _body.GetOrgans<InternalOrganComponent>(target))
        {
            organs.Add(GetNetEntity(organ), new OrganTraumaData(organ.Comp.OrganIntegrity,
                organ.Comp.IntegrityCap,
                organ.Comp.OrganSeverity,
                organ.Comp.IntegrityModifiers
                    .Select(x => (x.Key.Item1, x.Value))
                    .ToList()));
        }

        return organs;
    }

    private Dictionary<NetEntity, Solution> FetchChemicalData(EntityUid target)
    {
        var solutionsList = new Dictionary<NetEntity, Solution>();

        if (TryComp<BloodstreamComponent>(target, out var blood) &&
            _solutionContainerSystem.ResolveSolution(target, blood.BloodSolutionName, ref blood.BloodSolution, out var bloodSol))
        {
            solutionsList.Add(GetNetEntity(blood.BloodSolution.Value), bloodSol);
        }

        // TODO SHITMED: this is already networked????
        foreach (var stomach in _body.GetOrgans<StomachComponent>(target))
        {
            if (stomach.Comp.Solution is not {} solution)
                continue;

            solutionsList.Add(GetNetEntity(solution), solution.Comp.Solution);
        }

        return solutionsList;
    }
}
