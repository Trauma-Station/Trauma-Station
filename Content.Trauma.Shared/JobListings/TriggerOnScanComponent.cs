// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Trigger.Components.Triggers;

namespace Content.Trauma.Shared.JobListings;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnScanComponent : BaseTriggerOnXComponent;
