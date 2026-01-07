using Content.Shared._EinsteinEngines.Language;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Knowledge;

/// <summary>
///     Trauma - for everything to make sense - this is just original LanguageKnowledgeComponent, but renamed.
///     Name of the file is unchanged to not trigger any merge conflicts.
///     Grants knowledge about languages to the entity on mapinit.
/// </summary>
[RegisterComponent]
public sealed partial class LanguageGrantComponent : Component
{
    /// <summary>
    ///     List of languages this entity can speak without any external tools.
    /// </summary>
    [DataField("speaks")]
    public List<ProtoId<LanguagePrototype>> SpokenLanguages = new();

    /// <summary>
    ///     List of languages this entity can understand without any external tools.
    /// </summary>
    [DataField("understands")]
    public List<ProtoId<LanguagePrototype>> UnderstoodLanguages = new();
}
