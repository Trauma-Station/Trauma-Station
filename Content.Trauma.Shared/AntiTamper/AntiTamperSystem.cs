// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Chat;
using Content.Shared.Damage.Systems;
using Content.Shared.Destructible;
using Content.Shared.Examine;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.AntiTamper;

public sealed partial class AntiTamperSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedChatSystem _chat = default!;

    [Dependency] private EntityQuery<DestructibleComponent> _destructibleQuery = default!;

    [SubscribeLocalEvent]
    private void OnExamine(Entity<AntiTamperComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.LocExamine is null)
            return;
        args.PushMarkup(Loc.GetString(ent.Comp.LocExamine));
    }

    [SubscribeLocalEvent(after: [typeof(SharedDestructibleSystem)])] // after so .IsBroken is set
    private void OnDamage(Entity<AntiTamperComponent> ent, ref DamageDealtEvent args)
    {
        if (!_destructibleQuery.TryComp(ent, out var destructible))
            return; // i mean dont relaly care if it cant be destroyed anywaysssss

        var comp = ent.Comp;
        var ded = destructible.IsBroken; // Will be destroyed very shortly, but not yet

        var alarmOnDamaged = CompareFlag(comp.AlarmAlertType, AntiTamperAlertType.OnDamaged);
        var alarmOnDestroyed = CompareFlag(comp.AlarmAlertType, AntiTamperAlertType.OnDestroyed);
        var yellOnDamaged = CompareFlag(comp.YellAlertType, AntiTamperAlertType.OnDamaged);
        var yellOnDestroyed = CompareFlag(comp.YellAlertType, AntiTamperAlertType.OnDestroyed);

        if (!ded && alarmOnDamaged || ded && alarmOnDestroyed)
            AlertAlarm(ent);
        if (!ded && yellOnDamaged || ded && yellOnDestroyed)
            AlertYell(ent);
    }

    /// <summary>
    /// Play the AntiTamper alarm noise.
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="respectCooldown"></param>
    public void AlertAlarm(Entity<AntiTamperComponent> ent, bool respectCooldown = true)
    {
        if (respectCooldown && _timing.CurTime - ent.Comp.LastAlarm < ent.Comp.AlarmCooldown)
            return;

        _audio.PlayPvs(ent.Comp.AlarmSound, Transform(ent).Coordinates);

        ent.Comp.LastAlarm = _timing.CurTime;
    }

    /// <summary>
    /// Trigger the AntiTamper speech bubble yell.
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="respectCooldown"></param>
    public void AlertYell(Entity<AntiTamperComponent> ent, bool respectCooldown = true)
    {
        if (respectCooldown && _timing.CurTime - ent.Comp.LastYell < ent.Comp.YellCooldown)
            return;

        _chat.TrySendInGameICMessage(ent, Loc.GetString(ent.Comp.LocTamperMessage), InGameICChatType.Speak, false);

        ent.Comp.LastYell = _timing.CurTime;
    }

    private bool CompareFlag(AntiTamperAlertType target, AntiTamperAlertType flag)
        => (target & flag) != 0;
}
