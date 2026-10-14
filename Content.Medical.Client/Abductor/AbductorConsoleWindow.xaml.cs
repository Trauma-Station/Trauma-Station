// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.UserInterface.Controls;

namespace Content.Medical.Client.Abductor;

[GenerateTypedNameReferences]
public sealed partial class AbductorConsoleWindow : FancyWindow
{
    public AbductorConsoleWindow() => RobustXamlLoader.Load(this);
}
