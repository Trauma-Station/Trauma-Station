// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Wizard;

public abstract partial class CommonWizardSystem : EntitySystem
{
    public abstract bool IsChunni(EntityUid? eyepatch);
    public abstract bool IsMovementBlocked(EntityUid? wizard);
}
