using Content.Goobstation.Shared.Speech;
using Content.Shared.Speech;
using Content.Shared.Whitelist;

namespace Content.Goobstation.Shared.Speech;

/// <summary>
/// Handles <see cref="ListenAttemptEvent"/> for <see cref="ListenBlacklistComponent"/>.
/// </summary>
public sealed class ListenBlacklistSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ListenBlacklistComponent, ListenAttemptEvent>(OnListenAttempt);
    }

    private void OnListenAttempt(Entity<ListenBlacklistComponent> ent, ref ListenAttemptEvent args)
    {
        if (_whitelist.IsWhitelistPass(ent.Comp.Blacklist, args.Source))
            args.Cancel();
    }
}
