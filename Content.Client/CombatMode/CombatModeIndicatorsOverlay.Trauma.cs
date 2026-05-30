using Content.Medical.Common.Targeting;
using Content.Trauma.Common.Vampires;
using Robust.Client.Graphics;
using Robust.Client.Player;

namespace Content.Client.CombatMode;

public sealed partial class CombatModeIndicatorsOverlay
{
    private readonly IPlayerManager _player;

    private readonly Texture _bloodSuck;

    public bool IsBloodsucking()
    {
        var entity = _player.LocalEntity;
        return entity is { } player
               && _entMan.HasComponent<VampireBloodsuckingComponent>(entity)
               && _entMan.TryGetComponent<TargetingComponent>(entity, out var target)
               && _hands.ActiveHandIsEmpty(player)
               && target.Target == TargetBodyPart.Head;
    }
}
