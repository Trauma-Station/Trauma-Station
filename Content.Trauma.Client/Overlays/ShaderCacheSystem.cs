// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Client.Overlays;

public sealed partial class ShaderCacheSystem : EntitySystem
{
    public ShaderInstance GetOrCreateShader(EntityUid uid, string id, ProtoId<ShaderPrototype> proto)
    {
        var comp = EnsureComp<ShaderCacheComponent>(uid);
        if (!comp.Cache.TryGetValue(id, out var shader))
        {
            shader = ProtoMan.Index(proto).InstanceUnique();
            comp.Cache.Add(id, shader);
        }

        return shader;
    }

    public void RemoveShader(EntityUid uid, string id)
    {
        if (!TryComp(uid, out ShaderCacheComponent? cache))
            return;

        cache.Cache.Remove(id);
        if (cache.Cache.Count == 0)
            RemComp(uid, cache);
    }
}
