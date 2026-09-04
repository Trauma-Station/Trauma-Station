// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Server.Antag;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Chat.Systems;
using Content.Server.Communications;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Atmos.Components;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Clothing.Components;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Content.Shared.Stunnable;
using Content.Shared.Temperature.Systems;
using Content.Trauma.Common.CollectiveMind;
using Content.Trauma.Shared.Magic.Demonologist;
using Content.Trauma.Shared.Magic.Demonologist.Components;
using Content.Trauma.Shared.Magic.Demonologist.Events;
using Content.Trauma.Shared.Magic.Demonologist.UI;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.Magic.Demonologist;

public sealed partial class DemonologistSystem : SharedDemonologistSystem
{
    [Dependency] private SharedAccessSystem _access = default!;
    [Dependency] private AntagSelectionSystem _antag = default!;
    [Dependency] private FlammableSystem _flammable = default!;
    [Dependency] private SharedIdCardSystem _idCard = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private RoleSystem _roles = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private SharedTemperatureSystem _temperature = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private UserInterfaceSystem _uiSystem = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private ChatSystem _chat = default!;

    private readonly Dictionary<EntityUid, (HashSet<ProtoId<AccessLevelPrototype>> saved, TimeSpan restoreAt)> _cursedAccess = new();


    private const float UIUpdateInterval = 5.0f;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var toRestore = new List<EntityUid>();

        foreach (var (idCard, data) in _cursedAccess)
        {
            if (!Exists(idCard) || now >= data.restoreAt)
                toRestore.Add(idCard);
        }

        foreach (var idCard in toRestore)
        {
            if (_cursedAccess.Remove(idCard, out var data) && Exists(idCard) && TryComp<AccessComponent>(idCard, out _))
                _access.TrySetTags(idCard, data.saved.ToList());
        }

        var query = EntityQueryEnumerator<DemonologistComponent, CommunicationsConsoleComponent>();
        while (query.MoveNext(out var uid, out var demo, out _))
        {
            demo.UIUpdateAccumulator += frameTime;

            if (demo.UIUpdateAccumulator < UIUpdateInterval)
                continue;

            demo.UIUpdateAccumulator -= UIUpdateInterval;

            if (_uiSystem.IsUiOpen(uid, DemonicAnnouncementUiKey.Key))
                UpdateDemonicConsoleInterface((uid, demo));
        }
    }

    [SubscribeLocalEvent]
    private void OnBloodBoil(BloodBoilSpellEvent args)
    {
        _temperature.ChangeHeat(args.Target, 350000f, true);
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnCombustion(CombustionSpellEvent args)
    {
        if (!TryComp<FlammableComponent>(args.Target, out var flammable))
            return;

        _flammable.AdjustFireStacks(args.Target, flammable!.MaximumFireStacks, flammable, ignite: true);
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnCursedAccess(CursedAccessSpellEvent args)
    {
        if (!_idCard.TryFindIdCard(args.Target, out var id))
        {
            args.Handled = true;
            return;
        }

        if (!TryComp<AccessComponent>(id, out var access))
        {
            args.Handled = true;
            return;
        }

        _cursedAccess[id] = (new HashSet<ProtoId<AccessLevelPrototype>>(access.Tags), _timing.CurTime + TimeSpan.FromSeconds(20));

        _access.TrySetTags(id, new List<ProtoId<AccessLevelPrototype>>());
        _stun.TryUpdateParalyzeDuration(args.Target, TimeSpan.FromSeconds(2));

        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnBindApprentice(BindApprenticeSpellEvent ev)
    {
        if (HasComp<DemonologistApprenticeComponent>(ev.Target) || HasComp<DemonologistComponent>(ev.Target))
            return;

        if (!_mind.TryGetMind(ev.Target, out var mindId, out _))
            return;

        _stun.TryUpdateParalyzeDuration(ev.Target, TimeSpan.FromSeconds(2));

        EnsureComp<DemonologistApprenticeComponent>(ev.Target);
        _antag.SendBriefing(ev.Target,
            Loc.GetString("demonologist-apprentice-role-greeting"),
            Color.FromHex("#990000"),
            new SoundPathSpecifier("/Audio/_Trauma/Demonologist/demonologist.ogg"));
        _roles.MindAddRole(mindId, "DemonologistApprenticeMindRole");

        var collectiveMind = EnsureComp<CollectiveMindComponent>(ev.Target);
        collectiveMind.DefaultChannel = "Demonmind";
        collectiveMind.Channels.Add("Demonmind");
        Dirty(ev.Target, collectiveMind);

        SetGear(ev.Target, ev.Gear);

        ev.Handled = true;
    }

    private void SetGear(EntityUid uid, Dictionary<string, EntProtoId> gear, bool force = true)
    {
        if (!TryComp(uid, out InventoryComponent? inventoryComponent))
            return;

        foreach (var (slot, item) in gear)
        {
            _inventory.TryUnequip(uid, slot, true, force, false, inventoryComponent);

            var ent = Spawn(item, Transform(uid).Coordinates);
            if (!_inventory.TryEquip(uid, ent, slot, true, force, false, inventoryComponent))
            {
                Del(ent);
                continue;
            }

            if (slot == "id" &&
                TryComp(ent, out PdaComponent? pdaComponent) &&
                TryComp<IdCardComponent>(pdaComponent.ContainedId, out var idCard))
                idCard.FullName = MetaData(uid).EntityName;

            if (HasComp<ClothingComponent>(ent))
                EnsureComp<UnremoveableComponent>(ent);
        }
    }

    [SubscribeLocalEvent]
    private void OnDemonicConsoleMapInit(Entity<DemonologistComponent> ent, ref MapInitEvent args)
    {
        UpdateDemonicConsoleInterface(ent);
    }

    private void UpdateDemonicConsoleInterface(Entity<DemonologistComponent> ent)
    {
        if (!TryComp<CommunicationsConsoleComponent>(ent, out var console))
            return;

        _uiSystem.SetUiState(ent.Owner, DemonicAnnouncementUiKey.Key,
            new DemonicAnnouncementConsoleState(console.AnnouncementCooldownRemaining <= 0f));
    }

    [SubscribeLocalEvent]
    private void OnDemonicAnnounce(Entity<DemonologistComponent> ent, ref DemonicAnnouncementMessage args)
    {
        if (!TryComp<CommunicationsConsoleComponent>(ent, out var console))
            return;

        if (console.AnnouncementCooldownRemaining > 0f)
            return;

        var maxLength = _cfg.GetCVar(CCVars.ChatMaxAnnouncementLength);
        var msg = SharedChatSystem.SanitizeAnnouncement(args.Message, maxLength);

        console.AnnouncementCooldownRemaining = console.Delay;

        Loc.TryGetString(console.Title, out var title);
        title ??= console.Title;

        _chat.DispatchGlobalAnnouncement(msg, title, colorOverride: console.Color);

        UpdateDemonicConsoleInterface(ent);
    }
}
