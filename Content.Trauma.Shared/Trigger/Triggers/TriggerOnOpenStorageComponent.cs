// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Trigger.Components.Triggers;

namespace Content.Trauma.Shared.Trigger.Triggers;

/// <summary>
/// Trigger when this entity's storage is opened.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class TriggerOnOpenStorageComponent : BaseTriggerOnXComponent;
