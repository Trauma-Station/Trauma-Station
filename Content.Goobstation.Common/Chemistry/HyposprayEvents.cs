// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Ted Lukin <66275205+pheenty@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Goobstation.Common.Chemistry;

/// <summary>
/// Raised on a hypospray when it successfully injects.
/// </summary>
[ByRefEvent]
public readonly record struct AfterHyposprayInjectsEvent(EntityUid User, EntityUid Target);
