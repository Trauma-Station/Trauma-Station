// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Weapons.DelayedKnockdown;
using Content.Goobstation.Shared.Disease.Components;
using Content.Medical.Shared.Body;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Stunnable;
using Content.Server.Temperature.Systems;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Mind.Components;
using Content.Shared.StatusEffect;
using Content.Shared.Temperature.Components;
using Content.Trauma.Server.Heretic.Abilities;
using Content.Trauma.Shared.Heretic.Components;
using Content.Trauma.Shared.Heretic.Components.Ghoul;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Rust;
using Content.Trauma.Shared.Heretic.Systems.Abilities;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.Heretic.Systems.PathSpecific;

public sealed class LeechingWalkSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly HereticAbilitySystem _ability = default!;
    [Dependency] private readonly RespiratorSystem _respirator = default!;
    [Dependency] private readonly DamageableSystem _dmg = default!;
    [Dependency] private readonly BodyRestoreSystem _bodyRestore = default!;
    [Dependency] private readonly BloodstreamSystem _blood = default!;
    [Dependency] private readonly TemperatureSystem _temperature = default!;
    [Dependency] private readonly SharedStaminaSystem _stam = default!;
    [Dependency] private readonly StunSystem _stun = default!;
    [Dependency] private readonly Content.Shared.StatusEffectNew.StatusEffectsSystem _statusNew = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly EntityQuery<DamageableComponent> _damageableQuery = default!;
    [Dependency] private readonly EntityQuery<TemperatureComponent> _temperatureQuery = default!;
    [Dependency] private readonly EntityQuery<StaminaComponent> _staminaQuery = default!;
    [Dependency] private readonly EntityQuery<StatusEffectsComponent> _statusQuery = default!;
    [Dependency] private readonly EntityQuery<RespiratorComponent> _respiratorQuery = default!;
    [Dependency] private readonly EntityQuery<HereticComponent> _hereticQuery = default!;
    [Dependency] private readonly EntityQuery<GhoulComponent> _ghoulQuery = default!;
    [Dependency] private readonly EntityQuery<BodyComponent> _bodyQuery = default!;
    [Dependency] private readonly EntityQuery<BloodstreamComponent> _bloodQuery = default!;

    private static readonly TimeSpan UpdateDelay = TimeSpan.FromSeconds(1);
    private TimeSpan _nextUpdate = TimeSpan.Zero;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LeechingWalkComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<LeechingWalkComponent> ent, ref MapInitEvent args)
    {
        RemCompDeferred<DiseaseCarrierComponent>(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;

        if (_nextUpdate > now)
            return;

        _nextUpdate = now + UpdateDelay;

        var leechQuery = EntityQueryEnumerator<LeechingWalkComponent, MindContainerComponent, TransformComponent>();
        while (leechQuery.MoveNext(out var uid, out var leech, out var mindContainer, out var xform))
        {
            if (!_ability.IsTileRust(xform.Coordinates, out _))
                continue;

            _damageableQuery.TryComp(uid, out var damageable);

            var multiplier = 1f;
            var shouldHeal = true;
            var boneHeal = FixedPoint2.Zero;
            if (_hereticQuery.TryComp(mindContainer.Mind, out var heretic))
            {
                multiplier += heretic.PassiveLevel * 0.5f;
                if (heretic is { Ascended: true, CurrentPath: HereticPath.Rust })
                {
                    if (_respiratorQuery.TryComp(uid, out var respirator))
                    {
                        _respirator.UpdateSaturation(uid,
                            respirator.MaxSaturation - respirator.MinSaturation,
                            respirator);
                    }

                    multiplier += 1.5f;
                }

                if (heretic.PassiveLevel >= 2)
                    boneHeal = leech.BoneHeal * heretic.PassiveLevel;

                if (heretic.PassiveLevel >= 3)
                {
                    if (damageable != null && _dmg.GetTotalDamage((uid, damageable)) < FixedPoint2.Epsilon)
                    {
                        if (_bodyQuery.TryComp(uid, out var body))
                            _bodyRestore.RestoreBody((uid, body));
                        shouldHeal = false;
                    }
                }
            }
            else if (_ghoulQuery.HasComp(uid))
                multiplier = 2f;

            RemCompDeferred<DelayedKnockdownComponent>(uid);

            var toHeal = -SharedHereticAbilitySystem.AllDamage * multiplier;

            if (shouldHeal && damageable != null)
            {
                _ability.IHateWoundMed((uid, damageable, null),
                    toHeal,
                    leech.BloodHeal * multiplier,
                    null,
                    boneHeal);
            }

            if (_bloodQuery.TryComp(uid, out var blood))
                _blood.FlushChemicals((uid, blood), leech.ChemPurgeRate * multiplier, leech.ExcludedReagents);

            if (_temperatureQuery.TryComp(uid, out var temperature))
                _temperature.ForceChangeTemperature(uid, leech.TargetTemperature, temperature);

            if (_staminaQuery.TryComp(uid, out var stamina) && stamina.StaminaDamage > 0)
            {
                _stam.TakeStaminaDamage(uid,
                    -float.Min(leech.StaminaHeal * multiplier, stamina.StaminaDamage),
                    stamina,
                    visual: false);
            }

            var reduction = leech.StunReduction * multiplier;
            _stun.TryAddStunDuration(uid, -reduction);
            _stun.AddKnockdownTime(uid, -reduction);

            _statusNew.TryRemoveStatusEffect(uid, leech.SleepStatus);
            _statusNew.TryRemoveStatusEffect(uid, leech.DrowsinessStatus);
            _statusNew.TryRemoveStatusEffect(uid, leech.RainbowStatus);

            if (_statusQuery.TryComp(uid, out var status))
            {
                _status.TryRemoveStatusEffect(uid, "BlurryVision", status);
                _status.TryRemoveStatusEffect(uid, "TemporaryBlindness", status);
            }
        }
    }

}
