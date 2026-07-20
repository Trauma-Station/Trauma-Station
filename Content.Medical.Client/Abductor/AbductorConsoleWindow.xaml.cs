// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Client.UserInterface.CustomControls;

namespace Content.Medical.Client.Abductor;

[GenerateTypedNameReferences]
public sealed partial class AbductorConsoleWindow : DefaultWindow
{
    public AbductorConsoleWindow() => RobustXamlLoader.Load(this);
}
