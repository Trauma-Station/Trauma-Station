// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.MartialArts.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ComboActionsComponent : Component
{
    /// <summary>
    /// Maps a list of combos to entity UIs.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<ComboPrototype>, EntityUid> ComboActions = new();

    /// <summary>
    /// Stores list of combos from YAML.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<ComboPrototype>, EntProtoId> StoredComboActions = new();

    /// <summary>
    /// Queued prototype for the next strike.
    /// </summary>
    [DataField]
    public ProtoId<ComboPrototype>? QueuedPrototype;
}
