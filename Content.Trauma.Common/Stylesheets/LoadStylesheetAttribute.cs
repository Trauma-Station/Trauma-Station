// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Stylesheets;

/// <summary>
/// Attribute for stylesheets to be loaded by the stylesheet manager.
/// They can be gotten via <c>TryGetStylesheet</c> after loading.
/// </summary>
public sealed class LoadStylesheetAttribute : Attribute;
