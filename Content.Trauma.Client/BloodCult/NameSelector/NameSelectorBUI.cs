// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.BloodCult.UI;

namespace Content.Trauma.Client.BloodCult.NameSelector;

public sealed class NameSelectorBUI : BoundUserInterface
{
    public NameSelectorBUI(EntityUid owner, Enum key) : base(owner, key)
    {
        var window = this.CreateWindow<NameSelectorWindow>();
        window.OnSelected += name =>
        {
            SendPredictedMessage(new NameSelectedMessage(name));
            Close();
        };
    }
}
