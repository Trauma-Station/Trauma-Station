using Robust.Shared.Prototypes;

namespace Content.Shared.Botany.Systems;

public sealed partial class BotanySystem
{
    public bool PlantHasComp<T>(EntityUid? snapshot, EntProtoId? plantProtoId)
        where T : IComponent, new()
    {
        if (snapshot != null && HasComp<T>(snapshot))
            return true;

        if (plantProtoId is not { } id)
            return false;

        if (!ProtoMan.Resolve(id, out var proto))
            return false;

        return proto.HasComp<T>(Factory);
    }
}
