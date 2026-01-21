using Content.Server.Medical.Components;
using Content.Shared._Shitmed.Medical;
using Content.Shared._Shitmed.Medical.HealthAnalyzer;
using Content.Shared._Shitmed.Medical.Surgery.Wounds;
using Content.Shared._Shitmed.Medical.Surgery.Wounds.Components;
using Content.Shared._Shitmed.Medical.Surgery.Wounds.Systems;
using Content.Shared._Shitmed.Medical.Surgery.Pain.Components;
using Content.Shared._Shitmed.Medical.Surgery.Traumas;
using Content.Shared._Shitmed.Medical.Surgery.Traumas.Components;
using Content.Shared._Shitmed.Medical.Surgery.Traumas.Systems;
using Content.Shared._Shitmed.Targeting;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Damage.Components;
using Content.Shared.MedicalScanner;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
using System.Linq;

namespace Content.Server.Medical;

/// <summary>
/// Trauma - multi-modal health analyzer stuff
/// </summary>
public sealed partial class HealthAnalyzerSystem
{
    [Dependency] private readonly MobThresholdSystem _threshold = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly TraumaSystem _trauma = default!;
    [Dependency] private readonly WoundSystem _wound = default!;

    private EntityQuery<BodyComponent> _bodyQuery;
    private EntityQuery<BodyPartComponent> _partQuery;
    private EntityQuery<DamageableComponent> _damageQuery;

    private void InitializeTrauma()
    {
        _bodyQuery = GetEntityQuery<BodyComponent>();
        _partQuery = GetEntityQuery<BodyPartComponent>();
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
        if (args.BodyPart == null)
        {
            BeginAnalyzingEntity(healthAnalyzer, owner.Value, null);
        }
        else
        {
            var (targetType, targetSymmetry) = _body.ConvertTargetBodyPart(args.BodyPart.Value);
            if (_body.GetBodyChildrenOfType(owner.Value, targetType, symmetry: targetSymmetry) is { } part)
                BeginAnalyzingEntity(healthAnalyzer, owner.Value, part.FirstOrDefault().Id);
        }
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
        => Deleted(uid) || _partQuery.CompOrNull(uid.Value)?.Body == null;

    public HealthAnalyzerUiState GetHealthAnalyzerUiState(Entity<HealthAnalyzerComponent?> ent, EntityUid? target)
    {
        if (!Resolve(ent, ref ent.Comp))
            return new HealthAnalyzerUiState();

        return GetHealthAnalyzerUiState(target, ent.Comp.CurrentMode, ent.Comp.CurrentBodyPart);
    }

    private void FetchBodyData(EntityUid target,
        out Dictionary<NetEntity, List<WoundableTraumaData>> traumas,
        out Dictionary<NetEntity, FixedPoint2> pain,
        out Dictionary<TargetBodyPart, bool> bleeding)
    {
        traumas = new();
        pain = new();
        bleeding = new();

        if (!_bodyQuery.TryComp(target, out var body))
            return;

        if (body.RootContainer.ContainedEntity is not { } rootPart)
            return;

        foreach (var (woundable, component) in _wound.GetAllWoundableChildren(rootPart))
        {
            traumas.Add(GetNetEntity(woundable), FetchTraumaData(woundable, component));
            pain.Add(GetNetEntity(woundable), FetchPainData(woundable, component));
            bleeding.Add(_body.GetTargetBodyPart(woundable), component.Bleeds > 0);
        }
    }

    private Dictionary<TargetBodyPart, bool> FetchBleedData(Entity<BodyComponent?> body)
    {
        var bleeding = new Dictionary<TargetBodyPart, bool>();
        if (!Resolve(body, ref body.Comp, false))
            return bleeding;

        if (body.Comp.RootContainer.ContainedEntity is not { } rootPart)
            return bleeding;

        foreach (var (woundable, component) in _wound.GetAllWoundableChildren(rootPart))
            bleeding.Add(_body.GetTargetBodyPart(woundable), component.Bleeds > 0);

        return bleeding;
    }

    private List<WoundableTraumaData> FetchTraumaData(EntityUid target, WoundableComponent woundable)
    {
        var traumasList = new List<WoundableTraumaData>();

        if (_trauma.TryGetWoundableTrauma(target, out var traumasFound))
        {
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
        }

        return traumasList;
    }

    private FixedPoint2 FetchPainData(EntityUid target, WoundableComponent woundable)
    {
        var pain = FixedPoint2.Zero;

        if (!TryComp<NerveComponent>(target, out var nerve))
            return pain;

        return nerve.PainFeels;
    }

    private Dictionary<NetEntity, OrganTraumaData> FetchOrganData(EntityUid target)
    {
        var organs = new Dictionary<NetEntity, OrganTraumaData>();
        if (!_bodyQuery.TryComp(target, out var body))
            return organs;

        foreach (var (organId, organComp) in _body.GetBodyOrgans(target))
        {
            organs.Add(GetNetEntity(organId), new OrganTraumaData(organComp.OrganIntegrity,
                organComp.IntegrityCap,
                organComp.OrganSeverity,
                organComp.IntegrityModifiers
                    .Select(x => (x.Key.Item1, x.Value))
                    .ToList()));
        }

        return organs;
    }

    private Dictionary<NetEntity, Solution> FetchChemicalData(EntityUid target)
    {
        var solutionsList = new Dictionary<NetEntity, Solution>();

        if (!TryComp(target, out SolutionContainerManagerComponent? container) || container.Containers.Count == 0)
            return solutionsList;

        foreach (var (name, solution) in _solutionContainerSystem.EnumerateSolutions((target, container)))
        {
            if (name is null
                || name == BloodstreamComponent.DefaultBloodTemporarySolutionName
                // TODO SHITMED: what the fuck is this??
                || name == "print" // I hate this so fucking much.
                || !TryGetNetEntity(solution, out var netSolution))
                continue;

            solutionsList.Add(netSolution.Value, solution.Comp.Solution);
        }

        if (_bodyQuery.TryComp(target, out var body)
            && _body.TryGetBodyOrganEntityComps<StomachComponent>((target, body), out var stomachs))
        {
            foreach (var stomach in stomachs)
            {
                if (stomach.Comp1.Solution is null
                    || !TryGetNetEntity(stomach.Comp1.Solution, out var netSolution))
                    continue;

                solutionsList.Add(netSolution.Value, stomach.Comp1.Solution.Value.Comp.Solution); // This is horrible.
            }
        }

        return solutionsList;
    }
}
