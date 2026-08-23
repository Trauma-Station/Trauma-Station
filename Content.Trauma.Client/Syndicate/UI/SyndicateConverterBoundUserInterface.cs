// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Syndicate.Components;

namespace Content.Trauma.Client.Syndicate.UI;

public sealed class SyndicateConverterBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private SyndicateConverterMenu? _menu;

    public SyndicateConverterBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<SyndicateConverterMenu>();
        _menu.SetEntity(Owner);

        _menu.ConvertButtonPressed += () =>
        {
            SendMessage(new SyndicateConverterStartPackBuiMessage());
        };

        _menu.OpenCentered();
    }
}

