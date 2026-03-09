using Content.Shared.Heretic;
using Content.Shared.Inventory.Events;
using Content.Shared.Overlays;
using Content.Trauma.Common.Parry;
using Content.Shared.Hands.Components;

namespace Content.Trauma.Shared.Hands;

public sealed class TraumaHandsRelaySystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<HandsComponent, CheckMagicItemEvent>(RelayEvent);

        // By-ref events.
        SubscribeLocalEvent<HandsComponent, ParryAttemptEvent>(RefRelayEvent);
        SubscribeLocalEvent<HandsComponent, RefreshEquipmentHudEvent<ShowHealthBarsComponent>>(RefRelayEvent);
        SubscribeLocalEvent<HandsComponent, RefreshEquipmentHudEvent<ShowHealthIconsComponent>>(RefRelayEvent);
    }
}
