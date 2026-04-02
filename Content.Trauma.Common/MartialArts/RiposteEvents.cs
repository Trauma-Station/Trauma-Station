// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;

namespace Content.Trauma.Common.MartialArts;

[ImplicitDataDefinitionForInheritors]
public abstract partial class BaseRiposteCheckEvent : HandledEntityEventArgs;

public sealed partial class CanDoCQCEvent : BaseRiposteCheckEvent
{
    public readonly EntProtoId Form;

    public CanDoCQCEvent(EntProtoId form) : base()
    {
        Form = form;
    }
}
