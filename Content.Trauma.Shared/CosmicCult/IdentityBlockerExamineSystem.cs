// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Examine;
using Content.Shared.IdentityManagement.Components;

namespace Content.Trauma.Shared.CosmicCult;

/// <summary>
/// Adds a message when examining clothing that shows if the item covers your face (or parts of it)
/// </summary>
public sealed class IdentityBlockerExamineSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IdentityBlockerComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(Entity<IdentityBlockerComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("identity-blocker-examine", ("coverage", (int)ent.Comp.Coverage)));
    }
}
