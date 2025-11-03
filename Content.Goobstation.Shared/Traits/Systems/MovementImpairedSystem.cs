// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 Solstice <solsticeofthewinter@gmail.com>
// SPDX-FileCopyrightText: 2025 SolsticeOfTheWinter <solsticeofthewinter@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Traits.Assorted;

namespace Content.Goobstation.Shared.Traits.Systems;

public sealed partial class MovementImpairedSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ImpairedMobilityComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<ImpairedMobilityComponent> ent, ref ExaminedEvent args)
    {
        if (args.IsInDetailsRange)
            args.PushMarkup(Loc.GetString("movement-impaired-trait-examined", ("target", Identity.Entity(ent, EntityManager))));
    }
}
