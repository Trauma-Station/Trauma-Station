namespace Content.Trauma.Shared.Kudzu;

/// <summary>
/// Counts as foliage, and therefore gets a lower layer if the entity seeing them has the FoliageIgnoringVision Component.
/// </summary>
[RegisterComponent]
public sealed partial class IsFoliageComponent : Component;
