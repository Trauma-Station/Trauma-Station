namespace Content.Shared.Gatherable.Components;

public sealed partial class GatherableComponent : Component
{
    /// <summary>
    ///     Whether the resource has been gathered or not.
    /// </summary>
    /// <remarks>
    ///     HEY KIDDOS, DID YOU KNOW THAT IF YOU HIT A SINGLE ROCK WITH TWO DIFFERENT PROJECTILES AT THE SAME TIME, IT SPAWNS TWICE AS MANY THINGS??? I FUCKING HATE THIS SHITCODE
    /// </remarks>
    [DataField]
    public bool Gathered = false;
}
