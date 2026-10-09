// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.JobListings;

/// <summary>
/// When this is on an uplink entity it forbids opening the job board.
/// </summary>
[RegisterComponent]
public sealed partial class HiddenJobListingsComponent : Component;