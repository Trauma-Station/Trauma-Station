// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Mind;
using Content.Shared.StatusIcon;

namespace Content.Trauma.Shared.BloodCult;

/// <summary>
/// Component added to blood cultists and the leader.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BloodCultistComponent : Component
{
    public override bool SessionSpecific => true;

    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> ActiveSpells = new();

    [DataField]
    public Color? OriginalEyeColor;

    [DataField]
    public int SpellsLimit = 3;
}
