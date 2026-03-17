using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shitcode.Common.Hands;

[ByRefEvent]
public record struct ItemDropAttemptEvent(bool Cancelled);
