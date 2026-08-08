namespace Content.Shared.DeviceLinking.Events
{
    public sealed class PortDisconnectedEvent : EntityEventArgs
    {
        public readonly string Port;
        public readonly EntityUid? RemovedPortUid; // Trauma

        public PortDisconnectedEvent(string port, EntityUid? removedPortUid = null) // Trauma: added removedPortUid
        {
            Port = port;
            RemovedPortUid = removedPortUid; // Trauma
        }
    }
}
