// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Guidebook;
using Content.Client.Guidebook.Richtext;
using Content.Goobstation.Server.MedicalPatch;
using Content.Server.Chemistry.Components;
using Content.Server.Power.Components;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.FixedPoint;
using Content.Shared.Guidebook;
using Content.Shared.Materials;
using Content.Shared.Speech;
using Content.Trauma.Client.ChemiCompiler.UI;
using Content.Trauma.Shared.ChemiCompiler;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.ContentPack;
using System.Linq;

namespace Content.IntegrationTests.Tests._Trauma;

/// <summary>
/// Tests the ChemiCompiler actually runs ChemFuck programs and does what they say.
/// </summary>
public sealed class ChemiCompilerTest : GameTest
{
    [SidedDependency(Side.Server)] private readonly IResourceManager _sResources = default!;
    [SidedDependency(Side.Server)] private readonly ItemSlotsSystem _slots = default!;
    [SidedDependency(Side.Server)] private readonly SharedMaterialStorageSystem _materials = default!;
    [SidedDependency(Side.Server)] private readonly SharedSolutionContainerSystem _solutions = default!;
    [SidedDependency(Side.Server)] private readonly SharedUserInterfaceSystem _sUi = default!;

    [SidedDependency(Side.Client)] private readonly DocumentParsingManager _parser = default!;
    [SidedDependency(Side.Client)] private readonly IResourceManager _cResources = default!;
    [SidedDependency(Side.Client)] private readonly SharedUserInterfaceSystem _cUi = default!;

    private static readonly EntProtoId Machine = "ChemiCompiler";
    private static readonly EntProtoId Beaker = "LargeBeaker";
    private static readonly EntProtoId Hotplate = "ChemistryHotplate";
    private static readonly EntProtoId Player = "MobHuman";
    private static readonly EntProtoId Glass = "SheetGlass";
    private static readonly EntProtoId Cloth = "MaterialCloth";
    private static readonly EntProtoId Plastic = "SheetPlastic";

    // same id as the machine, but a different kind of prototype
    private static readonly ProtoId<GuideEntryPrototype> GuideEntry = "ChemiCompiler";

    private static readonly ProtoId<ReagentPrototype> Sulfur = "Sulfur";
    private static readonly ProtoId<ReagentPrototype> Oxygen = "Oxygen";
    private static readonly ProtoId<ReagentPrototype> Hydrogen = "Hydrogen";
    private static readonly ProtoId<ReagentPrototype> SulfuricAcid = "SulfuricAcid";
    private static readonly ProtoId<ReagentPrototype> Water = "Water";

    /// <summary>
    /// The machine <see cref="Setup"/> made, for talking to its interface on the client.
    /// </summary>
    private NetEntity _machine;

    /// <summary>
    /// The player <see cref="Setup"/> attached, for anything that needs someone to be doing it.
    /// </summary>
    private EntityUid _player;

    /// <summary>
    /// A run of + signs, the only way to write a number in ChemFuck.
    /// </summary>
    private static string Count(int n)
        => new('+', n);

    /// <summary>
    /// Spawns a machine with beakers in every reservoir named, attaches a player and opens the interface.
    /// </summary>
    private async Task<(EntityUid Machine, Dictionary<int, EntityUid> Beakers)> Setup(
        Dictionary<int, Solution> contents)
    {
        var map = await Pair.CreateTestMap();
        var uid = EntityUid.Invalid;
        var beakers = new Dictionary<int, EntityUid>();

        await Server.WaitAssertion(() =>
        {
            uid = SEntMan.SpawnAtPosition(Machine, map.GridCoords);
            // these tests aren't about the power grid
            SRemComp<ApcPowerReceiverComponent>(uid);
            _machine = SEntMan.GetNetEntity(uid);

            var comp = SEntMan.GetComponent<ChemiCompilerComponent>(uid);

            foreach (var (reservoir, fill) in contents)
            {
                var beaker = SEntMan.SpawnAtPosition(Beaker, map.GridCoords);
                Assert.That(_slots.TryInsert(uid, comp.SlotId(reservoir), beaker, null),
                    $"Failed to put a beaker in reservoir {reservoir}");

                if (fill.Volume > FixedPoint2.Zero)
                {
                    Assert.That(_solutions.TryGetFitsInDispenser(beaker, out var soln, out _));
                    _solutions.AddSolution(soln.Value, fill);
                }

                beakers[reservoir] = beaker;
            }

            // the ui only opens for an attached player, and the machine has to be in its view for the
            // client to ever hear about it
            _player = SEntMan.SpawnAtPosition(Player, map.GridCoords);
            Server.PlayerMan.SetAttachedEntity(ServerSession!, _player);
        });

        await RunTicksSync(15);

        await Server.WaitPost(() => _sUi.OpenUi(uid, ChemiCompilerUiKey.Key, ServerSession!));

        await RunTicksSync(15);

        return (uid, beakers);
    }

