// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Mobs;
using Content.Shared.Smoking;
using Content.Shared.Verbs;
using Content.Trauma.Common.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Goobstation.Shared.HoloCigar;

/// <summary>
/// This is the system for the Holo-Cigar. - pure unadulterated shitcode below beware
/// </summary>
public sealed class HoloCigarSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly ClothingSystem _clothing = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    private const string LitPrefix = "lit";
    private const string UnlitPrefix = "unlit";
    private const string MaskSlot = "mask";

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<HoloCigarComponent, GetVerbsEvent<AlternativeVerb>>(OnAddInteractVerb);

        SubscribeLocalEvent<TheManWhoSoldTheWorldComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<TheManWhoSoldTheWorldComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<TheManWhoSoldTheWorldComponent, MobStateChangedEvent>(OnMobStateChangedEvent);
    }

    private void OnAddInteractVerb(Entity<HoloCigarComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands is null)
            return;

        AlternativeVerb verb = new()
        {
            Act = () => Toggle(ent),
            Message = Loc.GetString("holo-cigar-verb-desc"),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/clock.svg.192dpi.png")),
            Text = Loc.GetString("holo-cigar-verb-text"),
        };

        args.Verbs.Add(verb);
    }

    #region Event Methods

    private void OnMobStateChangedEvent(Entity<TheManWhoSoldTheWorldComponent> ent, ref MobStateChangedEvent args)
    {
        if (!TryComp<HoloCigarComponent>(ent.Comp.HoloCigarEntity, out var holoCigarComponent))
            return;

        if (args.NewMobState != MobState.Dead)
            return;

        _audio.Stop(holoCigarComponent.MusicEntity); // no music out of mouth duh

        _audio.PlayPredicted(ent.Comp.DeathAudio, ent, ent, AudioParams.Default.WithVolume(3f));
    }

    private void OnComponentShutdown(Entity<TheManWhoSoldTheWorldComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<HoloCigarComponent>(ent.Comp.HoloCigarEntity, out var holoCigarComponent))
            return;

        _audio.Stop(holoCigarComponent.MusicEntity);
    }

    private void OnMapInit(Entity<TheManWhoSoldTheWorldComponent> ent, ref MapInitEvent args)
    {
        if (!_inventory.TryGetSlotEntity(ent, MaskSlot, out var cigarEntity) ||
            !HasComp<HoloCigarComponent>(cigarEntity))
            return;
        ent.Comp.HoloCigarEntity = cigarEntity;
    }

    private void Toggle(Entity<HoloCigarComponent> ent)
    {
        ent.Comp.Lit = !ent.Comp.Lit;
        Dirty(ent);

        var state = ent.Comp.Lit ? SmokableState.Lit : SmokableState.Unlit;
        var prefix = ent.Comp.Lit ? LitPrefix : UnlitPrefix;

        _appearance.SetData(ent, SmokingVisuals.Smoking, state);
        _clothing.SetEquippedPrefix(ent, prefix);
        _item.SetHeldPrefix(ent, prefix);

        if (!ent.Comp.Lit)
        {
            ent.Comp.MusicEntity = _audio.Stop(ent.Comp.MusicEntity);
            return;
        }

        // playing is not predicted as it spawns phantom audio for some reason
        if (_net.IsClient || _audio.PlayPvs(ent.Comp.Music, ent)?.Entity is not {} audio)
            return;

        EnsureComp<CopyrightedAudioComponent>(audio); // even a midi can cause copyright claims
        ent.Comp.MusicEntity = audio;
    }

    #endregion
}
