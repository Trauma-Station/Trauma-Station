// SPDX-License-Identifier: AGPL-3.0-or-later
namespace Content.Trauma.Shared.Genetics.Abilities;

public sealed class AutotomyMutationSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AutotomyMutationComponent, AutotomyActionEvent>(OnAction);
    }

    private void OnAction(Entity<AutotomyMutationComponent> ent, ref AutotomyActionEvent args)
    {
        args.Handled = true;
        // TODO:
        // get parts that arent chest head or groin
        // detach a random part if it exists
    }
}
