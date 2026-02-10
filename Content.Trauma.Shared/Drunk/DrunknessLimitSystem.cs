// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Goobstation.Common.CCVar;
using Content.Shared.Drunk;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Drunk;

public sealed class DrunknessLimitSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;

    private TimeSpan _maxDrunkLimit;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DrunknessLimitComponent, StatusEffectEndTimeUpdatedEvent>(OnEndTimeUpdated);

        Subs.CVar(_cfg, GoobCVars.MaxDrunkTime, x => _maxDrunkLimit = TimeSpan.FromSeconds(x), true);
    }

    private void OnEndTimeUpdated(Entity<DrunknessLimitComponent> ent, ref StatusEffectEndTimeUpdatedEvent args)
    {
        var maxEnd = _timing.CurTime + _maxDrunkLimit;
        if (args.EndTime is not {} endTime || endTime > maxEnd)
            _status.SetStatusEffectEndTime(ent.Owner, maxEnd);
    }
}
