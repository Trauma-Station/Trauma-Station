// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Knowledge.Attribute.Attribute;

/// <summary>
/// This handles all attribute related entities.
/// </summary>
public sealed partial class AttributeSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    // [Dependency] private readonly SharedPopupSystem _popup = default!;

    /// <summary>
    /// Every attribute prototype and its data.
    /// </summary>
    public Dictionary<EntProtoId, AttributeComponent> AllAttributes = new();

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KnowledgeHolderComponent, OnAttributeSingleContest>(OnSingleContest);
        SubscribeLocalEvent<KnowledgeHolderComponent, OnAttributeOpposedContest>(OnOpposedContest);
    }

    /// <summary>
    /// Common lerp used for attributes.
    /// </summary>
    public static int LerpCurve(FixedPoint2 input, FixedPoint2 minX, FixedPoint2 maxX, FixedPoint2 minY, FixedPoint2 maxY)
    {
        var rawY = minY + (input - minX) * (maxY - minY) / (maxX - minX);

        return rawY.Int();
    }

    /// <summary>
    /// Override method for adjusting attribute.
    /// </summary>
    public void AdjustAttribute(Entity<AttributeComponent> attribute, int adjust)
    {
        attribute.Comp.Inherent = AdjustAttribute(attribute.Comp.Inherent, adjust);
    }

    /// <summary>
    /// Adjusted an attribute according to exp shit.
    /// </summary>
    public static FixedPoint2 AdjustAttribute(FixedPoint2 inherent, int adjust)
    {
        FixedPoint2 value = inherent;
        int amount = Math.Abs(adjust);
        int direction = Math.Sign(adjust);

        for (int i = 0; i < amount; i++)
        {
            if (value < 10.00)
                value += direction * 0.10;
            else if (value > 16.00)
                value += direction * 0.03;
            else
                value += direction * 0.05;
        }

        return value;
    }

    private void OnSingleContest(Entity<KnowledgeHolderComponent> ent, ref OnAttributeSingleContest args)
    {
        (args.DiceUser, args.CriticallySucceeded) = RollContest(args.DiceUser, ent.Owner);
        args.Failed = args.DiceUser + args.ModUser <= args.Threshold;
        args.CriticallyFailed = args.DiceUser == 1;

        // _popup.PopupClient($"{args.DiceUser}+{args.ModUser} vs. {args.Threshold}", ent, ent, PopupType.Medium);
    }

    private void OnOpposedContest(Entity<KnowledgeHolderComponent> ent, ref OnAttributeOpposedContest args)
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

    private (int, bool) RollContest(int diceType, EntityUid uid)
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
