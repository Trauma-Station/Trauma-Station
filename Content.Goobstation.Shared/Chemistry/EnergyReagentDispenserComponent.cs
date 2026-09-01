// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Audio;
using Robust.Shared.Containers;

namespace Content.Goobstation.Shared.Chemistry;

/// <summary>
/// A machine that dispenses reagents into a solution container using energy.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(EnergyReagentDispenserSystem))]
[AutoGenerateComponentState]
public sealed partial class EnergyReagentDispenserComponent : Component
{
    [DataField]
    public string OutputSlotName = "energyBeakerSlot";

    [ViewVariables]
    public ContainerSlot BeakerSlot = default!;

    [ViewVariables]
    public EntityUid? Beaker => BeakerSlot.ContainedEntity;

    [DataField]
    public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg")
    {
        Params = AudioParams.Default.WithVolume(-2f)
    };

    [DataField, AutoNetworkedField]
    public int DispenseAmount = 10;

    [DataField]
    public int MinDispenseAmount = 1;

    [DataField]
    public int MaxDispenseAmount = 120;

    [DataField]
    public SoundSpecifier PowerSound = new SoundPathSpecifier("/Audio/Machines/buzz-sigh.ogg")
    {
        Params = AudioParams.Default.WithVolume(-2f)
    };

    /// <summary>
    /// Every reagent and how much energy it costs to spawn 1u of it.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<ReagentPrototype>, int> Reagents = default!;
}
