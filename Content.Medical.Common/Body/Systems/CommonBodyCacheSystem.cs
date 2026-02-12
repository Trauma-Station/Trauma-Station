// SPDX-License-Identifier: AGPL-3.0-or-later
namespace Content.Medical.Common.Body;

/// <summary>
/// Provides API for any module to look for cached organs in a body.
/// </summary>
public abstract class CommonBodyCacheSystem : EntitySystem
{
    public abstract EntityUid? GetOrgan(EntityUid body, [ForbidLiteral] string category);
}
