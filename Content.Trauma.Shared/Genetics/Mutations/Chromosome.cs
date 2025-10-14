namespace Content.Trauma.Shared.Genetics.Mutations;

/// <summary>
/// A chromosome that can be added to any mutation to enhance it in some way.
/// Taken verbatim from the /tg/ wiki
/// </summary>
public enum Chromosome : byte
{
    Synchronizer, // reduces some downsides by 50%
    Stabilizer, // reduces instability gained by 20%
    Power, // boosts strength of some mutations
    Energetic // lowers the cooldown for ActionMutation
}
