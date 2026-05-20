// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Client.Knowledge;

[ByRefEvent]
public record struct GetAttributeModifierEvent(List<(string Label, string Value)> Modifiers);
