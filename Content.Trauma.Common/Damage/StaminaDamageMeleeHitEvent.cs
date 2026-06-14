// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Common.Damage;

public sealed class StaminaDamageMeleeHitEvent(List<EntityUid> hitEntities, Vector2? direction) : EntityEventArgs
{
    public List<EntityUid> HitEntities = hitEntities;

    public Vector2? Direction = direction;
}
