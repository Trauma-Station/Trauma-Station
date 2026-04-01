// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Trauma.Common.Mindshield;

[ByRefEvent]
public record struct RemoveMindShieldEvent(bool Cancelled = false);
