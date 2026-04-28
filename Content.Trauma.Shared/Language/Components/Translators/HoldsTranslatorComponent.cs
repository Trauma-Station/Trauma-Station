// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Language.Components.Translators;

/// <summary>
///     Applied internally to the holder of an Entity with [HandheldTranslatorComponent].
/// </summary>
[RegisterComponent]
public sealed partial class HoldsTranslatorComponent : Component
{
    [NonSerialized]
    public HashSet<Entity<HandheldTranslatorComponent>> Translators = new();
}
