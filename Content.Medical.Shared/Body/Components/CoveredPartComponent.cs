// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Medical.Shared.Body;

/// <summary>
/// Component for bodyparts to track what armor is covering them.
/// Used for per-limb explosion protection, could do more with it in the future.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(CoveredPartSystem))]
[AutoGenerateComponentState]
public sealed partial class CoveredPartComponent : Component
{
    /// <summary>
    /// Armor pieces covering this part.
    /// Modified when armor is worn/taken off but not if something somehow changed its coverage.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<EntityUid> Covered = new();
}
