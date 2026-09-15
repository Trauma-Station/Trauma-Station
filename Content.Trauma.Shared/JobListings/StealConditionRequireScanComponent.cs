// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.JobListings;

/// <summary>
/// Overrides the StealConditionComponent and ignores if the steal target being held on their person.
/// Instead the objective progress is determined by the <see cref="ScanalyzerMindArchiveComponent"/> and is based off if you scanned the item.
/// </summary>
[RegisterComponent]
public sealed partial class StealConditionRequireScanComponent : Component;
