using Content.Trauma.Common.Mindshield;
using Content.Shared.Mindshield;
using Content.Shared.Mindshield.Components;

namespace Content.Shared.Implants;

public sealed partial class MindshieldImplantSystem
{
    [Dependency] private MindShieldSystem _mindShield = default!;

    private bool TryPreventMindshield(EntityUid uid, EntityUid implant)
    {
        var attemptEv = new MindShieldAttemptEvent();
        RaiseLocalEvent(uid, ref attemptEv);
        if (attemptEv.CancelPopup is not { } cancelPopup)
        {
            var ev = new MindShieldedEvent();
            RaiseLocalEvent(uid, ref ev);
            return false;
        }

        _popup.PopupEntity(Loc.GetString(cancelPopup), uid);
        var shield = Comp<MindShieldComponent>(implant);
        shield.Broken = true;
        Dirty(implant, shield);
        _mindShield.RefreshMindshieldStatus(uid);
        return true;
    }
}
