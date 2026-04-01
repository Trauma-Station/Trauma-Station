using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Trauma.Common.Mindshield;

[ByRefEvent]
public record struct RemoveMindShieldEvent(bool Cancelled = false);
