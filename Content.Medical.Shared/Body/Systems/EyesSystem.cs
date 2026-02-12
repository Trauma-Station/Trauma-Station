// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Medical.Common.Body;
using Content.Medical.Common.Traumas;
using Content.Medical.Shared.Traumas;
using Content.Shared.Body;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Medical.Shared.Body;

public sealed class EyesSystem : EntitySystem
{
    [Dependency] private readonly BlindableSystem _blindable = default!;
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly TraumaSystem _trauma = default!;

    public static readonly ProtoId<OrganCategoryPrototype> EyesCategory = "Eyes";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EyesComponent, OrganIntegrityChangedEvent>(OnOrganIntegrityChanged);
        SubscribeLocalEvent<EyesComponent, OrganEnabledEvent>(OnOrganEnabled);
        SubscribeLocalEvent<EyesComponent, OrganDisabledEvent>(OnOrganDisabled);
        SubscribeLocalEvent<EyesComponent, BodyRelayedEvent<EyesDamagedEvent>>(OnEyesDamaged);
    }

    // Too much shit would break if I were to nuke blindablecomponent rn. Guess we shitcoding this one.
    private void OnOrganIntegrityChanged(Entity<EyesComponent> ent, ref OrganIntegrityChangedEvent args)
    {
        if (args.NewIntegrity <= 0 ||
            _body.GetBody(ent.Owner) is not {} body ||
            !TryComp<InternalOrganComponent>(ent, out var organ) ||
            !TryComp<BlindableComponent>(body, out var blindable) ||
            organ.OrganIntegrity <= 0) // eyes should be disabled for blinding
            return;

        _blindable.SetEyeDamage((body, blindable), (int) organ.OrganIntegrity);
    }

    private void OnOrganEnabled(Entity<EyesComponent> ent, ref OrganEnabledEvent args)
    {
        if (!TryComp<InternalOrganComponent>(ent, out var organ) ||
            !TryComp(args.Body, out BlindableComponent? blindable))
            return;

        // We add the current eye damage since in any context, the organ being enabled means that it was
        // either removed or disabled, so the BlindableComponent must have some prior damage already.
        var adjustment = (int)(organ.IntegrityCap - organ.OrganIntegrity);
        _blindable.SetEyeDamage((args.Body, blindable), adjustment);
    }

    private void OnOrganDisabled(Entity<EyesComponent> ent, ref OrganDisabledEvent args)
    {
        if (TerminatingOrDeleted(args.Body))
            return; // can't see anyway if you are being deleted

        CheckMissingEyes(args.Body);
    }

    private void OnEyesDamaged(Entity<EyesComponent> ent, ref BodyRelayedEvent<EyesDamagedEvent> args)
    {
        _trauma.TryCreateOrganDamageModifier(ent.Owner, args.Args.Damage, args.Args.Body, "BlindableDamage");
    }

    private void CheckMissingEyes(EntityUid body)
    {
        if (TerminatingOrDeleted(body))
            return;

        if (_body.GetOrgan(body, EyesCategory) != null)
            return;

        // can't see without eyes
        if (TryComp<BlindableComponent>(body, out var blindable))
            _blindable.SetEyeDamage((body, blindable), blindable.MaxDamage);
    }
}
