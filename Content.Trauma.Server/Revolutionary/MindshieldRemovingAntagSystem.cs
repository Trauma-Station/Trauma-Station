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

        if (HasComp<FakeMindShieldComponent>(uid))
            return;

        if (!TryComp<ImplantedComponent>(uid, out var implanted))
            return;

        var found = false;
        foreach (var implant in implanted.ImplantContainer.ContainedEntities)
        {
            if (!HasComp<MindShieldImplantComponent>(implant))
                continue;

            found = true;
            _subdermal.ForceRemove((uid, implanted), implant);
            break;
        }

        if (!found)
            return; // no free implant for randoms

        _subdermal.AddImplant(uid, ent.Comp.FakeMindShieldImplant);

        if (TryComp<FakeMindShieldComponent>(uid, out var fakeMindShield))
        {
            fakeMindShield.IsEnabled = true;
            Dirty(uid, fakeMindShield);
        }
    }
}
