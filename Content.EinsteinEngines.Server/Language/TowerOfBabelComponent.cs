using Content.EinsteinEngines.Shared.Language;
using Robust.Shared.Prototypes;

namespace Content.EinsteinEngines.Server.Language;

[RegisterComponent]
public sealed partial class TowerOfBabelComponent : Component
{
    [DataField]
    public ProtoId<LanguagePrototype> DefaultLanguage = "TauCetiBasic";
}
