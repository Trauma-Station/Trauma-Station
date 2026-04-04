// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Chemistry.Components;

namespace Content.Trauma.Shared.Plumbing;

public interface ISolutionMixtureHolder
{
    public Solution Liquid { get; set; }
}
