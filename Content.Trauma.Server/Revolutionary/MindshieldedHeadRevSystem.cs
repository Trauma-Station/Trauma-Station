using Content.Server.Antag;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Mindshield.Components;

namespace Content.Server.Revolutionary;

/// <summary>
/// Handles putting fake mindshield implants into headrevs that start with a real one
/// </summary>
public sealed partial class MindshieldedHeadRevSystem : EntitySystem
{
    [Dependency] private SharedSubdermalImplantSystem _subdermal = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MindshieldedHeadRevComponent, AfterAntagEntitySelectedEvent>(OnAntagSelected);
    }

    private void OnAntagSelected(Entity<MindshieldedHeadRevComponent> ent, ref AfterAntagEntitySelectedEvent args)
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