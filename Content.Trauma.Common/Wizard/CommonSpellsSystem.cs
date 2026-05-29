// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Trauma.Common.Wizard;

public abstract partial class CommonSpellsSystem : EntitySystem
{
    public abstract event Action? StopTargeting;

    public abstract void SetSwapSecondaryTarget(EntityUid user, EntityUid? target, EntityUid action);
}
