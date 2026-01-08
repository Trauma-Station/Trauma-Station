// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 Marcus F <199992874+thebiggestbruh@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Goobstation.Common.Temperature;

[ByRefEvent]
public record struct TemperatureImmunityEvent(float CurrentTemperature);

[ByRefEvent]
public record struct TemperatureChangeAttemptEvent(float CurrentTemperature, float LastTemperature, float Delta, bool Cancelled = false);
