// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Medical.Common.Body;
using Content.Medical.Shared.Wounds;
using Content.Server.Medical.Components;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Damage.Components;
using Content.Shared.FixedPoint;
using Content.Shared.MedicalScanner;
using Content.Shared.Mobs.Systems;
using Content.Trauma.Common.Medical.HealthAnalyzer;
using Robust.Shared.Prototypes;

namespace Content.Server.Medical;

/// <summary>
/// Trauma - multi-modal health analyzer stuff
/// </summary>
public sealed partial class HealthAnalyzerSystem
{
    [Dependency] private MobThresholdSystem _threshold = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private WoundSystem _wound = default!;

    // not using BuiEvents for these subs so it works for cryo pods too for free

    /// <summary>
    /// Handle the selection of a body part on the health analyzer
    /// </summary>
    /// <param name="healthAnalyzer">The health analyzer that's receiving the updates</param>
    /// <param name="args">The message containing the selected part</param>
    [SubscribeLocalEvent]
    private void OnHealthAnalyzerPartSelected(Entity<HealthAnalyzerComponent> healthAnalyzer, ref HealthAnalyzerPartMessage args)
    {
        if (healthAnalyzer.Comp.ScannedEntity is not { } target || !Exists(target))
            return;

        if (args.Category is not { } category)
            BeginAnalyzingEntity(healthAnalyzer, target, null);
        else if (_body.GetOrgan(target, category) is { } organ)
            BeginAnalyzingEntity(healthAnalyzer, target, organ);
    }

    // can't keep scanning a deleted or detached part
    private bool IsPartInvalid(EntityUid? uid)
        => Deleted(uid) || _body.GetBody(uid.Value) == null;

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

    public HealthAnalyzerUiState GetHealthAnalyzerUiState(Entity<HealthAnalyzerComponent?> ent, EntityUid? target)
    {
        if (!Resolve(ent, ref ent.Comp))
            return new HealthAnalyzerUiState();

        return GetHealthAnalyzerUiState(target, ent.Comp.CurrentBodyPart);
    }
}
