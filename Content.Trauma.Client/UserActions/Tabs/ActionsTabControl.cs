// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Client.UserActions.Tabs;

[GenerateTypedNameReferences]
public sealed partial class ActionsTabControl : BaseTabControl
{
    public ActionsTabControl()
    {
        RobustXamlLoader.Load(this);
    }

    public override bool UpdateState() => true;
}
