using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;

namespace Content.Shared.Mindshield;

public sealed partial class MindShieldSystem
{
    /// <summary>
    /// Status icon displayed in the sec HUD for broken mindshields.
    /// </summary>
    public static ProtoId<SecurityIconPrototype> BrokenStatusIcon = "MindShieldBrokenIcon";

    public void GetMindshieldStatus(EntityUid entity, out bool isMindshielded, out bool isVisible)
    {
        GetMindshieldStatus(entity, out isMindshielded, out isVisible, out _);
    }

    public bool IsShielded(EntityUid entity)
    {
        GetMindshieldStatus(entity, out var isShielded, out _);
        return isShielded;
    }
}