    /// <summary>
    /// Sends a message from the client's interface, the way pressing a button in it would.
    /// </summary>
    private async Task SendBui(BoundUserInterfaceMessage msg)
    {
        await Client.WaitAssertion(() =>
        {
            var clientUid = CEntMan.GetEntity(_machine);
            Assert.That(CEntMan.TryGetComponent<UserInterfaceComponent>(clientUid, out var ui),
                "Machine has no user interface component on the client");
            Assert.That(ui!.ClientOpenInterfaces.TryGetValue(ChemiCompilerUiKey.Key, out var bui),
                "The ChemiCompiler interface is not open on the client");

            bui!.SendMessage(msg);
        });
    }

    /// <summary>
    /// Saves a program into slot 1 through the interface and runs it, without waiting for it to finish.
    /// </summary>
    private async Task Start(
        EntityUid uid,
        string program,
        int? maxInstructions = null,
        TimeSpan? maxRuntime = null)
    {
        Assert.That(ChemFuck.BuildJumpTable(program), Is.Not.Null, "Program has unbalanced brackets");

        await SendBui(new ChemiCompilerSaveMessage(0, program));
        await RunTicksSync(5);

        await Server.WaitAssertion(() =>
        {
            var comp = SEntMan.GetComponent<ChemiCompilerComponent>(uid);
            Assert.That(comp.Programs[0], Is.EqualTo(program), "The save message never reached the machine");

            // neither limit has a button in the interface
            if (maxInstructions is { } max)
                comp.MaxInstructions = max;
            if (maxRuntime is { } runtime)
                comp.MaxRuntime = runtime;
        });

        await SendBui(new ChemiCompilerRunMessage(0));

        // tick one at a time, since everything after this is timed from the moment the program started
        var running = false;
        for (var i = 0; i < 20 && !running; i++)
        {
            await RunTicksSync(1);
            await Server.WaitPost(() => running = SHasComp<ActiveChemiCompilerComponent>(uid));
        }

        Assert.That(running, "The run message never started the program");
    }

    /// <summary>
    /// True if the machine is still working through a program.
    /// </summary>
    private bool IsRunning(EntityUid uid)
        => SHasComp<ActiveChemiCompilerComponent>(uid);

    /// <summary>
    /// Saves a program into slot 1, runs it, and waits for it to stop.
    /// </summary>
    private async Task Run(EntityUid uid, string program, float seconds = 5f, int? maxInstructions = null)
    {
        await Start(uid, program, maxInstructions);

        await RunSeconds(seconds);
        await RunTicksSync(1);

        await Server.WaitAssertion(() =>
        {
            Assert.That(IsRunning(uid), Is.False,
                "Program was still running after it should have finished");
        });
    }

    private FixedPoint2 Quantity(EntityUid beaker, ProtoId<ReagentPrototype> reagent)
        => _solutions.GetTotalPrototypeQuantity(beaker, reagent);

    /// <summary>
    /// Puts one item's worth of materials into the machine.
    /// </summary>
    private void Load(EntityUid uid, Dictionary<ProtoId<MaterialPrototype>, int> cost)
    {
        Assert.That(_materials.TryChangeMaterialAmount(uid, cost),
            $"Failed to load the machine with {string.Join(", ", cost.Keys)}");
    }

    /// <summary>
    /// How much of everything named in a cost the machine is holding.
    /// </summary>
    private int Stored(EntityUid uid, Dictionary<ProtoId<MaterialPrototype>, int> cost)
        => cost.Keys.Sum(material => _materials.GetMaterialAmount(uid, material));

    /// <summary>
    /// Every patch in the world right now.
    /// </summary>
    private List<EntityUid> Patches()
    {
        var found = new List<EntityUid>();
        var query = SEntMan.EntityQueryEnumerator<MedicalPatchComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            found.Add(uid);
        }

