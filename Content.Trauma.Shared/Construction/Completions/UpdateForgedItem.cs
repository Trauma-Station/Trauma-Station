// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Construction;
using Content.Trauma.Shared.Forging;

namespace Content.Trauma.Shared.Construction.Completions;

/// <summary>
/// Changes the construction entity to be the forged item's finished version.
/// </summary>
public sealed partial class FinishForgedItem : IGraphAction
{
    private ForgingSystem? _forging;

    public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entMan)
    {
        _forging ??= entMan.System<ForgingSystem>();

        _forging.FinishForgedItem(uid, userUid);
    }
}
