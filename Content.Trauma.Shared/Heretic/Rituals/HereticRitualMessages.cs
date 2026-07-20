// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Polymorph;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Lock;

namespace Content.Trauma.Shared.Heretic.Rituals;

[Serializable, NetSerializable]
public sealed class HereticRitualMessage(NetEntity ritual) : BoundUserInterfaceMessage
{
    public NetEntity Ritual = ritual;
}

[Serializable, NetSerializable]
public enum HereticRitualRuneUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class HereticShapeshiftMessage(ProtoId<PolymorphPrototype> protoId) : BoundUserInterfaceMessage
{
    public ProtoId<PolymorphPrototype> ProtoId = protoId;
}


[Serializable, NetSerializable]
public enum HereticShapeshiftUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class EldritchIdMessage(EldritchIdConfiguration config) : BoundUserInterfaceMessage
{
    public EldritchIdConfiguration Config = config;
}

[Serializable, NetSerializable]
public enum EldritchIdUiKey : byte
{
    Key
}
