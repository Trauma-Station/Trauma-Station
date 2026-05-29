using Content.Shared.Implants.Components;
using System.Linq;

namespace Content.Shared.Implants;

public abstract partial class SharedSubdermalImplantSystem
{
    /// <summary>
    /// Relay a event by ref to implants.
    /// </summary>
    public void RelayToImplantRef<T>(EntityUid uid, ImplantedComponent component, ref T args) where T : notnull
    {
        if (!_container.TryGetContainer(uid, ImplanterComponent.ImplantSlotId, out var implantContainer))
            return;

        var relayEv = new ImplantRelayEvent<T>(args, uid);
        foreach (var implant in implantContainer.ContainedEntities.ToList())
        {
            RaiseLocalEvent(implant, relayEv);
        }
        args = relayEv.Event; // set the original event at the end
    }
}
