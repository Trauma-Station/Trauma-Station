// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Chemistry;

namespace Content.Goobstation.Shared.Chemistry
{
    /// <summary>
    /// This class holds constants that are shared between client and server.
    /// </summary>
    public static class SharedEnergyReagentDispenser
    {
        public const string OutputSlotName = "energyBeakerSlot";
    }

    [Serializable, NetSerializable]
    public sealed class EnergyReagentDispenserSetDispenseAmountMessage(int amount) : BoundUserInterfaceMessage
    {
        public readonly int Amount = amount;
    }

    [Serializable, NetSerializable]
    public sealed class EnergyReagentDispenserDispenseReagentMessage(string reagentId) : BoundUserInterfaceMessage
    {
        public readonly string ReagentId = reagentId;
    }

    [Serializable, NetSerializable]
    public sealed class EnergyReagentDispenserClearContainerSolutionMessage : BoundUserInterfaceMessage;

    [Serializable, NetSerializable]
    public sealed class EnergyReagentInventoryItem(string reagentId, string reagentLabel, float powerCostPerUnit, Color reagentColor)
    {
        public string ReagentId = reagentId;
        public string ReagentLabel = reagentLabel;
        public float PowerCostPerUnit = powerCostPerUnit;
        public Color ReagentColor = reagentColor;
    }

    [Serializable, NetSerializable]
    public sealed class EnergyReagentDispenserBoundUserInterfaceState(
        ContainerInfo? outputContainer,
        NetEntity? outputContainerEntity,
        List<EnergyReagentInventoryItem> inventory,
        int selectedDispenseAmount,
        float batteryCharge,
        float batteryMaxCharge,
        float currentReceivingEnergy,
        float idleUse,
        bool usingBattery,
        bool hasPower)
        : BoundUserInterfaceState
    {
        public readonly ContainerInfo? OutputContainer = outputContainer;

        public readonly NetEntity? OutputContainerEntity = outputContainerEntity;

        /// <summary>
        /// A list of the reagents which this dispenser can dispense.
        /// </summary>
        public readonly List<EnergyReagentInventoryItem> Inventory = inventory;

        public readonly int SelectedDispenseAmount = selectedDispenseAmount;
        public readonly float BatteryCharge = batteryCharge;
        public readonly float BatteryMaxCharge = batteryMaxCharge;
        public readonly float CurrentReceivingEnergy = currentReceivingEnergy;
        public readonly float IdleUse = idleUse;
        public readonly bool UsingBattery = usingBattery;
        public readonly bool HasPower = hasPower;
    }

    [Serializable, NetSerializable]
    public enum EnergyReagentDispenserUiKey
    {
        Key
    }
}
