// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;

namespace Content.Trauma.Shared.Guardian;

/// <summary>
/// Fired when the lightning guardian zaps a target with its bolt of lightning.
/// </summary>
public sealed partial class GuardianLightningBoltEvent : EntityTargetActionEvent
{
}
