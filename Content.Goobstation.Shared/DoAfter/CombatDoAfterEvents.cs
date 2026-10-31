// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DoAfter;

namespace Content.Goobstation.Shared.DoAfter;

[Serializable, NetSerializable]
public sealed partial class CombatDoAfterEvent : SimpleDoAfterEvent;

[ImplicitDataDefinitionForInheritors]
public abstract partial class BaseCombatDoAfterSuccessEvent : EntityEventArgs;

public sealed partial class CombatDoAfterThrownEvent : BaseCombatDoAfterSuccessEvent;
