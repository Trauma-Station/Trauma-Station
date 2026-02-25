// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Actions;
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Genetics.Abilities;

/// <summary>
/// Component for the chemspike transfer action.
/// Removes itself and transfers the user's bloodstream chemicals to the mob the spike is embedded in.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ChemTransferActionComponent : Component;

public sealed partial class ChemTransferActionEvent : InstantActionEvent;