        return found;
    }

    /// <summary>
    /// The registers plus @ should move exactly the amount asked for, from the reservoir asked for.
    /// </summary>
    [Test]
    public async Task TransferMovesReagents()
    {
        var (uid, beakers) = await Setup(new()
        {
            [1] = new Solution(Water, FixedPoint2.New(50)),
            [9] = new Solution(),
        });

        // cell0 = 1 -> sx, cell1 = 9 -> tx, cell2 = 13 -> ax, then move
        await Run(uid, $"+}}>{Count(9)})>{Count(13)}'@");

        await Server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(Quantity(beakers[9], Water), Is.EqualTo(FixedPoint2.New(13)),
                    "Target reservoir did not receive 13u");
                Assert.That(Quantity(beakers[1], Water), Is.EqualTo(FixedPoint2.New(37)),
                    "Source reservoir did not lose 13u");
            });
        });
    }

    /// <summary>
    /// Loops should run their body the right number of times, and nested brackets should pair up correctly.
    /// </summary>
    [Test]
    public async Task LoopsRepeat()
    {
        var (uid, beakers) = await Setup(new()
        {
            [1] = new Solution(Water, FixedPoint2.New(50)),
            [9] = new Solution(),
        });

        // cell2 counts down from 5 adding 2 to cell3 each time, leaving 10 in cell3 for the amount register
        await Run(uid, $"+}}>{Count(9)})>{Count(5)}[->++<]>'@");

        await Server.WaitAssertion(() =>
        {
            Assert.That(Quantity(beakers[9], Water), Is.EqualTo(FixedPoint2.New(10)),
                "Loop did not build an amount of 10");
        });
    }

    /// <summary>
    /// Reagents put in a reservoir together should actually react, so real recipes can be automated.
    /// </summary>
    [Test]
    public async Task ReagentsReactInReservoirs()
    {
        var (uid, beakers) = await Setup(new()
        {
            [1] = new Solution(Sulfur, FixedPoint2.New(50)),
            [2] = new Solution(Oxygen, FixedPoint2.New(50)),
            [3] = new Solution(Hydrogen, FixedPoint2.New(50)),
            [9] = new Solution(),
        });

        // 10 sulfur, 20 oxygen and 10 hydrogen into r9, the 1:2:1 sulfuric acid recipe.
        // after the first move sx is in cell0 and ax is in cell2, so the rest just adjusts those two cells.
        var sulfur = $"+}}>{Count(9)})>{Count(10)}'@";
        var oxygen = $"<<+}}>>{Count(10)}'@"; // sx 1 -> 2, ax 10 -> 20
        var hydrogen = $"<<+}}>>{new string('-', 10)}'@"; // sx 2 -> 3, ax 20 -> 10

        await Run(uid, $"{sulfur}{oxygen}{hydrogen}", seconds: 15f);

        await Server.WaitAssertion(() =>
        {
            Assert.That(Quantity(beakers[9], SulfuricAcid), Is.GreaterThan(FixedPoint2.Zero),
                "No sulfuric acid was produced in the mixing reservoir");
        });
    }

    /// <summary>
    /// The heat instruction should bring a reservoir to (273 - tx) + ax Kelvin.
    /// </summary>
    [Test]
    public async Task HeatSetsTemperature()
    {
        var (uid, beakers) = await Setup(new()
        {
            [1] = new Solution(Water, FixedPoint2.New(50)),
        });

        // sx = 1, tx = 0, ax = 100, so 373K. 50u of water needs ~4000J, which at 160J/s is ~25s
        await Run(uid, $"+}}>)>{Count(100)}'$", seconds: 40f);

        await Server.WaitAssertion(() =>
        {
            Assert.That(_solutions.TryGetFitsInDispenser(beakers[1], out _, out var solution));
            Assert.That(solution.Temperature, Is.EqualTo(373f).Within(0.5f),
                "Heat instruction did not reach the temperature the registers asked for");
        });
    }

    /// <summary>
    /// Heating has to cost the same energy a hotplate would, so automating chemistry isn't also faster.
    /// </summary>
    [Test]
    public async Task HeatingIsNoFasterThanAHotplate()
    {
        var (uid, beakers) = await Setup(new()
        {
            [1] = new Solution(Water, FixedPoint2.New(50)),
        });

        await Server.WaitAssertion(() =>
        {
            var hotplate = SProtoMan.Index(Hotplate);
            Assert.That(hotplate.TryComp<SolutionHeaterComponent>(out var heater, SEntMan.ComponentFactory),
                "The hotplate prototype no longer has a SolutionHeater to compare against");

            var comp = SEntMan.GetComponent<ChemiCompilerComponent>(uid);
            Assert.That(comp.HeatPerSecond, Is.EqualTo(heater!.HeatPerSecond),
                "The ChemiCompiler heats at a different rate to a hotplate");
        });

        // heat 50u of water to 373K: ~50 J/K * ~80K = ~4000J, so ~25s at 160 J/s
        await Start(uid, $"+}}>)>{Count(100)}'$");

        // a third of the way in it must be under way but nowhere near done
        await RunSeconds(10f);
        await RunTicksSync(1);

        await Server.WaitAssertion(() =>
        {
            Assert.That(_solutions.TryGetFitsInDispenser(beakers[1], out _, out var solution));
            Assert.Multiple(() =>
            {
                Assert.That(solution!.Temperature, Is.GreaterThan(300f),
                    "Heating had not started ramping the temperature");
                Assert.That(solution.Temperature, Is.LessThan(360f),
                    "Heating got most of the way there far quicker than a hotplate would");
            });
            Assert.That(IsRunning(uid), "The program finished before the heating could have");
        });

        await RunSeconds(30f);
        await RunTicksSync(1);

        await Server.WaitAssertion(() =>
        {
            Assert.That(_solutions.TryGetFitsInDispenser(beakers[1], out _, out var solution));
            Assert.That(solution!.Temperature, Is.EqualTo(373f).Within(0.5f),
                "Heating never reached the target temperature");
        });
    }

    /// <summary>
    /// Target 13 is the ejection port, which should destroy what it's given rather than moving it anywhere.
    /// </summary>
    [Test]
    public async Task EjectionPortDiscardsReagents()
    {
        var (uid, beakers) = await Setup(new()
        {
            [1] = new Solution(Water, FixedPoint2.New(50)),
        });

        // sx = 1, tx = 13, ax = 30
        await Run(uid, $"+}}>{Count(13)})>{Count(30)}'@");

        await Server.WaitAssertion(() =>
        {
            Assert.That(Quantity(beakers[1], Water), Is.EqualTo(FixedPoint2.New(20)),
                "Ejection port did not discard exactly what it was given");
        });
    }

    /// <summary>
    /// Targets 11 and 12 should package reagents up into pills and vials instead of moving them to a beaker.
    /// </summary>
    [Test]
    public async Task PillAndVialGeneratorsWork()
    {
        var (uid, beakers) = await Setup(new()
        {
            [1] = new Solution(Water, FixedPoint2.New(50)),
        });

        var pills = 0;
        await Server.WaitAssertion(() =>
        {
            // other tests in the run may have left pills lying around, so only the difference matters
            pills = SEntMan.Count<PillComponent>();

            var comp = SEntMan.GetComponent<ChemiCompilerComponent>(uid);
            Load(uid, comp.VialCost);
        });

        // sx = 1, tx = 11, ax = 30. the dosage limit is 20, so that's two pills
        await Run(uid, $"+}}>{Count(11)})>{Count(30)}'@");

        await Server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(SEntMan.Count<PillComponent>() - pills, Is.EqualTo(2),
                    "Pill generator did not split 30u across two pills");
                Assert.That(Quantity(beakers[1], Water), Is.EqualTo(FixedPoint2.New(20)),
                    "Pill generator did not take 30u from the source reservoir");
            });
        });

        // sx = 1, tx = 12, ax = 10
        await Run(uid, $"+}}>{Count(12)})>{Count(10)}'@");

        await Server.WaitAssertion(() =>
        {
            var comp = SEntMan.GetComponent<ChemiCompilerComponent>(uid);
            Assert.Multiple(() =>
            {
                Assert.That(Quantity(beakers[1], Water), Is.EqualTo(FixedPoint2.New(10)),
                    "Vial generator did not take 10u from the source reservoir");
                Assert.That(Stored(uid, comp.VialCost), Is.Zero,
                    "Vial generator did not spend the glass it was loaded with");
            });
        });
    }

    /// <summary>
    /// The vial generator has to fail without glass, and give the reagents back when it does.
    /// </summary>
    [Test]
    public async Task VialGeneratorNeedsGlass()
    {
        var (uid, beakers) = await Setup(new()
        {
            [1] = new Solution(Water, FixedPoint2.New(50)),
        });

        await Server.WaitAssertion(() =>
        {
            Assert.That(SHasComp<MaterialStorageComponent>(uid),
                "The machine has nowhere to keep glass, so this test proves nothing");

            var comp = SComp<ChemiCompilerComponent>(uid);
            Assert.That(Stored(uid, comp.VialCost), Is.Zero,
                "The machine started with glass in it");
        });

        // sx = 1, tx = 12, ax = 10, with nothing to make the vial out of
        await Run(uid, $"+}}>{Count(12)})>{Count(10)}'@");

        await Server.WaitAssertion(() =>
        {
            Assert.That(Quantity(beakers[1], Water), Is.EqualTo(FixedPoint2.New(50)),
                "A vial was made with no glass loaded, or the reagents were not given back");
        });
    }

    /// <summary>
    /// Every material the generators need has to be insertable by hand, not just settable in code.
    /// Cloth is tagged RawMaterial rather than Sheet, so a Sheet-only whitelist silently refuses it.
    /// </summary>
    [Test]
    public async Task MaterialsCanBeInsertedByHand()
    {
        var (uid, _) = await Setup(new());

        await Server.WaitAssertion(() =>
        {
            var coords = SEntMan.GetComponent<TransformComponent>(uid).Coordinates;

            foreach (var stack in new EntProtoId[] { Glass, Cloth, Plastic })
            {
                var sheets = SEntMan.SpawnAtPosition(stack, coords);
                Assert.That(_materials.TryInsertMaterialEntity(_player, sheets, uid),
                    $"{stack.Id} could not be put into the machine");
            }
        });
    }

    /// <summary>
    /// Targets 14 up should each print their own kind of patch, filled with what was sent to them.
    /// </summary>
    [Test]
    public async Task PatchGeneratorsWork()
    {
        var (uid, _) = await Setup(new()
        {
            [1] = new Solution(Water, FixedPoint2.New(100)),
        });

        var expected = new List<EntProtoId>();
        await Server.WaitAssertion(() =>
        {
            var comp = SEntMan.GetComponent<ChemiCompilerComponent>(uid);
            expected = new List<EntProtoId>(comp.PatchPrototypes);

            // one patch's worth of materials per target this test is about to use
            for (var i = 0; i < expected.Count; i++)
            {
                Load(uid, comp.PatchCost);
            }
        });

        var volumes = new Dictionary<string, FixedPoint2>();

        for (var i = 0; i < expected.Count; i++)
        {
            var before = new List<EntityUid>();
            await Server.WaitPost(() => before = Patches());

            // sx = 1, tx = the patch target, ax = 10
            var target = ChemiCompilerComponent.TargetPatchFirst + i;
            await Run(uid, $"+}}>{Count(target)})>{Count(10)}'@");

            var index = i;
            await Server.WaitAssertion(() =>
            {
                var made = Patches().Except(before).ToList();
                Assert.That(made, Has.Count.EqualTo(1),
                    $"Target {target} did not print exactly one patch");

                var patch = made[0];
                var proto = SEntMan.GetComponent<MetaDataComponent>(patch).EntityPrototype;
                Assert.Multiple(() =>
                {
                    Assert.That(proto?.ID, Is.EqualTo(expected[index].Id),
                        $"Target {target} printed the wrong kind of patch");
                    Assert.That(Quantity(patch, Water), Is.EqualTo(FixedPoint2.New(10)),
                        $"Target {target} printed a patch with nothing in it");
                });

                Assert.That(_solutions.TryGetSolution(patch, SharedChemMaster.BottleSolutionName,
                    out _, out var solution));
                volumes[expected[index].Id] = solution!.MaxVolume;
            });
        }

        await Server.WaitAssertion(() =>
        {
            var comp = SEntMan.GetComponent<ChemiCompilerComponent>(uid);
            Assert.That(Stored(uid, comp.PatchCost), Is.Zero,
                "The patches did not use up the materials they were paid for with");

            // the large patch is only worth having because it holds more, so filling must not flatten it
            Assert.That(volumes["MedicalPatchLarge"], Is.GreaterThan(volumes["MedicalPatchBasic"]),
                "The large patch lost its extra capacity, so the fill overrode the prototype's volume");
        });
    }

    /// <summary>
    /// The patch generators have to fail with no cloth or plastic, and give the reagents back when they do.
    /// </summary>
    [Test]
    public async Task PatchGeneratorNeedsMaterials()
    {
        var (uid, beakers) = await Setup(new()
        {
            [1] = new Solution(Water, FixedPoint2.New(50)),
        });

        var patches = 0;
        await Server.WaitAssertion(() =>
        {
            patches = SEntMan.Count<MedicalPatchComponent>();

            var comp = SEntMan.GetComponent<ChemiCompilerComponent>(uid);
            Assert.That(Stored(uid, comp.PatchCost), Is.Zero,
                "The machine started with patch materials in it");
        });

        // sx = 1, tx = 14, ax = 10, with nothing to make the patch out of
        await Run(uid, $"+}}>{Count(ChemiCompilerComponent.TargetPatchFirst)})>{Count(10)}'@");

        await Server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(SEntMan.Count<MedicalPatchComponent>() - patches, Is.Zero,
                    "A patch was printed with no materials loaded");
                Assert.That(Quantity(beakers[1], Water), Is.EqualTo(FixedPoint2.New(50)),
                    "The reagents were not given back when the patch failed");
            });
        });
    }

    /// <summary>
    /// Isolating should pull out only the reagent the data pointer names, leaving the rest behind.
    /// </summary>
    [Test]
    public async Task IsolateExtractsOneReagent()
    {
        var mixed = new Solution(Water, FixedPoint2.New(30));
        mixed.AddReagent(Sulfur, FixedPoint2.New(30));

        var (uid, beakers) = await Setup(new()
        {
            [1] = mixed,
            [9] = new Solution(),
        });

        // sx = 1, tx = 9, ax = 10, then walk back to a cell holding 1 so # takes the first reagent
        await Run(uid, $"+}}>{Count(9)})>{Count(10)}'<<#");

        await Server.WaitAssertion(() =>
        {
            var water = Quantity(beakers[9], Water);
            var sulfur = Quantity(beakers[9], Sulfur);

            Assert.Multiple(() =>
            {
                Assert.That(water + sulfur, Is.EqualTo(FixedPoint2.New(10)),
                    "Isolate did not move exactly 10u");
                Assert.That(water == FixedPoint2.Zero || sulfur == FixedPoint2.Zero,
                    "Isolate moved more than one kind of reagent");
            });
        });
    }

    /// <summary>
    /// Instructions have to cost time, or the machine finishes any program in the tick you start it.
    /// </summary>
    [Test]
    public async Task InstructionsTakeTime()
    {
        var (uid, _) = await Setup(new());

        // 100 increments at the fast tier is about two seconds of work
        await Start(uid, Count(100));

        await RunSeconds(1f);
        await RunTicksSync(1);

        await Server.WaitAssertion(() =>
        {
            Assert.That(IsRunning(uid), Is.True,
                "A hundred instructions finished in under a second, so they are effectively free");
        });

        await RunSeconds(4f);
        await RunTicksSync(1);

        await Server.WaitAssertion(() =>
        {
            Assert.That(IsRunning(uid), Is.False, "The program never finished");
        });
    }

    /// <summary>
    /// Touching a beaker has to cost much more than shuffling numbers around.
    /// </summary>
    [Test]
    public async Task PhysicalOperationsCostMore()
    {
        var (uid, _) = await Setup(new()
        {
            [1] = new Solution(Water, FixedPoint2.New(50)),
            [9] = new Solution(),
        });

        // sets sx, tx and ax but never moves anything: 22 fast and 3 normal instructions, about 0.75s
        const string registersOnly = "+}>+++++++++)>++++++++++'";

        await Start(uid, registersOnly);
        await RunSeconds(1.5f);
        await RunTicksSync(1);

        await Server.WaitAssertion(() =>
        {
            Assert.That(IsRunning(uid), Is.False,
                "Setting registers alone took over a second and a half, so the cheap tiers are not cheap");
        });

        // the exact same work plus three transfers, which should add about another second and a half
        await Start(uid, $"{registersOnly}@@@");
        await RunSeconds(1.5f);
        await RunTicksSync(1);

        await Server.WaitAssertion(() =>
        {
            Assert.That(IsRunning(uid), Is.True,
                "Three transfers finished as quickly as the registers alone, so they cost nothing extra");
        });

        await RunSeconds(5f);
        await RunTicksSync(1);

        await Server.WaitAssertion(() =>
        {
            Assert.That(IsRunning(uid), Is.False, "The transfers never finished");
        });
    }

    /// <summary>
    /// Doing nothing on purpose still has to cost the slow tier, which is the only reason the instruction exists.
    /// </summary>
    [Test]
    public async Task NopCostsTheSlowTier()
    {
        var (uid, _) = await Setup(new());

        // twenty fast instructions, about 0.4s
        await Start(uid, Count(20));
        await RunSeconds(0.8f);
        await RunTicksSync(1);

        await Server.WaitAssertion(() =>
        {
            Assert.That(IsRunning(uid), Is.False,
                "Twenty fast instructions took longer than expected, so this test proves nothing");
        });

        // the same work plus one nop, which should push it past a second on its own
        await Start(uid, $"{Count(20)}*");
        await RunSeconds(0.8f);
        await RunTicksSync(1);

        await Server.WaitAssertion(() =>
        {
            Assert.That(IsRunning(uid), Is.True,
                "A nop cost no more than a fast instruction, so it is in the wrong speed tier");
        });

        await RunSeconds(2f);
        await RunTicksSync(1);

        await Server.WaitAssertion(() =>
        {
            Assert.That(IsRunning(uid), Is.False, "The nop never finished");
        });
    }

    /// <summary>
    /// The runtime cap, not the instruction count, is what stops a stuck program.
    /// </summary>
    [Test]
    public async Task RuntimeLimitHaltsStuckPrograms()
    {
        var (uid, _) = await Setup(new());

        await Start(uid, "+[]", maxRuntime: TimeSpan.FromSeconds(3));

        await RunSeconds(8f);
        await RunTicksSync(1);

        await Server.WaitAssertion(() =>
        {
            Assert.That(IsRunning(uid), Is.False, "A stuck program outlived its runtime limit");

            // it should be nowhere near the instruction limit, proving time is what stopped it
            var comp = SEntMan.GetComponent<ChemiCompilerComponent>(uid);
            Assert.That(comp.MaxInstructions, Is.GreaterThan(1000),
                "This test only means something while the instruction limit is the looser of the two");
        });
    }

    /// <summary>
    /// A program that never ends has to give up on its own rather than running forever.
    /// </summary>
    [Test]
    public async Task InfiniteLoopHalts()
    {
        var (uid, _) = await Setup(new()
        {
            [1] = new Solution(),
        });

        // cell0 is 1 and nothing ever changes it, so this loops until the instruction limit stops it
        await Run(uid, "+[]", seconds: 15f, maxInstructions: 200);
    }

    /// <summary>
    /// Checks the BUI prototype points at a real class and that the window can be built.
    /// </summary>
    [Test]
    public async Task InterfaceOpensOnClient()
    {
        await Setup(new()
        {
            [1] = new Solution(Water, FixedPoint2.New(10)),
        });

        await Client.WaitAssertion(() =>
        {
            var clientUid = CEntMan.GetEntity(_machine);

            Assert.That(CEntMan.TryGetComponent<UserInterfaceComponent>(clientUid, out var ui),
                "Machine has no user interface component on the client");
            Assert.That(ui!.ClientOpenInterfaces.TryGetValue(ChemiCompilerUiKey.Key, out var bui),
                "The ChemiCompiler interface did not open on the client");
            Assert.That(bui, Is.TypeOf<ChemiCompilerBUI>(),
                "The interface prototype did not resolve to the ChemiCompiler BUI");
        });
    }

    /// <summary>
    /// Inserting is predicted, so the client also handles the container event and must not blank the programs
    /// it has no idea about.
    /// </summary>
    [Test]
    public async Task InsertingABeakerKeepsSavedPrograms()
    {
        var (uid, _) = await Setup(new());

        await SendBui(new ChemiCompilerSaveMessage(0, "+++"));
        await RunTicksSync(15);

        await AssertSlotFilled("before inserting a beaker");

        await Server.WaitAssertion(() =>
        {
            var comp = SEntMan.GetComponent<ChemiCompilerComponent>(uid);
            var beaker = SEntMan.SpawnAtPosition(Beaker, SEntMan.GetComponent<TransformComponent>(uid).Coordinates);
            Assert.That(_slots.TryInsert(uid, comp.SlotId(1), beaker, null));
        });

        await RunTicksSync(15);

        await AssertSlotFilled("after inserting a beaker");
    }

    private async Task AssertSlotFilled(string when)
    {
        await Client.WaitAssertion(() =>
        {
            var clientUid = CEntMan.GetEntity(_machine);

            Assert.That(_cUi.TryGetUiState<ChemiCompilerState>(clientUid, ChemiCompilerUiKey.Key, out var state),
                $"The client had no interface state {when}");
            Assert.That(state!.Filled[0], Is.True,
                $"The client forgot that slot 1 holds a program {when}");
        });
    }

    /// <summary>
    /// Item slots swap by default, which on a machine with ten of them means clicking never fills the second.
    /// </summary>
    [Test]
    public async Task BeakersFillTheNextFreeReservoir()
    {
        var map = await Pair.CreateTestMap();

        await Server.WaitAssertion(() =>
        {
            var uid = SEntMan.SpawnAtPosition(Machine, map.GridCoords);
            var comp = SEntMan.GetComponent<ChemiCompilerComponent>(uid);

            // fill them one at a time the way the interact handler does, and check nothing gets displaced
            for (var i = 1; i <= ChemiCompilerComponent.Reservoirs; i++)
            {
                var beaker = SEntMan.SpawnAtPosition(Beaker, map.GridCoords);
                Assert.That(_slots.TryGetSlot(uid, comp.SlotId(i), out var slot));
                Assert.That(_slots.CanInsert(uid, slot!, beaker, null, slot!.Swap), Is.True,
                    $"Reservoir {i} would not accept a beaker while empty");

                _slots.TryInsert(uid, comp.SlotId(i), beaker, null);

                Assert.That(_slots.CanInsert(uid, slot, SEntMan.SpawnAtPosition(Beaker, map.GridCoords), null, slot.Swap),
                    Is.False,
                    $"Reservoir {i} still accepted a beaker while full, so clicking would swap instead of filling the next one");
            }
        });
    }

    /// <summary>
    /// Runs a program and reports the line the machine has built up but not said yet.
    /// The program must still be running at that point, so end it with nops.
    /// </summary>
    private async Task<string> PendingOutput(EntityUid uid, string program, float seconds)
    {
        await Start(uid, program);
        await RunSeconds(seconds);
        await RunTicksSync(1);

        var buffer = string.Empty;
        await Server.WaitAssertion(() =>
        {
            Assert.That(IsRunning(uid), Is.True,
                "The program finished before its buffer could be looked at, so this proves nothing");
            buffer = SEntMan.GetComponent<ActiveChemiCompilerComponent>(uid).Output.ToString();
        });

        // let it run itself out so the next program can start
        await RunSeconds(8f);
        await RunTicksSync(1);
        return buffer;
    }

    /// <summary>
    /// The . instruction builds up a line and a newline sends it. Anything pending is said when the program halts.
    /// </summary>
    [Test]
    public async Task OutputBuffersUntilNewline()
    {
        var (uid, _) = await Setup(new());

        await Server.WaitAssertion(() =>
        {
            Assert.That(SHasComp<SpeechComponent>(uid), Is.True,
                "The machine can't speak, so it has nowhere to put its output");
        });

        // 'A' is 65, then bump to 'B'. the nops keep the program alive while the buffer is inspected.
        const string write = ".+.";
        var nops = new string('*', 4);

        var pending = await PendingOutput(uid, $"{Count(65)}{write}{nops}", seconds: 3f);
        Assert.That(pending, Is.EqualTo("AB"), "Characters written with . did not build up into a line");

        // same again, but a newline (10) between the writing and the nops should have sent the line
        var flushed = await PendingOutput(uid, $"{Count(65)}{write}>{Count(10)}.{nops}", seconds: 3f);
        Assert.That(flushed, Is.Empty, "A newline did not send the line and clear the buffer");
    }

    /// <summary>
    /// A program can write any byte, including the two that chat parses as markup. Saying those must not
    /// crash the server.
    /// </summary>
    [Test]
    public async Task MarkupCharactersDoNotCrashTheServer()
    {
        var (uid, _) = await Setup(new());

        await Run(uid, $"{Count(91)}.+.", seconds: 5f);
    }

    /// <summary>
    /// Sounds must exist and be audible. Gain is 10^(dB/10), so -16dB is near silent.
    /// </summary>
    [Test]
    public async Task SoundsAreAudibleAndExist()
    {
        var (uid, _) = await Setup(new());

        await Server.WaitAssertion(() =>
        {
            var comp = SEntMan.GetComponent<ChemiCompilerComponent>(uid);

            var sounds = new (string Name, SoundSpecifier Sound)[]
            {
                ("start", comp.StartSound),
                ("fail", comp.FailSound),
                ("transfer", comp.TransferSound),
                ("heat", comp.HeatSound),
                ("idle", comp.IdleSound),
            };

            Assert.Multiple(() =>
            {
                foreach (var (name, sound) in sounds)
                {
                    Assert.That(sound, Is.TypeOf<SoundPathSpecifier>(), $"The {name} sound is not a file path");

                    var path = ((SoundPathSpecifier) sound).Path;
                    Assert.That(_sResources.ContentFileExists(path), Is.True,
                        $"The {name} sound points at {path}, which does not exist");

                    var gain = SharedAudioSystem.VolumeToGain(sound.Params.Volume);
                    Assert.That(sound.Params.Volume, Is.GreaterThan(-10f),
                        $"The {name} sound is {sound.Params.Volume}dB, which is {gain:P1} gain and effectively silent");
                }
            });
        });
    }

    /// <summary>
    /// The upstream test that checks every guide entry is disabled, and this document is full of brackets.
    /// </summary>
    [Test]
    public async Task GuidebookEntryParses()
    {
        await Client.WaitAssertion(() =>
        {
            var proto = CProtoMan.Index(GuideEntry);
            using var reader = _cResources.ContentFileReadText(proto.Text);
            var text = reader.ReadToEnd();

            Assert.Multiple(() =>
            {
                Assert.That(_parser.TryAddMarkup(new Document(), text),
                    "The ChemiCompiler guidebook entry could not be parsed");

                // guidebook documents look like XML but aren't, so entities render verbatim.
                // angle brackets have to be written as \> and \<.
                foreach (var entity in new[] { "&gt;", "&lt;", "&amp;" })
                {
                    Assert.That(text, Does.Not.Contain(entity),
                        $"The guidebook entry contains {entity}, which the player will see verbatim");
                }
            });
        });
    }

    /// <summary>
    /// Brackets that don't pair up must be rejected rather than left to hang the machine.
    /// </summary>
    [Test]
    public async Task UnbalancedBracketsAreRejected()
    {
        await Server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(ChemFuck.BuildJumpTable("+]"), Is.Null, "A ] with no [ was accepted");
                Assert.That(ChemFuck.BuildJumpTable("+["), Is.Null, "A [ that is never closed was accepted");
                Assert.That(ChemFuck.BuildJumpTable("[[]]"), Is.Not.Null, "Nested brackets were rejected");
                Assert.That(ChemFuck.BuildJumpTable("[][]"), Is.Not.Null, "Sequential loops were rejected");
            });
        });
    }
}
