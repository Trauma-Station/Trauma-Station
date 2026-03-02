// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using Content.Goobstation.Shared.Serialization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Goobstation.Shared.MisandryBox;

[Prototype]
public sealed partial class AccountAppendPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = null!;

    [DataField(required: true, customTypeSerializer: typeof(GuidSerializer))]
    public Guid Userid = Guid.Empty;

    [DataField(required: true)]
    public ComponentRegistry Components = default!;
}
