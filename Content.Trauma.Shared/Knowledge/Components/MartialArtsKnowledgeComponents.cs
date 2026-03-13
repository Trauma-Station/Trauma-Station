// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Trauma.Shared.MartialArts.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MartialArtsKnowledgeComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Blocked;

    /// <summary>
    /// Set to false to disable gaining XP by performing combos.
    /// </summary>
    [DataField]
    public bool GiveExperience = true;

    [DataField, AutoNetworkedField]
    public int TemporaryBlockedCounter;

    [DataField(required: true)]
    public SpriteSpecifier Icon;
}
