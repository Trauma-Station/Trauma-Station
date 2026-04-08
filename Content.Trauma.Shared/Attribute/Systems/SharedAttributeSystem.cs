// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body;
using Content.Shared.FixedPoint;
using Content.Shared.Mind.Components;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Trauma.Common.Attribute;
using Content.Trauma.Common.Attribute.Components;
using Content.Trauma.Common.Attribute.Systems;
using Content.Trauma.Common.Silicons.Borgs;
using Content.Trauma.Shared.Attribute.Components;
using Content.Trauma.Shared.Mobs;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Trauma.Shared.Attribute.Systems;

/// <summary>
/// This handles all attribute related entities.
/// </summary>
public sealed partial class SharedAttributeSystem : CommonAttributeSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly EntityQuery<AwakeMobComponent> _awakeQuery = default!;
    [Dependency] private readonly EntityQuery<AttributeComponent> _query = default!;
    [Dependency] private readonly EntityQuery<AttributeContainerComponent> _containerQuery = default!;
    [Dependency] private readonly EntityQuery<AttributeHolderComponent> _holderQuery = default!;

    /// <summary>
    /// Every attribute prototype and its data.
    /// </summary>
    public Dictionary<EntProtoId, AttributeComponent> AllAttributes = new();

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AttributeHolderComponent, OnAttributeSingleContest>(OnSingleContest);
        SubscribeLocalEvent<AttributeHolderComponent, OnAttributeOpposedContest>(OnOpposedContest);
    }

    public static int LerpCurve(FixedPoint2 input, FixedPoint2 minX, FixedPoint2 maxX, FixedPoint2 minY, FixedPoint2 maxY)
    {
        FixedPoint2 rawY = minY + (input - minX) * (maxY - minY) / (maxX - minX);

        return rawY.Int();
    }

    private void OnSingleContest(Entity<AttributeHolderComponent> ent, ref OnAttributeSingleContest args)
    {
        (args.DiceUser, args.CriticallySucceeded) = RollContest(args.DiceUser, ent.Owner);
        args.Failed = (args.DiceUser + args.ModUser <= args.Threshold);
        args.CriticallyFailed = (args.DiceUser == 1);

        _popup.PopupPredicted($"{args.DiceUser}+{args.ModUser} vs. {args.Threshold}", ent, ent, PopupType.Medium);
    }

    private void OnOpposedContest(Entity<AttributeHolderComponent> ent, ref OnAttributeOpposedContest args)
    {
        (args.DiceUser, args.CriticallySucceededUser) = RollContest(args.DiceUser, ent.Owner);
        (args.DiceOpposed, args.CriticallySucceededOpposed) = RollContest(args.DiceOpposed, args.Opposer);

        args.Failed = (args.DiceUser + args.ModUser <= args.DiceOpposed + args.ModOpposed);
        args.CriticallyFailedUser = (args.DiceUser == 1);
        args.CriticallyFailedOpposed = (args.DiceOpposed == 1);

        _popup.PopupPredicted($"{args.DiceUser}+{args.ModUser} vs. {args.DiceOpposed}+{args.ModOpposed}", ent, ent, PopupType.Medium);
        _popup.PopupPredicted($"{args.DiceOpposed}+{args.ModOpposed} vs. {args.DiceUser}+{args.ModUser}", args.Opposer, args.Opposer, PopupType.Medium);
    }

    private (int, bool) RollContest(int diceType, EntityUid uid)
    {
        var dice = diceType;
        var pen = false;
        int amount = 0;
        int count = 0;
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
