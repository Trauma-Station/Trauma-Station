// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Sandevistan;
using Content.Shared.Damage.Systems;
using Content.Shared.Projectiles;
using Content.Trauma.Common.Weapons;
using Content.Trauma.Shared.Heretic.Components;
using Content.Trauma.Shared.Heretic.Components.Side;
using Content.Trauma.Shared.Heretic.Systems.Abilities;
using Content.Trauma.Shared.Heretic.Systems.PathSpecific.Blade;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Heretic.Systems.Side;

public abstract partial class SharedUnfathomableCurioSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SandevistanSystem _sande = default!;

    private const string SlowfieldFixtureId = "unfathomable-curio-slowfield";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UnfathomableCurioShieldComponent, BeforeHarmfulActionEvent>(OnBeforeHarmfulAction,
            after: [typeof(SharedHereticAbilitySystem), typeof(RiposteeSystem)]);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<UnfathomableCurioShieldComponent>();
        while (query.MoveNext(out var uid, out var shield))
        {
            if (shield.Active)
                continue;

            if (now < shield.ActivateTime)
                continue;

            shield.Active = true;
            shield.ActivateTime = now;
            Dirty(uid, shield);

            _audio.PlayPvs(shield.RechargeSound, uid);
            _sande.CreateSlowfieldFixture(uid, shield.SlowdownRadius, SlowfieldFixtureId);

            var comp = Factory.GetComponent<VelocityModifierContactsComponent>();
            comp.CollisionFixture = SlowfieldFixtureId;
            comp.Modifier = shield.BulletSlowdown;
            comp.Whitelist = shield.BulletWhitelist;
            AddComp(uid, comp, true);
        }
    }

    private void OnBeforeHarmfulAction(Entity<UnfathomableCurioShieldComponent> ent, ref BeforeHarmfulActionEvent args)
    {
        if (!ent.Comp.Active || args.Cancelled || args.Type != HarmfulActionType.Harm)
            return;

        args.Cancelled = true;
        ResetShield(ent, true, args.User);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<UnfathomableCurioShieldComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        ResetShield(ent, false, null);
    }

    [SubscribeLocalEvent]
    private void OnTakeDamage(Entity<UnfathomableCurioShieldComponent> ent, ref BeforeDamageChangedEvent args)
    {
        if (!ent.Comp.Active || args.Cancelled || args.Damage.GetTotal() < 5)
            return;

        args.Cancelled = true;
        ResetShield(ent, true, args.Origin);
    }

    [SubscribeLocalEvent]
    private void OnInit(Entity<UnfathomableCurioShieldComponent> ent, ref MapInitEvent args)
    {
        ResetShield(ent, false, null, false);
    }

    [SubscribeLocalEvent]
    private void OnPreventCollide(Entity<UnfathomableCurioShieldComponent> ent, ref PreventCollideEvent args)
    {
        if (!TryComp<FixturesComponent>(ent, out var fixtures)
            || !fixtures.Fixtures.TryGetValue(SlowfieldFixtureId, out var slowfieldFixture)
            || args.OurFixture != slowfieldFixture)
            return;

        if (!HasComp<ProjectileComponent>(args.OtherEntity))
            args.Cancelled = true;
    }

    private void ResetShield(Entity<UnfathomableCurioShieldComponent> ent, bool playSound, EntityUid? origin, bool resetDeactivateTime = true)
    {
        var now = _timing.CurTime;
        ent.Comp.Active = false;
        if (resetDeactivateTime)
            ent.Comp.DeactivateTime = now;
        ent.Comp.ActivateTime = now + ent.Comp.ActivateDelay;
        Dirty(ent);

        RemComp<VelocityModifierContactsComponent>(ent);
        _sande.DestroySlowfieldFixture(ent, SlowfieldFixtureId);

        if (!playSound)
            return;

        _audio.PlayPredicted(ent.Comp.BlockSound, ent, origin);
    }
}
