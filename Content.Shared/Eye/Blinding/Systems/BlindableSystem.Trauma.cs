using Content.Medical.Common.Body;
using Content.Shared.Body;
using Content.Shared.Eye.Blinding.Components;

namespace Content.Shared.Eye.Blinding.Systems;

/// <summary>
/// Trauma - eye damage gets transferred to organs
/// </summary>
public sealed partial class BlindableSystem
{
    [Dependency] private readonly BodySystem _body = default!;

    private void UpdateEyeOrganDamage(EntityUid uid, int amount)
    {
        var ev = new EyesDamagedEvent(uid, amount);
        if (TryComp<BodyComponent>(uid, out var body))
            _body.RelayEvent((uid, body), ref ev);
    }

    // Alternative version of the method intended to be used with Eye Organs, so that you can just pass in
    // the severity and set that.
    public void SetEyeDamage(Entity<BlindableComponent?> blindable, int amount)
    {
        if (!Resolve(blindable, ref blindable.Comp, false))
            return;

        blindable.Comp.EyeDamage = amount;
        UpdateEyeDamage(blindable, true);
    }
}
