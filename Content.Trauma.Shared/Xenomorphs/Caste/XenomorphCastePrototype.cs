// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Shared.Xenomorphs.Caste;

[Prototype]
public sealed partial class XenomorphCastePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public string Name = string.Empty;

    [DataField]
    public ProtoId<XenomorphCastePrototype>? NeedCasteDeath;

    [DataField]
    public int MaxCount;
}
