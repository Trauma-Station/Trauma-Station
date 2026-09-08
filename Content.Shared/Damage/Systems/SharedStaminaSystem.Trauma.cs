using Content.Goobstation.Common.Stunnable;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Events;

namespace Content.Shared.Damage.Systems;

public abstract partial class SharedStaminaSystem
{
    public void TakeOvertimeStaminaDamage(EntityUid uid, float value)
    {
        if (value == 0)
            return;

        var hasComp = TryComp<OvertimeStaminaDamageComponent>(uid, out var overtime);

        if (!hasComp)
            overtime = EnsureComp<OvertimeStaminaDamageComponent>(uid);

        var ev = new BeforeStaminaDamageEvent(value);
        RaiseLocalEvent(uid, ref ev);
        overtime!.Amount = hasComp ? overtime.Amount + ev.Value : ev.Value;
        overtime!.Damage = hasComp ? overtime.Damage + ev.Value : ev.Value;
    }

    public void ToggleStaminaDrain(EntityUid target, float drainRate, bool enabled, bool modifiesSpeed, string key, EntityUid? source = null, bool ignoreResist = false)
    {
        if (!TryComp<StaminaComponent>(target, out var stamina))
            return;

        // If theres no source, we assume its the target that caused the drain.
        var actualSource = source ?? target;

        if (enabled)
        {
            stamina.ActiveDrains.TryAdd(key, (drainRate, modifiesSpeed, GetNetEntity(actualSource), ignoreResist));
            EnsureComp<ActiveStaminaComponent>(target);
        }
        else
        {
            if (stamina.ActiveDrains.ContainsKey(key))
                stamina.ActiveDrains.Remove(key);
        }

        Dirty(target, stamina);
    }
}
