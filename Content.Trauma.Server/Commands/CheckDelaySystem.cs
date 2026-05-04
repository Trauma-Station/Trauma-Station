using Content.Trauma.Shared.Commands;

namespace Content.Trauma.Server.Commands;

public sealed class CheckDelaySystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<CheckDelayEvent>(OnCheckDelay);
    }

    private void OnCheckDelay(CheckDelayEvent ev, EntitySessionEventArgs args)
    {
        ev.Received = DateTime.UtcNow;
        RaiseNetworkEvent(ev, args.SenderSession);
    }
}
