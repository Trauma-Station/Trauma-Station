// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Random.Helpers;
using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Components;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Knowledge;

/// <summary>
/// This exists for common rolling api shit
/// </summary>
public sealed partial class RollSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KnowledgeHolderComponent, SingleContestEvent>(OnSingleContest);
        SubscribeLocalEvent<KnowledgeHolderComponent, OpposedContestEvent>(OnOpposedContest);
    }

    private void OnSingleContest(Entity<KnowledgeHolderComponent> ent, ref SingleContestEvent args)
    {
        (args.DiceUser, args.CriticallySucceeded) = RollContest(args.DiceUser, ent.Owner);
        if (args.IsSkill)
        {
            args.Failed = args.DiceUser + args.ModUser > args.Threshold;
            args.CriticallyFailed = args.DiceUser >= 100;
            return;
        }
        args.Failed = args.DiceUser + args.ModUser <= args.Threshold;
        args.CriticallyFailed = args.DiceUser == 1;

        // _popup.PopupClient($"{args.DiceUser}+{args.ModUser} vs. {args.Threshold}", ent, ent, PopupType.Medium);
    }

    private void OnOpposedContest(Entity<KnowledgeHolderComponent> ent, ref OpposedContestEvent args)
    {
        (args.DiceUser, args.CriticallySucceededUser) = RollContest(args.DiceUser, ent.Owner);
        (args.DiceOpposed, args.CriticallySucceededOpposed) = RollContest(args.DiceOpposed, args.Opposer);

        args.Failed = args.DiceUser + args.ModUser <= args.DiceOpposed + args.ModOpposed;
        args.CriticallyFailedUser = args.DiceUser == 1;
        args.CriticallyFailedOpposed = args.DiceOpposed == 1;

        // Looks like shit, might delete popups later.
        // _popup.PopupClient($"{args.DiceUser}+{args.ModUser} vs. {args.DiceOpposed}+{args.ModOpposed}", ent, ent, PopupType.Medium);
        // _popup.PopupEntity($"{args.DiceOpposed}+{args.ModOpposed} vs. {args.DiceUser}+{args.ModUser}", args.Opposer, args.Opposer, PopupType.Medium);
    }

    /// <summary>
    /// All important rolling equation.
    /// </summary>
    public (int, bool) RollContest(int diceType, EntityUid uid)
    {
        var dice = diceType;
        var pen = false;
        var amount = 0;
        var count = 0;
        var roller = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(uid));

        while (count < 10)
        {
            var rolled = roller.Next(1, dice + 1);
            amount += rolled;
            if (rolled == dice)
            {
                pen = true;
                amount -= 1;
            }
            else
                return (amount, pen);
            count++;
            dice = dice switch
            {
                100 => 20,
                20 => 6,
                _ => dice,
            };
        }

        return (amount, pen);
    }
}
