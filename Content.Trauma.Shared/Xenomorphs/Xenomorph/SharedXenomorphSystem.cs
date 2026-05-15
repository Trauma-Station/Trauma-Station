// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Trauma.Common.Xenomorphs;
using Content.Trauma.Shared.Xenomorphs.Infection;
using Content.Trauma.Shared.Xenomorphs.Larva;

namespace Content.Trauma.Shared.Xenomorphs.Xenomorph;

public abstract partial class SharedXenomorphSystem : CommonXenomorphSystem
{
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    private static readonly ProtoId<TagPrototype> XenomorphItemTag = "XenomorphItem";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenomorphComponent, PickupAttemptEvent>(OnPickup);
    }

    private void OnPickup(EntityUid uid, XenomorphComponent component, PickupAttemptEvent args)
    {
        if (_tag.HasTag(args.Item, XenomorphItemTag))
            return;

        _popup.PopupClient(Loc.GetString("xenomorph-pickup-item-fail"), args.Item, uid);
        args.Cancel();
    }

    public override bool IsSlimed(EntityUid uid)
    {
        return HasComp<XenomorphPreventSuicideComponent>(uid);
    }

    public override bool IsVictim(EntityUid uid)
    {
        return HasComp<XenomorphLarvaVictimComponent>(uid);
    }
}
