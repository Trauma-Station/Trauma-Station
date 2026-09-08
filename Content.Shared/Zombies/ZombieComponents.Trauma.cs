using Content.Trauma.Common.Language;
using Robust.Shared.Prototypes;

namespace Content.Shared.Zombies;
public sealed partial class ZombieComponent : Component
{
    /// <summary>
    ///     This is the forced language a zombie should have when they are zombified and try to speak.
    /// </summary>
    /// <remarks>
    ///     This is intended as a fallback to prevent zombies from using sign language or any other
    ///     language that bypasses accent filter, and it prevents them from understanding everything
    ///     else while being zombified.
    /// </remarks>
    [DataField]
    public ProtoId<LanguagePrototype> ForcedLanguage = "Zombish";
}
