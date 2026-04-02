// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Goobstation.Server.Virology;

[ByRefEvent]
public record struct VirologyMachineCheckEvent(bool Cancelled = false);

public record struct VirologyMachineDoneEvent(bool Success);
