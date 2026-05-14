using Content.Server.Silicons.Laws;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using Content.Trauma.Common.Silicon;
using Robust.Shared.Prototypes;

namespace Content.Server.Silicons.Borgs;

/// <summary>
/// Handles lawset patching when switching type.
/// If a borg is made emagged it needs its emag laws carried over.
/// </summary>
public sealed partial class BorgSwitchableTypeSystem
{
    [Dependency] private SiliconLawSystem _law = default!;

    private void ConfigureLawset(EntityUid uid, ProtoId<SiliconLawsetPrototype> id)
    {
        var laws = _law.GetLawset(id);

        _law.SetLaws(laws.Laws, uid);

        var lawsetChangedev = new SiliconLawsetChangedEvent();
        RaiseLocalEvent(uid, ref lawsetChangedev);

        // re-add law 0 and final law based on new lawset
        if (CompOrNull<EmagSiliconLawComponent>(uid)?.OwnerName != null)
        {
            // raising the event manually to bypass re-emagging checks
            var ev = new SiliconEmaggedEvent(uid);
            RaiseLocalEvent(uid, ref ev);
        }

        // ion storms don't get mirrored because thats basically impossible to track
    }
}
