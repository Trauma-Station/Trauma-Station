// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.FixedPoint;
using Content.Goobstation.Shared.Changeling.Components;
using Content.Medical.Common.Damage;
using Content.Medical.Common.Targeting;
using Content.Shared.Atmos.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Damage.Systems;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Goobstation.Shared.Changeling.Systems;

public sealed partial class FleshmendSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedBloodstreamSystem _bloodstream = default!;
    [Dependency] private DamageableSystem _dmg = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FleshmendComponent, StatusEffectAppliedEvent>(OnApplied);
        SubscribeLocalEvent<FleshmendComponent, StatusEffectRemovedEvent>(OnRemoved);
    }

    private void OnApplied(Entity<FleshmendComponent> ent, ref StatusEffectAppliedEvent args)
    {
        if (ent.Comp.DoVisualEffect)
            EnsureComp<FleshmendEffectComponent>(args.Target);

        if (ent.Comp.PassiveSound != null)
            DoFleshmendSound(ent);

        ent.Comp.UpdateTimer = _timing.CurTime + ent.Comp.UpdateDelay;

        Cycle(ent, args.Target);
    }

    private void OnRemoved(Entity<FleshmendComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (ent.Comp.DoVisualEffect)
            RemComp<FleshmendEffectComponent>(args.Target);

        if (ent.Comp.PassiveSound != null)
            StopFleshmendSound(ent);
    }

    private void DoFleshmendSound(Entity<FleshmendComponent> ent)
    {
        var audioParams = AudioParams.Default.WithLoop(true).WithVolume(-3f);
        var source = _audio.PlayPvs(ent.Comp.PassiveSound, ent, audioParams);
        ent.Comp.SoundSource = source?.Entity;
    }

    private void StopFleshmendSound(Entity<FleshmendComponent> ent)
    {
        _audio.Stop(ent.Comp.SoundSource);
        ent.Comp.SoundSource = null;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        var query = EntityQueryEnumerator<FleshmendComponent, StatusEffectComponent>();
        var now = _timing.CurTime;
        while (query.MoveNext(out var uid, out var comp, out var effect))
        {
            if (effect.AppliedTo is not { } target || comp.UpdateTimer < now)
                continue;

            comp.UpdateTimer = now + comp.UpdateDelay;

            Cycle((uid, comp), target);
        }
    }

    private void Cycle(Entity<FleshmendComponent> ent, EntityUid target)
    {
        if (!TryFlammableChecks(ent, target))
            return;

        DoFleshmend(ent, target);
    }

    private bool TryFlammableChecks(Entity<FleshmendComponent> ent, EntityUid target)
    {
        if (TryComp<FlammableComponent>(target, out var flam)
            && flam.OnFire
            && !ent.Comp.IgnoreFire)
        {
            if (ent.Comp.DoVisualEffect)
                RemComp<FleshmendEffectComponent>(target);

            if (ent.Comp.PassiveSound != null)
                StopFleshmendSound(ent);

            return false;
        }

        if (ent.Comp.DoVisualEffect)
            EnsureComp<FleshmendEffectComponent>(target);

        if (ent.Comp.PassiveSound != null
            && ent.Comp.SoundSource == null)
            DoFleshmendSound(ent);
        return true;
    }

    private void DoFleshmend(Entity<FleshmendComponent> ent, EntityUid target)
    {
        // heal the damage
        foreach (var (group, amount) in ent.Comp.Healing)
        {
            _dmg.HealEvenly(target, amount, group);
        }

        // heal bleeding and restore blood
        _bloodstream.TryModifyBleedAmount(target, ent.Comp.BleedingAdjust);
        _bloodstream.TryModifyBloodLevel(target, ent.Comp.BloodLevelAdjust);
    }
}
