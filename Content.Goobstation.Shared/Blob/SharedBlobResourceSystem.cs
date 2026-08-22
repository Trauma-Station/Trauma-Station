// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Blob.Components;
using Content.Shared.Popups;

namespace Content.Goobstation.Shared.Blob;

public abstract partial class SharedBlobResourceSystem : EntitySystem
{
    [Dependency] private SharedBlobCoreSystem _core = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    [SubscribeLocalEvent]
    private void OnSpecialPulse(Entity<BlobResourceComponent> ent, ref BlobSpecialPulseEvent args)
    {
        var points = ent.Comp.PointsPerPulsed + args.Chem.BonusPoints;
        if (!_core.ChangeBlobPoint(args.Core, points) ||
            args.Core.Comp.Observer is not { } user)
            return;

        _popup.PopupEntity(Loc.GetString("blob-get-resource", ("point", points)),
            ent,
            user,
            PopupType.Large);
    }
}
