using Content.Client.Hands.Systems;
using Content.Client.Popups;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Trauma.Common.Heretic;
using Robust.Client.UserInterface;
using Robust.Shared.Input;

namespace Content.Client.ContextMenu.UI;

public sealed partial class EntityMenuUIController
{
    [UISystemDependency] private readonly HandsSystem _hands = default!;
    [UISystemDependency] private readonly PopupSystem _popup = default!;

    private bool ShouldContextMenuClick(BoundKeyFunction function, EntityUid entity)
    {
        if (function != EngineKeyFunctions.Use || _playerManager.LocalEntity is not { } player)
            return true;

        if (!_hands.TryGetActiveItem(player, out var used))
            return true;

        var ev = new ShouldBlockContextMenuEvent(entity);
        _entityManager.EventBus.RaiseLocalEvent(used.Value, ref ev);
        if (!ev.ShouldBlock)
            return true;

        _popup.PopupClient(Loc.GetString("block-context-menu-message",
            ("entity", Identity.Entity(entity, _entityManager, player)),
            ("item", used.Value)),
            player,
            player,
            PopupType.SmallCaution);
        return false;
    }
}
