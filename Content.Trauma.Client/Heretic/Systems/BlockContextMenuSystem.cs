using Content.Shared.Whitelist;
using Content.Trauma.Common.Heretic;

namespace Content.Trauma.Client.Heretic.Systems;

public sealed class BlockContextMenuSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlockContextMenuComponent, ShouldBlockContextMenuEvent>(OnShouldBlock);
    }

    private void OnShouldBlock(Entity<BlockContextMenuComponent> ent, ref ShouldBlockContextMenuEvent args)
    {
        if (_whitelist.CheckBoth(args.Target, ent.Comp.Blacklist, ent.Comp.Whitelist))
            args.ShouldBlock = true;
    }
}
