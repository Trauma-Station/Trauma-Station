using Content.Goobstation.Common.Magic;
using Content.Shared.Ghost;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Zombies;
using Content.Trauma.Common.Wizard;
using Content.Trauma.Shared.Wizard.Chuuni;
using Content.Trauma.Shared.Wizard.FadingTimedDespawn;
using Robust.Shared.Spawners;

namespace Content.Trauma.Shared.Wizard;

public sealed partial class SharedWizardSystem : CommonWizardSystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GhostComponent, BeforeMindSwappedEvent>(OnMindswapGhost);
        SubscribeLocalEvent<SpectralComponent, BeforeMindSwappedEvent>(OnMindswapSpectral);
        SubscribeLocalEvent<TimedDespawnComponent, BeforeMindSwappedEvent>(OnMindswapTemporary);
        SubscribeLocalEvent<FadingTimedDespawnComponent, BeforeMindSwappedEvent>(OnMindswapFadedTemporary);
        SubscribeLocalEvent<MobStateComponent, BeforeMindSwappedEvent>(OnMindswapIncapacitated);
        SubscribeLocalEvent<ZombieComponent, BeforeMindSwappedEvent>(OnMindswapZombie);
    }

    public override bool IsChunni(EntityUid? eyepatch)
    {
        return HasComp<ChuuniEyepatchComponent>(eyepatch);
    }

    private void OnMindswapGhost(Entity<GhostComponent> ent, ref BeforeMindSwappedEvent args)
    {
        if (args.Cancelled)
            return;

        args.Message = "ghost";
        args.Cancelled = true;
    }

    private void OnMindswapSpectral(Entity<SpectralComponent> ent, ref BeforeMindSwappedEvent args)
    {
        if (args.Cancelled)
            return;

        args.Message = "ghost";
        args.Cancelled = true;
    }

    private void OnMindswapTemporary(Entity<TimedDespawnComponent> ent, ref BeforeMindSwappedEvent args)
    {
        if (args.Cancelled)
            return;

        args.Message = "temporary";
        args.Cancelled = true;
    }

    private void OnMindswapFadedTemporary(Entity<FadingTimedDespawnComponent> ent, ref BeforeMindSwappedEvent args)
    {
        if (args.Cancelled)
            return;

        args.Message = "temporary";
        args.Cancelled = true;
    }

    private void OnMindswapIncapacitated(Entity<MobStateComponent> ent, ref BeforeMindSwappedEvent args)
    {
        if (args.Cancelled || !_mobState.IsIncapacitated(ent))
            return;

        args.Message = "dead";
        args.Cancelled = true;
    }

    private void OnMindswapZombie(Entity<ZombieComponent> ent, ref BeforeMindSwappedEvent args)
    {
        if (args.Cancelled)
            return;
        args.Message = "dead";
        args.Cancelled = true;
    }
}
