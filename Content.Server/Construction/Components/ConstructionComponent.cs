// <Trauma>
using Content.Goobstation.Common.Construction;
// </Trauma>
using Content.Shared.Construction.Prototypes;
using Content.Shared.DoAfter;
using Robust.Shared.Prototypes;

namespace Content.Server.Construction.Components
{
    [RegisterComponent] // Trauma - removed Access
    public sealed partial class ConstructionComponent : SharedConstructionComponent // Goobstation - extend shared component
    {
        [DataField(required: true)]
        public ProtoId<ConstructionGraphPrototype> Graph;

        [DataField(required: true)]
        public string Node = string.Empty;

        [DataField("edge")]
        public int? EdgeIndex;

        [DataField("step")]
        public int StepIndex;

        [DataField]
        public HashSet<string> Containers = new();

        [DataField("defaultTarget")]
        public string? TargetNode;

        [ViewVariables]
        public int? TargetEdgeIndex;

        [ViewVariables]
        public Queue<string>? NodePathfinding;

        [DataField("deconstructionTarget")]
        public string? DeconstructionNode = "start";

        [ViewVariables]
        // TODO Force flush interaction queue before serializing to YAML.
        // Otherwise you can end up with entities stuck in invalid states (e.g., waiting for DoAfters).
        public readonly Queue<object> InteractionQueue = new();
    }
}
