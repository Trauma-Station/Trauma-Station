// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Goobstation.Shared.Stacks;

[Serializable, NetSerializable]
public sealed class StackCustomSplitAmountMessage : BoundUserInterfaceMessage
{
    public int Amount;

    public StackCustomSplitAmountMessage(int amount)
    {
        Amount = amount;
    }
}
