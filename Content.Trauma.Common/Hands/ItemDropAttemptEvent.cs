// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Trauma.Common.Hands;

[ByRefEvent]
public record struct ItemDropAttemptEvent(bool Cancelled);
