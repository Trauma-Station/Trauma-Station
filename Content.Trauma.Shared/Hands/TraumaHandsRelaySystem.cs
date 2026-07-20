// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory.Events;
using Content.Shared.Overlays;
using Content.Trauma.Common.Weapons;
using Content.Trauma.Shared.Heretic.Events;
using Content.Trauma.Shared.Viewcone;

namespace Content.Trauma.Shared.Hands;

public sealed partial class TraumaHandsRelaySystem : EntitySystem
{
    [Dependency] private SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<HandsComponent, CheckMagicItemEvent>(_hands.RelayEvent);

        // By-ref events.
        SubscribeLocalEvent<HandsComponent, BeforeHarmfulActionEvent>(_hands.RefRelayEvent);
        SubscribeLocalEvent<HandsComponent, RefreshEquipmentHudEvent<ShowHealthBarsComponent>>(_hands.RefRelayEvent);
        SubscribeLocalEvent<HandsComponent, RefreshEquipmentHudEvent<ShowHealthIconsComponent>>(_hands.RefRelayEvent);
        SubscribeLocalEvent<HandsComponent, ModifyViewconeAngleEvent>(_hands.RefRelayEvent);
    }
}
