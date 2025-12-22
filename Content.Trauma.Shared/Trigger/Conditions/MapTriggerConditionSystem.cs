// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Trigger;
using Content.Shared.Whitelist;

namespace Content.Trauma.Shared.Trigger.Conditions;

public sealed class MapTriggerConditionSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MapTriggerConditionComponent, AttemptTriggerEvent>(OnAttemptTrigger);
    }

    private void OnAttemptTrigger(Entity<MapTriggerConditionComponent> ent, ref AttemptTriggerEvent args)
    {
        if (args.Key is not {} key || !ent.Comp.Keys.Contains(key))
            return;

        args.Cancelled |= Transform(ent).MapUid is not {} map ||
            !_whitelist.CheckBoth(map, blacklist: ent.Comp.Blacklist, whitelist: ent.Comp.Whitelist);
    }
}
