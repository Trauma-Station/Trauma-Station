// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Shared.Surgery;

namespace Content.Medical.Client.Surgery;

public sealed partial class SurgeryBui : BoundUserInterface
{
    [ViewVariables] private SurgeryWindow? _window;

    public SurgeryBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _window = this.CreateWindow<SurgeryWindow>();
        _window.OnPerformStep += (part, surgery, step) => SendPredictedMessage(new SurgeryStepChosenBuiMsg(part, surgery, step));

        _window.SetOwner(Owner);
    }
}
