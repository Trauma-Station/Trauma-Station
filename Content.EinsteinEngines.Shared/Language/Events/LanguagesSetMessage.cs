using Robust.Shared.Serialization;

namespace Content.EinsteinEngines.Shared.Language.Events;

/// <summary>
///     Sent from the client to the server when it needs to want to set his currentLanguage.
/// </summary>
[Serializable, NetSerializable]
public sealed class LanguagesSetMessage(string currentLanguage) : EntityEventArgs
{
    public string CurrentLanguage = currentLanguage;
}
