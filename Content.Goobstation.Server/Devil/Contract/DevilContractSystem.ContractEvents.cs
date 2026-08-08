// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Goobstation.Shared.Devil;
using Content.Medical.Common.Body;
using Content.Medical.Shared.Wounds;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Robust.Shared.Random;

namespace Content.Goobstation.Server.Devil.Contract;

public sealed partial class DevilContractSystem
{
    // TODO: change to entity effects bruh
    [SubscribeLocalEvent]
    private void OnSoulOwnership(DevilContractSoulOwnershipEvent args)
    {
        if (args.Contract?.ContractOwner is not { } contractOwner)
            return;

        TryTransferSouls(contractOwner, args.Target, 1);
    }

    [SubscribeLocalEvent]
    private void OnLosePart(DevilContractLosePartEvent args)
    {
        var parts = _part.GetBodyParts(args.Target, BodyPartType.Hand);
        if (parts.Count <= 0)
            return;

        var pick = _random.Pick(parts);
        _body.RemoveOrgan(args.Target, pick);
        Log.Debug($"Removed part {ToPrettyString(pick)} from {ToPrettyString(args.Target)}");
        QueueDel(pick);
    }

    [SubscribeLocalEvent]
    private void OnLoseOrgan(DevilContractLoseOrganEvent args)
    {
        var eligibleOrgans = _body.GetInternalOrgans(args.Target);
        // don't remove the brain, as funny as that is.
        eligibleOrgans.RemoveAll(o => HasComp<BrainComponent>(o));
        if (eligibleOrgans.Count <= 0)
            return;

        var pick = _random.Pick(eligibleOrgans);
        _body.RemoveOrgan(args.Target, pick.Owner);
        Log.Debug($"Removed part {ToPrettyString(pick)} from {ToPrettyString(args.Target)}");
        QueueDel(pick);
    }

    // LETS GO GAMBLING!!!!!
    [SubscribeLocalEvent]
    private void OnChance(DevilContractChanceEvent args)
    {
        AddRandomClause(args.Target);
    }
}
