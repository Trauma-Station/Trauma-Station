// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Trauma.Common.Knockdown;

/// <summary>
/// Raised on the entity that is being knocked down by an attacking entity trying to knockdown the entity.
/// </summary>
public record struct KnockdownOnCollideAttemptEvent(EntityUid Attacker, bool Cancelled = false);
