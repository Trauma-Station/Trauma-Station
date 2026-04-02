// SPDX-FileCopyrightText: 2018 PJB3005 <pieterjan.briers@gmail.com>
// SPDX-FileCopyrightText: 2018 Pieter-Jan Briers <pieterjan.briers@gmail.com>
// SPDX-FileCopyrightText: 2019 Silver <Silvertorch5@gmail.com>
// SPDX-FileCopyrightText: 2020 AJCM-git <60196617+AJCM-git@users.noreply.github.com>
// SPDX-FileCopyrightText: 2020 ComicIronic <comicironic@gmail.com>
// SPDX-FileCopyrightText: 2020 Exp <theexp111@gmail.com>
// SPDX-FileCopyrightText: 2020 GlassEclipse <tsymall5@gmail.com>
// SPDX-FileCopyrightText: 2020 Hugal31 <hugo.laloge@gmail.com>
// SPDX-FileCopyrightText: 2020 Metal Gear Sloth <metalgearsloth@gmail.com>
// SPDX-FileCopyrightText: 2020 NuclearWinter <nukeuler123@gmail.com>
// SPDX-FileCopyrightText: 2020 Remie Richards <remierichards@gmail.com>
// SPDX-FileCopyrightText: 2020 SoulSloth <67545203+SoulSloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2020 Víctor Aguilera Puerto <6766154+Zumorica@users.noreply.github.com>
// SPDX-FileCopyrightText: 2020 Víctor Aguilera Puerto <zddm@outlook.es>
// SPDX-FileCopyrightText: 2020 chairbender <kwhipke1@gmail.com>
// SPDX-FileCopyrightText: 2020 zumorica <zddm@outlook.es>
// SPDX-FileCopyrightText: 2021 20kdc <asdd2808@gmail.com>
// SPDX-FileCopyrightText: 2021 Acruid <shatter66@gmail.com>
// SPDX-FileCopyrightText: 2021 E F R <602406+Efruit@users.noreply.github.com>
// SPDX-FileCopyrightText: 2021 Fishfish458 <47410468+Fishfish458@users.noreply.github.com>
// SPDX-FileCopyrightText: 2021 Javier Guardia Fernández <DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2021 Paul <ritter.paul1+git@googlemail.com>
// SPDX-FileCopyrightText: 2021 Paul Ritter <ritter.paul1@googlemail.com>
// SPDX-FileCopyrightText: 2021 Silver <silvertorch5@gmail.com>
// SPDX-FileCopyrightText: 2021 Vera Aguilera Puerto <gradientvera@outlook.com>
// SPDX-FileCopyrightText: 2021 Vera Aguilera Puerto <zddm@outlook.es>
// SPDX-FileCopyrightText: 2021 Visne <39844191+Visne@users.noreply.github.com>
// SPDX-FileCopyrightText: 2021 fishfish458 <fishfish458>
// SPDX-FileCopyrightText: 2021 mirrorcult <notzombiedude@gmail.com>
// SPDX-FileCopyrightText: 2022 Pieter-Jan Briers <pieterjan.briers+git@gmail.com>
// SPDX-FileCopyrightText: 2022 Vera Aguilera Puerto <6766154+Zumorica@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 mirrorcult <lunarautomaton6@gmail.com>
// SPDX-FileCopyrightText: 2022 wrexbe <81056464+wrexbe@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 DrSmugleaf <DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Leon Friedrich <60421075+ElectroJr@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Nemanja <98561806+EmoGarbage404@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 ShadowCommander <10494922+ShadowCommander@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Errant <35878406+Errant-4@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Vasilis <vascreeper@yahoo.com>
// SPDX-FileCopyrightText: 2024 username <113782077+whateverusername0@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 whateverusername0 <whateveremail>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameStates;

namespace Content.Shared.Mind.Components;

