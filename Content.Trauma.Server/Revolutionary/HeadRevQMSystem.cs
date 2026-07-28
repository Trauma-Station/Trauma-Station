// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Roles.Jobs;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Mindshield.Components;
using Content.Shared.Revolutionary.Components;

namespace Content.Server.Revolutionary;

/// <summary>
/// Handles the QM's ability to get a fake mindshield implant if they roll Head Rev.
/// </summary>
public sealed partial class HeadRevQMSystem : EntitySystem
{
    [Dependency] private JobSystem _jobSystem = default!;
    [Dependency] private SharedSubdermalImplantSystem _subdermal = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HeadRevolutionaryComponent, ComponentInit>(OnHeadRevInit);
    }

    private void OnHeadRevInit(EntityUid uid, HeadRevolutionaryComponent comp, ComponentInit args)
    {
        if (!TryComp<MindContainerComponent>(uid, out var mindContainer) ||
            !_jobSystem.MindTryGetJob(mindContainer.Mind, out var job) ||
            job.ID != "Quartermaster")
        {
            return;
        }

        if (TryComp<FakeMindShieldComponent>(uid, out var fakeMindShield) && fakeMindShield.IsEnabled)
            return;

        if (!TryComp<ImplantedComponent>(uid, out var implanted))
            return;

        foreach (var implant in implanted.ImplantContainer.ContainedEntities)
        {
            if (!HasComp<MindShieldImplantComponent>(implant))
                continue;

            _subdermal.ForceRemove((uid, implanted), implant);
            break;
        }

        _subdermal.AddImplant(uid, "FakeMindShieldImplant");

        if (TryComp<FakeMindShieldComponent>(uid, out fakeMindShield))
        {
            fakeMindShield.IsEnabled = true;
            Dirty(uid, fakeMindShield);
        }
    }
}
