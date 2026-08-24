// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Bank;

namespace Content.Trauma.Client.Bank;

public sealed partial class BankSystem : SharedBankSystem
{
    public List<string> GetBankIDs(List<NetEntity> banks)
    {
        List<string> bankIds = new();
        foreach (var bank in banks)
        {
            if (!TryComp<BankComponent>(GetEntity(bank), out var bankComp))
                continue;
            bankIds.Add(bankComp.BankId);
        }
        return bankIds;
    }

    public string GetBankID(NetEntity bank)
    {
        if (!TryComp<BankComponent>(GetEntity(bank), out var bankComp))
            return "";

        return bankComp.BankId;
    }
}
