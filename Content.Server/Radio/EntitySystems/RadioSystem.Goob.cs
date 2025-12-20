using Content.Server.Power.Components;
using Content.Goobstation.Shared.Communications;
using Robust.Shared.Map;
using System.Linq;

namespace Content.Server.Radio.EntitySystems;

public sealed partial class RadioSystem
{
    public bool HasActiveTransmitter(MapId mapId)
    => EntityQuery<TelecomTransmitterComponent, ApcPowerReceiverComponent, TransformComponent>()
        .Any(server => server.Item3.MapID == mapId && server.Item2.Powered);
}
