using Content.Server.Wires;
using Content.Shared.IdentityManagement;
using Content.Shared.Wires;
using Content.Trauma.Shared.AntiTamper;
using Serilog;

namespace Content.Trauma.Server.AntiTamper;

public sealed partial class AntiTamperWireAction : ComponentWireAction<AntiTamperComponent>
{
    public override Color Color { get; set; } = Color.PaleVioletRed;
    public override string Name { get; set; } = "wire-name-anti-tamper";

    [DataField("pulseTimeout")]
    private int _pulseTimeout = 10;

    public override StatusLightState? GetLightState(Wire wire, AntiTamperComponent comp)
    {
        return comp.Enabled ? StatusLightState.On : StatusLightState.Off;
    }

    public override object StatusKey => AntiTamperWireActionKey.Status;

    public override bool Cut(EntityUid user, Wire wire, AntiTamperComponent comp)
    {
        comp.Enabled = false;
        return true;
    }

    public override bool Mend(EntityUid user, Wire wire, AntiTamperComponent comp)
    {
        comp.Enabled = true;
        return true;
    }

    // Genius!
    public override void Pulse(EntityUid user, Wire wire, AntiTamperComponent comp)
    {
        if (WiresSystem.TryGetData<bool>(wire.Owner, AntiTamperWireActionKey.Pulsed, out var pulsedKey) && pulsedKey)
            return;

        EntityManager.System<AntiTamperSystem>().AlertYell((wire.Owner, comp));
        EntityManager.System<AntiTamperSystem>().AlertAlarm((wire.Owner, comp));

        WiresSystem.SetData(wire.Owner, AntiTamperWireActionKey.Pulsed, true);
        WiresSystem.StartWireAction(wire.Owner, _pulseTimeout, AntiTamperWireActionKey.PulseCancel,
            new TimedWireEvent(AwaitPulseCancel, wire));
    }

    private void AwaitPulseCancel(Wire wire)
    {
        WiresSystem.SetData(wire.Owner, AntiTamperWireActionKey.Pulsed, false);
    }

    private enum PulseTimeoutKey : byte
    {
        Key
    }
}
