// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Armor;
using Content.Trauma.Common.Armor;

namespace Content.Trauma.Shared.Armor;

public sealed partial class ArmorPointBlankSystem : EntitySystem
{
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArmorPointBlankComponent, ArmorProtectAttemptEvent>(OnProtectAttempt);
        SubscribeLocalEvent<ArmorPointBlankComponent, ArmorExamineEvent>(OnArmorExamine);
    }

    private void OnProtectAttempt(Entity<ArmorPointBlankComponent> ent, ref ArmorProtectAttemptEvent args)
    {
        if (!args.IsPreciseHit || args.Multiplier <= 0f || ent.Comp.Range <= 0f || args.Origin is not { } origin)
            return;

        if (_transform.InRange(ent.Owner, origin, ent.Comp.Range))
            args.Multiplier *= ent.Comp.ResistancePenalty;
    }

    private void OnArmorExamine(Entity<ArmorPointBlankComponent> ent, ref ArmorExamineEvent args)
    {
        if (ent.Comp.Range <= 0f)
            return;

        args.Msg.PushNewline();
        args.Msg.AddMarkupOrThrow($"Protection is [color=red]multplied by {ent.Comp.ResistancePenalty}x[/color] within a {ent.Comp.Range}m range on precise hit");
    }
}
