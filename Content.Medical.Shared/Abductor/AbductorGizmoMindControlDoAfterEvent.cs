using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Medical.Shared.Abductor;

[Serializable, NetSerializable]
public sealed partial class AbductorGizmoMindControlDoAfterEvent : SimpleDoAfterEvent { }
