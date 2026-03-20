using Content.Medical.Common.Damage;
using Content.Medical.Common.Targeting;
using Content.Shared._Shitcode.Heretic.Curses;
using Content.Shared.Atmos.Components;
using Content.Shared.StatusEffectNew.Components;

namespace Content.Server._Shitcode.Heretic.Curses;

public sealed partial class HereticCurseSystem
{
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = Timing.CurTime;

        var corrosionQuery = EntityQueryEnumerator<CurseOfCorrosionStatusEffectComponent, StatusEffectComponent>();
        while (corrosionQuery.MoveNext(out _, out var corrosion, out var status))
        {
            if (corrosion.NextVomit > curTime || status.AppliedTo == null || status.EndEffectTime < curTime)
                continue;

            var next = _random.NextFloat(corrosion.MinMaxSecondsBetweenVomits.X,
                corrosion.MinMaxSecondsBetweenVomits.Y);

            corrosion.NextVomit = curTime + TimeSpan.FromSeconds(next);

            _dmg.TryChangeDamage(status.AppliedTo.Value,
                corrosion.Damage * next,
                true,
                targetPart: TargetBodyPart.All,
                splitDamage: SplitDamageBehavior.SplitEnsureAll);
            _vomit.Vomit(status.AppliedTo.Value);
        }

        var flammableQuery = GetEntityQuery<FlammableComponent>();

        var flamesQuery = EntityQueryEnumerator<CurseOfFlamesStatusEffectComponent, StatusEffectComponent>();
        while (flamesQuery.MoveNext(out _, out var flames, out var status))
        {
            if (flames.NextIgnition > curTime || status.AppliedTo is not { } target || status.EndEffectTime < curTime)
                continue;

            flames.NextIgnition = curTime + flames.Delay;

            if (!flammableQuery.TryComp(target, out var flam))
                continue;

            if (flam.FireStacks > flames.MinFireStacks &&
                flam.OnFire &&
                flam.FireProtectionPenetration >= flames.Penetration)
                continue;

            _flammable.SetFireStacks(target,
                MathF.Max(flames.MinFireStacks, flam.FireStacks),
                flam,
                true,
                flames.Penetration);
        }
    }
}
