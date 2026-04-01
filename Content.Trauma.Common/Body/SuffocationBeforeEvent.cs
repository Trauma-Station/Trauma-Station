// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Body;

[ByRefEvent]
public record struct SuffocationBeforeEvent(bool Cancelled = false);
