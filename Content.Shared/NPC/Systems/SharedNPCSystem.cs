// SPDX-FileCopyrightText: 2024 Ed <96445749+TheShuEd@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 faint <46868845+ficcialfaint@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 metalgearsloth <comedian_vs_clown@hotmail.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
//
// SPDX-License-Identifier: MIT

namespace Content.Shared.NPC.Systems;

public abstract partial class SharedNPCSystem : EntitySystem
{
    /// <summary>
    /// Returns whether the given entity is an NPC.
    /// </summary>
    /// <param name="uid">Entity UID to check.</param>
    /// <returns><c>true</c> if the entity is an NPC, otherwise <c>false</c>.</returns>
    public abstract bool IsNpc(EntityUid uid);
}
