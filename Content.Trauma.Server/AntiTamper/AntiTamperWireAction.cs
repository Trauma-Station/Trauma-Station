// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Wires;
using Content.Shared.Wires;
using Content.Trauma.Shared.AntiTamper;

namespace Content.Trauma.Server.AntiTamper;

public sealed partial class AntiTamperWireAction : ComponentWireAction<AntiTamperComponent>
{
    private AntiTamperSystem _antiTamper = default!;

    public override Color Color { get; set; } = Color.PaleVioletRed;
    public override string Name { get; set; } = "wire-name-anti-tamper";

    [DataField]
    private int _pulseTimeout = 10;

    public override void Initialize()
    {
        base.Initialize();

        _antiTamper = EntityManager.System<AntiTamperSystem>();
    }

    public override StatusLightState? GetLightState(Wire wire, AntiTamperComponent comp)
    {
        return comp.Enabled ? StatusLightState.On : StatusLightState.Off;
    }

    public override object StatusKey => AntiTamperWireActionKey.Status;

    public override bool Cut(EntityUid user, Wire wire, AntiTamperComponent comp)
    {
        // too easy guh, need a better way if you want to make it disableable without risk
        // "disableable" is a really fucking stupid word
        _antiTamper.AlertYell((wire.Owner, comp), respectCooldown: false);
        _antiTamper.AlertAlarm((wire.Owner, comp), respectCooldown: false);

        comp.Enabled = false;
        EntityManager.Dirty(wire.Owner, comp);

        return true;
    }

    public override bool Mend(EntityUid user, Wire wire, AntiTamperComponent comp)
    {
        comp.Enabled = true;
        EntityManager.Dirty(wire.Owner, comp);

        return true;
    }

    // Genius!
    public override void Pulse(EntityUid user, Wire wire, AntiTamperComponent comp)
    {
        if (WiresSystem.TryGetData<bool>(wire.Owner, AntiTamperWireActionKey.Pulsed, out var pulsedKey) && pulsedKey)
            return;

        _antiTamper.AlertYell((wire.Owner, comp), respectCooldown: false);
        _antiTamper.AlertAlarm((wire.Owner, comp), respectCooldown: false);

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
