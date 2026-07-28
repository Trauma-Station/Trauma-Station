using Robust.Shared.Prototypes;

namespace Content.Server.Revolutionary;

/// <summary>
/// Component for headrev that needs a fake mindshield
/// </summary>
[RegisterComponent, Access(typeof(MindshieldedHeadRevSystem))]
public sealed partial class MindshieldedHeadRevComponent : Component
{
    [DataField]
    public EntProtoId FakeMindShieldImplant = "FakeMindShieldImplant";
}
