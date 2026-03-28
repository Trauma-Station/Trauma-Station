// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Examine;
using Content.Shitcode.Common.Hands;
using Content.Shitcode.Common.Wizard;

namespace Content.Shitcode.Shared.Wizard.ArcaneBarrage;

public sealed class DeleteOnDropAttemptSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeleteOnDropAttemptComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<DeleteOnDropAttemptComponent, ItemDropAttemptEvent>(OnDroppedItem);
    }

    private void OnExamine(Entity<DeleteOnDropAttemptComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("delete-on-drop-attempt-comp-examine"));
    }

    private void OnDroppedItem(Entity<DeleteOnDropAttemptComponent> ent, ref ItemDropAttemptEvent args)
    {
        PredictedQueueDel(ent);
        args.Cancelled = true;
    }
}
