// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.JobListings;

/// <summary>
/// When this is on an uplink host, like a pda or uplink, it forbids opening the job board.
/// </summary>
[RegisterComponent]
public sealed partial class HiddenJobListingsComponent : Component;