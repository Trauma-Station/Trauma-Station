// <Trauma>
using Content.Trauma.Common.Language;
// </Trauma>
using Content.Shared.Body;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Chemistry.Components;
using Content.Shared.Damage;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Roles;
using Content.Shared.StatusIcon;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

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
