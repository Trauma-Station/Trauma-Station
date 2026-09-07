// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Blob.Components;

namespace Content.Goobstation.Client.Blob;

public sealed class BlobChemSwapBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private BlobChemSwapMenu? _menu;

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<BlobChemSwapMenu>();
        _menu.SetOwner(Owner);
        _menu.OnSetChem += chem => SendPredictedMessage(new BlobSetChemMessage(chem));
    }
}
