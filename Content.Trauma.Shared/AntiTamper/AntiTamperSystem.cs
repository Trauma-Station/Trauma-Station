using Content.Shared.Chat;
using Content.Shared.Damage.Systems;
using Content.Shared.Destructible;
using Content.Shared.Examine;
using JetBrains.Annotations;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.AntiTamper;

public sealed partial class AntiTamperSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedChatSystem _chat = default!;
    [Dependency] private IGameTiming _timing = default!;

    [SubscribeLocalEvent, UsedImplicitly]
    private void OnExamine(Entity<AntiTamperComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.LocExamine is null)
            return;
        args.PushMarkup(Loc.GetString(ent.Comp.LocExamine));
    }

    [SubscribeLocalEvent(after: [typeof(SharedDestructibleSystem)]), UsedImplicitly]
    private void OnDamage(Entity<AntiTamperComponent> ent, ref DamageDealtEvent args)
    {
        if (!TryComp(ent, out DestructibleComponent? destructible))
            return; // i mean dont relaly care if it cant be destroyed anywaysssss

        var comp = ent.Comp;
        var ded = destructible.IsBroken;

        var alarmOnDamaged = CompareFlag(comp.AlarmAlertType, AntiTamperAlertType.OnDamaged);
        var alarmOnDestroyed = CompareFlag(comp.AlarmAlertType, AntiTamperAlertType.OnDestroyed);
        var yellOnDamaged = CompareFlag(comp.YellAlertType, AntiTamperAlertType.OnDamaged);
        var yellOnDestroyed = CompareFlag(comp.YellAlertType, AntiTamperAlertType.OnDestroyed);

        if (_timing.CurTime - comp.LastAlarm >= comp.AlarmCooldown &&
            !ded && alarmOnDamaged || ded && alarmOnDestroyed)
            AlertAlarm(ent);
        if (_timing.CurTime - comp.LastYell >= comp.AlarmCooldown &&
            !ded && yellOnDamaged || ded && yellOnDestroyed)
            AlertYell(ent);
    }

    public void AlertAlarm(Entity<AntiTamperComponent> ent)
    {
        _audio.PlayPvs(ent.Comp.AlarmSound, Transform(ent).Coordinates);
        ent.Comp.LastAlarm = _timing.CurTime;
    }

    public void AlertYell(Entity<AntiTamperComponent> ent)
    {
        _chat.TrySendInGameICMessage(ent, Loc.GetString(ent.Comp.LocTamperMessage), InGameICChatType.Speak, false);
        ent.Comp.LastYell = _timing.CurTime;
    }

    private bool CompareFlag(AntiTamperAlertType target, AntiTamperAlertType flag)
        => (target & flag) != 0;
}