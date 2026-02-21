// <Trauma>
using Content.Shared.Humanoid.Prototypes;
using System.Linq;
// </Trauma>
using Content.Client.Lobby;
using Content.Server.Preferences.Managers;
using Content.Shared.Humanoid;
using Content.Shared.Preferences;
using Robust.Client.State;

namespace Content.IntegrationTests.Tests.Lobby;

[TestFixture]
[TestOf(typeof(ClientPreferencesManager))]
[TestOf(typeof(ServerPreferencesManager))]
public sealed class CharacterCreationTest
{
    [Test]
    public async Task CreateDeleteCreateTest()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { InLobby = true });
        var server = pair.Server;
        var client = pair.Client;
        var user = pair.Client.User!.Value;
        var clientPrefManager = client.Resolve<IClientPreferencesManager>();
        var serverPrefManager = server.Resolve<IServerPreferencesManager>();

        Assert.That(client.Resolve<IStateManager>().CurrentState, Is.TypeOf<LobbyState>());
        // <Trauma> - check every species instead of using random heisentest ones
        // not indented because to avoid farming conflicts
        // this test is gigantic shitcode btw fully copy pasted twice
        var proto = server.ProtoMan;
        foreach (var species in proto.EnumeratePrototypes<SpeciesPrototype>())
        {
        // </Trauma>

        await client.WaitPost(() => clientPrefManager.SelectCharacter(0));
        await pair.RunTicksSync(5);

        var clientCharacters = clientPrefManager.Preferences?.Characters;
        Assert.That(clientCharacters, Is.Not.Null);
        Assert.That(clientCharacters, Has.Count.EqualTo(1));

        HumanoidCharacterProfile profile = null;
        await client.WaitPost(() =>
        {
            profile = HumanoidCharacterProfile.RandomWithSpecies(species.ID); // Trauma - use species
            clientPrefManager.CreateCharacter(profile);
        });
        await pair.RunTicksSync(5);

        clientCharacters = clientPrefManager.Preferences?.Characters;
        Assert.That(clientCharacters, Is.Not.Null);
        Assert.That(clientCharacters, Has.Count.EqualTo(2));
        AssertEqual(clientCharacters[1], profile,
            species.ID); // Trauma

        await PoolManager.WaitUntil(server, () => serverPrefManager.GetPreferences(user).Characters.Count == 2, maxTicks: 60);

        var serverCharacters = serverPrefManager.GetPreferences(user).Characters;
        Assert.That(serverCharacters, Has.Count.EqualTo(2));
        AssertEqual(serverCharacters[1], profile,
            species.ID); // Trauma

        await client.WaitAssertion(() => clientPrefManager.DeleteCharacter(1));
        await pair.RunTicksSync(5);
        Assert.That(clientPrefManager.Preferences?.Characters.Count, Is.EqualTo(1));
        await PoolManager.WaitUntil(server, () => serverPrefManager.GetPreferences(user).Characters.Count == 1, maxTicks: 60);
        Assert.That(serverPrefManager.GetPreferences(user).Characters.Count, Is.EqualTo(1));

        await client.WaitIdleAsync();

        await client.WaitAssertion(() =>
        {
            profile = HumanoidCharacterProfile.RandomWithSpecies(species.ID); // Trauma - use species
            clientPrefManager.CreateCharacter(profile);
        });
        await pair.RunTicksSync(5);

        clientCharacters = clientPrefManager.Preferences?.Characters;
        Assert.That(clientCharacters, Is.Not.Null);
        Assert.That(clientCharacters, Has.Count.EqualTo(2));
        AssertEqual(clientCharacters[1], profile,
            species.ID); // Trauma

        await PoolManager.WaitUntil(server, () => serverPrefManager.GetPreferences(user).Characters.Count == 2, maxTicks: 60);
        serverCharacters = serverPrefManager.GetPreferences(user).Characters;
        Assert.That(serverCharacters, Has.Count.EqualTo(2));
        AssertEqual(serverCharacters[1], profile,
            species.ID); // Trauma
        // <Trauma>
        await client.WaitAssertion(() => clientPrefManager.DeleteCharacter(1));
        await pair.RunTicksSync(5);
        }
        // </Trauma>
        await pair.CleanReturnAsync();
    }

    private void AssertEqual(HumanoidCharacterProfile a, HumanoidCharacterProfile b,
        string species) // Trauma
    {
        if (a.MemberwiseEquals(b))
            return;

        Assert.Multiple(() =>
        {
            Assert.That(a.Name, Is.EqualTo(b.Name));
            Assert.That(a.Age, Is.EqualTo(b.Age));
            Assert.That(a.Sex, Is.EqualTo(b.Sex));
            Assert.That(a.Gender, Is.EqualTo(b.Gender));
            Assert.That(a.Species, Is.EqualTo(b.Species));
            Assert.That(a.PreferenceUnavailable, Is.EqualTo(b.PreferenceUnavailable));
            Assert.That(a.SpawnPriority, Is.EqualTo(b.SpawnPriority));
            Assert.That(a.FlavorText, Is.EqualTo(b.FlavorText));
            Assert.That(a.JobPriorities, Is.EquivalentTo(b.JobPriorities));
            Assert.That(a.AntagPreferences, Is.EquivalentTo(b.AntagPreferences));
            Assert.That(a.TraitPreferences, Is.EquivalentTo(b.TraitPreferences));
            Assert.That(a.Loadouts, Is.EquivalentTo(b.Loadouts));
            AssertEqual(a.Appearance, b.Appearance,
                species); // Trauma
            Assert.Fail($"Profile not equal for {species}"); // Trauma - pass species
        });
    }

    private void AssertEqual(HumanoidCharacterAppearance a, HumanoidCharacterAppearance b,
        string species) // Trauma
    {
        if (a.Equals(b))
            return;

        // <Trauma> - pass species to them all, made markings more specific
        Assert.That(a.EyeColor, Is.EqualTo(b.EyeColor),
            $"Eye color changed for {species}!");
        Assert.That(a.SkinColor, Is.EqualTo(b.SkinColor),
            $"Skin color changed for {species}!");
        Assert.That(a.Markings, Is.EquivalentTo(b.Markings),
            $"Markings changed for {species}!");
        Assert.Fail($"Appearance not equal for {species}");
        // </Trauma>
    }
}
