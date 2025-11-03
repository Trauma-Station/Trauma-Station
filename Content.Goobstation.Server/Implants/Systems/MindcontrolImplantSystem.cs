// SPDX-FileCopyrightText: 2024 Fishbait <Fishbait@git.ml>
// SPDX-FileCopyrightText: 2024 Ilya246 <57039557+Ilya246@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Piras314 <p1r4s@proton.me>
// SPDX-FileCopyrightText: 2024 fishbait <gnesse@gmail.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 Solstice <solsticeofthewinter@gmail.com>
// SPDX-FileCopyrightText: 2025 coderabbitai[bot] <136622811+coderabbitai[bot]@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Server.Implants.Components;
using Content.Goobstation.Server.Mindcontrol;
using Content.Goobstation.Shared.Mindcontrol;
using Content.Shared.Implants;
using Content.Trauma.Common.Implants;
using Robust.Shared.Containers;

namespace Content.Goobstation.Server.Implants.Systems;

public sealed class MindcontrolImplantSystem : EntitySystem
{
    [Dependency] private readonly MindcontrolSystem _mindcontrol = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MindcontrolImplantComponent, ImplanterUsedEvent>(OnImplanterUsed);
        SubscribeLocalEvent<MindcontrolImplantComponent, ImplantRemovedEvent>(OnRemoved);
    }

    private void OnImplanterUsed(Entity<MindcontrolImplantComponent> ent, ref ImplanterUsedEvent args)
    {
        if (ent.Owner != args.Implant)
            return;

        var mob = args.Target;
        var comp = EnsureComp<MindcontrolledComponent>(mob);
        comp.Master = args.User;
        _mindcontrol.Start(mob, comp);
    }

    private void OnRemoved(Entity<MindcontrolImplantComponent> ent, ref ImplantRemovedEvent args)
    {
        RemComp<MindcontrolledComponent>(args.Implanted);
    }
}
