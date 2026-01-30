using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Whitelist;

namespace Content.Lavaland.Shared.Weapons.Ranged.Restricted;

public sealed class MapRestrictedGunSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MapRestrictedGunComponent, AttemptShootEvent>(OnAttemptShoot);
    }

    private void OnAttemptShoot(Entity<MapRestrictedGunComponent> ent, ref AttemptShootEvent args)
    {
        var xform = Transform(ent);
        if (args.Cancelled
            || xform.MapUid is not {} map
            || !_whitelist.CheckBoth(map, blacklist: ent.Comp.PlanetBlacklist, whitelist: ent.Comp.PlanetWhitelist))
            return;

        args.Cancelled = true;
        if (ent.Comp.PopupOnBlock != null)
            args.Message = Loc.GetString(ent.Comp.PopupOnBlock);
    }
}
