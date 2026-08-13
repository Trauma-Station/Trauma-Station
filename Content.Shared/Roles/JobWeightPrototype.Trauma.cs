using Robust.Shared.Prototypes;

namespace Content.Shared.Roles;

public sealed partial class JobWeightPrototype : IPrototype
{
    /// <summary>
    /// How many of each job to try pick from assistants with the job at least set to Low.
    /// These are replaced with default as a whole rather than per job like <see cref="Weights"/>.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<JobPrototype>, int> Required { get; private set; } = new();
}
