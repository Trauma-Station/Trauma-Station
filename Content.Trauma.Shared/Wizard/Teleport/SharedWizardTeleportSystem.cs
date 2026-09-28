// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.UserInterface;

namespace Content.Trauma.Shared.Wizard.Teleport;

public abstract class SharedWizardTeleportSystem : EntitySystem
{
    public virtual void OnTeleportSpell(EntityUid performer, EntityUid action)
    {
    }
}
