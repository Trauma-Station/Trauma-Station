// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Medical.Common.Body;
using Content.Medical.Common.DoAfter;
using Content.Medical.Shared.Body;
using Content.Shared.Body;
using Content.Shared.Damage.Systems;
using Content.Shared.Emp;

namespace Content.Medical.Shared.Cybernetics;

public sealed class CyberneticsSystem : EntitySystem
{
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly BodyPartSystem _part = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CyberneticsComponent, EmpPulseEvent>(OnEmpPulse);
        SubscribeLocalEvent<CyberneticsComponent, EmpDisabledRemovedEvent>(OnEmpDisabledRemoved);
        SubscribeLocalEvent<CyberneticsComponent, OrganEnableAttemptEvent>(OnEnableAttempt);
        SubscribeLocalEvent<CyberneticsComponent, BodyRelayedEvent<ModifyDoAfterDelayEvent>>(OnModifyDoAfter);
    }

    private void OnEmpPulse(Entity<CyberneticsComponent> ent, ref EmpPulseEvent ev)
    {
        if (ent.Comp.Disabled)
            return;

        ev.Affected = true;
        ev.Disabled = true;
        ent.Comp.Disabled = true;
        Dirty(ent);

        if (_body.GetBody(ent.Owner) is not {} body)
            return;

        _body.DisableOrgan(ent.Owner);
        var target = _part.GetOuterOrgan(ent.Owner);
        _damageable.ChangeDamage(target, ent.Comp.EmpDamage, increaseOnly: true);
    }

    private void OnEmpDisabledRemoved(Entity<CyberneticsComponent> ent, ref EmpDisabledRemovedEvent ev)
    {
        if (!ent.Comp.Disabled)
            return;

        ent.Comp.Disabled = false;
        Dirty(ent);

        _body.EnableOrgan(ent.Owner);
    }

    private void OnEnableAttempt(Entity<CyberneticsComponent> ent, ref OrganEnableAttemptEvent args)
    {
        // prevent enabling the organ while emped
        args.Cancelled |= ent.Comp.Disabled;
    }

    private void OnModifyDoAfter(Entity<CyberneticsComponent> ent, ref BodyRelayedEvent<ModifyDoAfterDelayEvent> args)
    {
        if (ent.Comp.Disabled)
            args.Args.Multiplier *= 10;
    }
}
