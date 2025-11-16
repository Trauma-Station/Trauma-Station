using Content.Trauma.Common.Materials;

namespace Content.Shared.Materials;

public sealed partial class MaterialPrototype
{
    /// <summary>
    /// The physical properties of this material, for use with nuclear reactors.
    /// </summary>
    [DataField]
    public MaterialProperties? Properties;
}
