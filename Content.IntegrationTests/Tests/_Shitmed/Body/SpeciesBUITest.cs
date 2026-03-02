// SPDX-FileCopyrightText: 2024 gluesniffler <159397573+gluesniffler@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Shitmed.Body;

[TestFixture]
public sealed class SpeciesBUiTest
{
    private const string BaseMobSpeciesTest = "BaseMobSpeciesTest";

    [TestPrototypes]
    private const string Prototypes = $@"
- type: entity
  parent: BaseSpeciesMobOrganic
  id: {BaseMobSpeciesTest}
  name: {BaseMobSpeciesTest}
";

    private Dictionary<Enum, InterfaceData> GetInterfaces(UserInterfaceComponent comp) =>
        (Dictionary<Enum, InterfaceData>)
            typeof(UserInterfaceComponent).GetField("Interfaces", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(comp);

    [Test]
    public async Task AllSpeciesHaveBaseBUiTest()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Dirty = true,
            Connected = false
        });

        var server = pair.Server;
        var proto = server.ResolveDependency<IPrototypeManager>();
        var factoryComp = server.ResolveDependency<IComponentFactory>();

        await server.WaitAssertion(() =>
        {
            var bUiSys = server.System<SharedUserInterfaceSystem>();

            var baseEnt = proto.Index(BaseMobSpeciesTest);
            Assert.That(baseEnt, Is.Not.Null);
            Assert.That(baseEnt.TryGetComponent<UserInterfaceComponent>(out var bUiBase, factoryComp), Is.True);
            Assert.That(bUiBase, Is.Not.Null);
            var baseKeys = GetInterfaces(bUiBase).Keys.ToArray();

            Assert.Multiple(() =>
            {
                foreach (var species in proto.EnumeratePrototypes<SpeciesPrototype>())
                {
                    var ent = proto.Index(species.Prototype);
                    Assert.That(ent.TryGetComponent<UserInterfaceComponent>(out var bUi, factoryComp), Is.True);
                    Assert.That(bUi, Is.Not.Null);
                    var states = GetInterfaces(bUi);
                    foreach (var key in baseKeys)
                    {
                        Assert.That(states.ContainsKey(key), Is.True, $"Species {species.ID} is missing UserInterface for enum.{key.GetType().Name}.{key}");
                    }
                }
            });
        });
        await pair.CleanReturnAsync();
    }
}
