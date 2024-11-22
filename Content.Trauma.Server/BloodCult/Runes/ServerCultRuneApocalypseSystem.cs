// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.GameTicking;
using Content.Shared.Emp;
using Content.Trauma.Server.BloodCult.Gamerule;
using Content.Trauma.Shared.BloodCult.Runes.Apocalypse;
using Robust.Shared.Player;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Trauma.Server.BloodCult.Runes.Apocalypse;

public sealed partial class ServerCultRuneApocalypseSystem : CultRuneApocalypseSystem
{
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private GameTicker _ticker = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedEmpSystem _emp = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    // basically none of this can be predicted
    [SubscribeLocalEvent]
    private void OnDoAfter(Entity<CultRuneApocalypseComponent> ent, ref ApocalypseRuneDoAfter args)
    {
        if (args.Cancelled || EntityQuery<BloodCultRuleComponent>().FirstOrDefault() is not { } cultRule)
            return;

        ent.Comp.Used = true;
        Dirty(ent);
        _appearance.SetData(ent, ApocalypseRuneVisuals.Used, true);

        _emp.EmpPulse(_transform.GetMapCoordinates(ent),
            ent.Comp.EmpRange,
            ent.Comp.EmpEnergyConsumption,
            ent.Comp.EmpDuration);

        foreach (var guaranteedEvent in ent.Comp.GuaranteedEvents)
        {
            _ticker.StartGameRule(guaranteedEvent);
        }

        var requiredCultistsThreshold = MathF.Floor(_player.PlayerCount * ent.Comp.CultistsThreshold);
        var totalCultists = cultRule.Cultists.Count + cultRule.Constructs.Count;
        if (totalCultists >= requiredCultistsThreshold)
            return;

        var (randomEvent, repeatTimes) = _random.Pick(ent.Comp.PossibleEvents);
        for (var i = 0; i < repeatTimes; i++)
        {
            _ticker.StartGameRule(randomEvent);
        }
    }
}
