// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Overlays;
using Content.Trauma.Shared.SecTrack;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Client.SecTrack.Overlays;

public sealed class ShowSquadIconsSystem : EquipmentHudSystem<ShowSquadIconsComponent>
{
    [Dependency] private IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SquadMemberComponent, GetStatusIconsEvent>(OnGetStatusIconsEvent);
    }

    private void OnGetStatusIconsEvent(EntityUid uid, SquadMemberComponent component, ref GetStatusIconsEvent ev)
    {
        if (!IsActive)
            return;

        if (_prototype.Resolve(component.StatusIcon, out var iconPrototype))
            ev.StatusIcons.Add(iconPrototype);
    }
}