/// <summary>
/// This component indicates that this entity may have mind, which is simply an entity with a <see cref="MindComponent"/>.
/// The mind entity is not actually stored in a "container", but is simply stored in nullspace.
/// </summary>
[RegisterComponent, Access(typeof(SharedMindSystem)), NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MindContainerComponent : Component
{
    /// <summary>
    ///     The mind controlling this mob. Can be null.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Mind;

    /// <summary>
    ///     True if we have a mind, false otherwise.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadOnly)]
    public bool HasMind;

    /// <summary>
    ///     Whether the mind will be put on a ghost after this component is shutdown.
    /// </summary>
    [DataField]
    public bool GhostOnShutdown = true;
}

/// <summary>
/// Base event for all other mind related events.
/// </summary>
public abstract class MindEvent : EntityEventArgs
{
    /// <summary>
    /// <see cref="MindComponent"/> entity currently being handled by the event.
    /// </summary>
    public readonly Entity<MindComponent> Mind;

    /// <summary>
    /// <see cref="MindContainerComponent"/> entity currently being handled by the event.
    /// </summary>
    public readonly Entity<MindContainerComponent> Container;

    /// <summary>
    /// The target entity in case the mind is being transferred. In <see cref="MindRemovedMessage" /> it means the entity that is being transferred to, and in <see cref="MindAddedMessage" /> it means the previous entity.
    /// Null if the mind is being added for the first time or fully removed from entities.
    /// </summary>
    public readonly EntityUid? TransferEntity;

    public MindEvent(Entity<MindComponent> mind, Entity<MindContainerComponent> container, EntityUid? transferEntity)
    {
        Mind = mind;
        Container = container;
        TransferEntity = transferEntity;
    }
}

/// <summary>
/// Event raised directed at a mind-container when a mind gets removed.
/// </summary>
/// <remarks>
/// Called after the owned entity is already set to null. TransferEntity is the entity this mind will be added to afterward, if any.
/// </remarks>
public sealed class MindRemovedMessage : MindEvent
{
    public MindRemovedMessage(Entity<MindComponent> mind, Entity<MindContainerComponent> container, EntityUid? transferEntity)
        : base(mind, container, transferEntity)
    {
    }
}

/// <summary>
/// Event raised directed at a mind when it gets removed from a mind-container.
/// </summary>
/// <remarks>
/// Called after the owned entity is already set to null. TransferEntity is the entity this mind will be added to afterward, if any.
/// </remarks>
public sealed class MindGotRemovedEvent : MindEvent
{
    public MindGotRemovedEvent(Entity<MindComponent> mind, Entity<MindContainerComponent> container, EntityUid? transferEntity)
        : base(mind, container, transferEntity)
    {
    }
}

/// <summary>
/// Event raised directed at a mind-container before a mind gets removed.
/// </summary>
/// <remarks>
/// Called before the OwnedEntity is set to null. TransferEntity is the entity this mind will be added to afterward, if any.
/// </remarks>
public sealed class BeforeMindRemovedMessage : MindEvent
{
    public BeforeMindRemovedMessage(Entity<MindComponent> mind, Entity<MindContainerComponent> container, EntityUid? transferEntity)
        : base(mind, container, transferEntity)
    {
    }
}

/// <summary>
/// Event raised directed at a mind before it gets removed from a mind-container.
/// </summary>
/// <remarks>
/// Called before the OwnedEntity is set to null. TransferEntity is the entity this mind will be added to afterward, if any.
/// </remarks>
public sealed class BeforeMindGotRemovedEvent : MindEvent
{
    public BeforeMindGotRemovedEvent(Entity<MindComponent> mind, Entity<MindContainerComponent> container, EntityUid? transferEntity)
        : base(mind, container, transferEntity)
    {
    }
}

/// <summary>
/// Event raised directed at a mind-container when a mind gets added.
/// </summary>
/// <remarks>
/// Called after the owned entity is already set to the new entity. TransferEntity is the previous entity that this mind owned, if any.
/// </remarks>
public sealed class MindAddedMessage : MindEvent
{
    public MindAddedMessage(Entity<MindComponent> mind, Entity<MindContainerComponent> container, EntityUid? transferEntity)
        : base(mind, container, transferEntity)
    {
    }
}

/// <summary>
/// Event raised directed at a mind when it gets added to a mind-container.
/// </summary>
/// <remarks>
/// Called after the owned entity is already set to the new entity. TransferEntity is the previous entity that this mind owned, if any.
/// </remarks>
public sealed class MindGotAddedEvent : MindEvent
{
    public MindGotAddedEvent(Entity<MindComponent> mind, Entity<MindContainerComponent> container, EntityUid? transferEntity)
        : base(mind, container, transferEntity)
    {
    }
}
