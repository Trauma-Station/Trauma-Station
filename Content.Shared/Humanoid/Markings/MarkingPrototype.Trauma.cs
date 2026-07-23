namespace Content.Shared.Humanoid.Markings;

public sealed partial class MarkingPrototype
{
    /// <summary>
    /// Suffixes of child markings IDs that will be applied when this marking is applied
    /// For example, suffix "Behind" of marking "MothWingsDefault" will apply marking "MothWingsDefaultBehind"
    /// </summary>
    [DataField]
    public List<string> ChildMarkingsSuffix = new();
}
