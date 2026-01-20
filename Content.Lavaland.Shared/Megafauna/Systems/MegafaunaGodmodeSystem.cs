using Content.Lavaland.Shared.Megafauna.Components;
using Content.Shared.Damage.Systems;
using Robust.Shared.Player;

namespace Content.Lavaland.Shared.Megafauna.Systems;

public sealed class MegafaunaGodmodeSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MegafaunaGodmodeComponent, BeforeDamageChangedEvent>(OnBeforeDamageChanged);
    }

    private void OnBeforeDamageChanged(Entity<MegafaunaGodmodeComponent> ent, ref BeforeDamageChangedEvent args)
    {
        if (args.Origin == null
            || !HasComp<ActorComponent>(args.Origin))
            args.Cancelled = true;
    }
}
