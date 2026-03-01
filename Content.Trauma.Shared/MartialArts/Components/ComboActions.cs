using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.MartialArts.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ComboActionsComponent : Component
{
    /// <summary>
    /// Maps a list of combos to entity UIs.
    /// </summary>
    [DataField]
    public Dictionary<string, EntityUid> ComboActions = new();

    /// <summary>
    /// Stores list of combos.
    /// </summary>
    [DataField]
    public List<string> StoredComboActions = new();
}
