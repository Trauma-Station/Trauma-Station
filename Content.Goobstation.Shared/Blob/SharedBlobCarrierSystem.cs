// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Blob;
using Content.Shared.Popups;
using Robust.Shared.Timing;

namespace Content.Goobstation.Shared.Blob;

public abstract partial class SharedBlobCarrierSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<BlobCarrierComponent>();
        while (query.MoveNext(out var ent, out var comp))
        {
            if (!comp.HasMind)
                return;

            comp.TransformationTimer += frameTime;

            if (now < comp.NextAlert)
                continue;

            var remainingTime = Math.Round(comp.TransformationDelay - comp.TransformationTimer, 0);
            _popup.PopupEntity(Loc.GetString("carrier-blob-alert", ("second", (int) remainingTime)), ent, ent, PopupType.LargeCaution);

            comp.NextAlert = now + TimeSpan.FromSeconds(comp.AlertInterval);

            if (comp.TransformationTimer >= comp.TransformationDelay)
                TransformToBlob((ent, comp));
        }
    }

    protected virtual void TransformToBlob(Entity<BlobCarrierComponent> ent)
    {
    }
}
