// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Chemistry.Reagent;

namespace Content.Goobstation.Shared.Chemistry;

[Serializable, NetSerializable]
public enum EnergyReagentDispenserUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class EnergyReagentDispenserSetDispenseAmountMessage(int amount) : BoundUserInterfaceMessage
{
    public readonly int Amount = amount;
}

[Serializable, NetSerializable]
public sealed class EnergyReagentDispenserDispenseReagentMessage(ProtoId<ReagentPrototype> reagentId) : BoundUserInterfaceMessage
{
    public readonly ProtoId<ReagentPrototype> ReagentId = reagentId;
}

[Serializable, NetSerializable]
public sealed class EnergyReagentDispenserClearContainerSolutionMessage : BoundUserInterfaceMessage;
