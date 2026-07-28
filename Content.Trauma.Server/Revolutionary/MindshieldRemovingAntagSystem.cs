// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Antag;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Mindshield.Components;

namespace Content.Trauma.Server.Revolutionary;

/// <summary>
/// Handles removing a real mindshield and replacing it with a fake one for antags who start with a real mindshield
/// </summary>
public sealed partial class MindshieldRemovingAntagSystem : EntitySystem
{
    [Dependency] private SharedSubdermalImplantSystem _subdermal = default!;

    [SubscribeLocalEvent]
    private void OnAntagSelected(Entity<MindshieldRemovingAntagComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        var uid = args.EntityUid;

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

        _subdermal.AddImplant(uid, ent.Comp.FakeMindShieldImplant);

        if (TryComp<FakeMindShieldComponent>(uid, out fakeMindShield))
        {
            fakeMindShield.IsEnabled = true;
            Dirty(uid, fakeMindShield);
        }
    }
}
