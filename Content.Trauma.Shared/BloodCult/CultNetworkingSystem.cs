namespace Content.Trauma.Shared.BloodCult;

/// <summary>
/// Only networks some cult member components to cultists.
/// </summary>
public sealed partial class CultNetworkingSystem : EntitySystem
{
    [Dependency] private BloodCultSystem _cult = default!;

    [SubscribeLocalEvent]
    private void OnCultistGetStateAttempt(Entity<BloodCultistComponent> ent, ref ComponentGetStateAttemptEvent args)
    {
        Handle(ref args);
    }

    [SubscribeLocalEvent]
    private void OnMemberGetStateAttempt(Entity<BloodCultMemberComponent> ent, ref ComponentGetStateAttemptEvent args)
    {
        Handle(ref args);
    }

    [SubscribeLocalEvent]
    private void OnLeaderGetStateAttempt(Entity<BloodCultLeaderComponent> ent, ref ComponentGetStateAttemptEvent args)
    {
        Handle(ref args);
    }

    // TODO: if someone makes a multi cult gamemode, pass the ent and only network it between the same cult rule as this entity
    private void Handle(ref ComponentGetStateAttemptEvent args)
    {
        // dont troll replays "networking"
        if (args.Player?.AttachedEntity is not { } mob)
            return;

        args.Cancelled = !_cult.IsCultist(mob);
    }
}
