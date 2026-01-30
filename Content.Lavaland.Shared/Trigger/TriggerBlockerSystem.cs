using Content.Shared.Trigger;
using Content.Shared.Whitelist;

namespace Content.Lavaland.Shared.Trigger;

public sealed class TriggerBlockerSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerBlockerComponent, AttemptTriggerEvent>(OnAttemptTrigger);
    }

    private void OnAttemptTrigger(Entity<TriggerBlockerComponent> ent, ref AttemptTriggerEvent args)
    {
        if (args.Cancelled)
            return;

        if (Transform(ent).MapUid is not {} map
            || _whitelist.IsWhitelistPass(ent.Comp.MapWhitelist, map)
            || _whitelist.IsWhitelistFail(ent.Comp.MapBlacklist, map))
            return;

        args.Cancelled = true;
    }
}
