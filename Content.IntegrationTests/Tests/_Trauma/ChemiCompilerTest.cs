// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Guidebook;
using Content.Client.Guidebook.Richtext;
using Content.IntegrationTests.Fixtures;
using Content.Server.Chemistry.Components;
using Content.Server.Power.Components;
using Content.Shared.Guidebook;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.ContentPack;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.FixedPoint;
using Content.Shared.Speech;
using Content.Trauma.Client.ChemiCompiler.UI;
using Content.Trauma.Shared.ChemiCompiler;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using System.Collections.Generic;

namespace Content.IntegrationTests.Tests._Trauma;

/// <summary>
/// Tests the ChemiCompiler actually runs ChemFuck programs and does what they say.
/// </summary>
public sealed class ChemiCompilerTest : GameTest
{
    private static readonly EntProtoId Machine = "ChemiCompiler";
    private static readonly EntProtoId Beaker = "LargeBeaker";
    private static readonly EntProtoId Hotplate = "ChemistryHotplate";

    // same id as the machine, but a different kind of prototype
    private static readonly ProtoId<GuideEntryPrototype> GuideEntry = "ChemiCompiler";

    private static readonly ProtoId<ReagentPrototype> Sulfur = "Sulfur";
    private static readonly ProtoId<ReagentPrototype> Oxygen = "Oxygen";
    private static readonly ProtoId<ReagentPrototype> Hydrogen = "Hydrogen";
    private static readonly ProtoId<ReagentPrototype> SulfuricAcid = "SulfuricAcid";
    private static readonly ProtoId<ReagentPrototype> Water = "Water";

    /// <summary>
    /// Builds the run of + signs needed to get a memory cell to a value, since ChemFuck can't just say a number.
    /// </summary>
    private static string Count(int n)
        => new('+', n);

    /// <summary>
    /// Sets up a machine with beakers in every reservoir named, and the reagents they should start with.
    /// </summary>
    private async Task<(EntityUid Machine, Dictionary<int, EntityUid> Beakers)> Setup(
        Dictionary<int, Solution> contents)
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var slots = entMan.System<ItemSlotsSystem>();
        var solutions = entMan.System<SharedSolutionContainerSystem>();

        var map = await Pair.CreateTestMap();
        var uid = EntityUid.Invalid;
        var beakers = new Dictionary<int, EntityUid>();

        await server.WaitAssertion(() =>
        {
            uid = entMan.SpawnAtPosition(Machine, map.GridCoords);
            // these tests aren't about the power grid
            entMan.RemoveComponent<ApcPowerReceiverComponent>(uid);

            var comp = entMan.GetComponent<ChemiCompilerComponent>(uid);

            foreach (var (reservoir, fill) in contents)
            {
                var beaker = entMan.SpawnAtPosition(Beaker, map.GridCoords);
                Assert.That(slots.TryInsert(uid, comp.SlotId(reservoir), beaker, null),
                    $"Failed to put a beaker in reservoir {reservoir}");

                if (fill.Volume > FixedPoint2.Zero)
                {
                    Assert.That(solutions.TryGetFitsInDispenser(beaker, out var soln, out _));
                    solutions.AddSolution(soln.Value, fill);
                }

                beakers[reservoir] = beaker;
            }
        });

        return (uid, beakers);
    }

    /// <summary>
    /// Saves a program into slot 1 and starts it, without waiting for it to finish.
    /// </summary>
    private async Task Start(
        EntityUid uid,
        string program,
        int? maxInstructions = null,
        TimeSpan? maxRuntime = null)
    {
        var server = Pair.Server;
        var entMan = server.EntMan;

        await server.WaitAssertion(() =>
        {
            var comp = entMan.GetComponent<ChemiCompilerComponent>(uid);
            comp.Programs[0] = program;
            if (maxInstructions is { } max)
                comp.MaxInstructions = max;
            if (maxRuntime is { } runtime)
                comp.MaxRuntime = runtime;

            var jumps = ChemFuck.BuildJumpTable(program);
            Assert.That(jumps, Is.Not.Null, "Program has unbalanced brackets");

            // starting through the UI would need an actor with the window open, so start it directly
            var active = entMan.AddComponent<ActiveChemiCompilerComponent>(uid);
            active.Program = program;
            active.JumpTable = jumps!;
            // mirror what the run message does, or the machine starts with a deadline far in the past
            var now = server.ResolveDependency<IGameTiming>().CurTime;
            active.Started = now;
            active.NextStep = now;
        });
    }

    /// <summary>
    /// True if the machine is still working through a program.
    /// </summary>
    private bool IsRunning(EntityUid uid)
        => Pair.Server.EntMan.HasComponent<ActiveChemiCompilerComponent>(uid);

    /// <summary>
    /// Saves a program into slot 1, runs it, and waits for it to stop.
    /// </summary>
    private async Task Run(EntityUid uid, string program, float seconds = 5f, int? maxInstructions = null)
    {
        await Start(uid, program, maxInstructions);

        var server = Pair.Server;
        var entMan = server.EntMan;

        await Pair.RunSeconds(seconds);
        await Pair.RunTicksSync(1);

        await server.WaitAssertion(() =>
        {
            Assert.That(entMan.HasComponent<ActiveChemiCompilerComponent>(uid), Is.False,
                "Program was still running after it should have finished");
        });
    }

    private FixedPoint2 Quantity(EntityUid beaker, ProtoId<ReagentPrototype> reagent)
    {
        var entMan = Pair.Server.EntMan;
        var solutions = entMan.System<SharedSolutionContainerSystem>();
        return solutions.GetTotalPrototypeQuantity(beaker, reagent);
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

        await Pair.Server.WaitAssertion(() =>
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

        await Pair.Server.WaitAssertion(() =>
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

        // 10 sulfur, 20 oxygen and 10 hydrogen into r9, which is the 1:2:1 sulfuric acid recipe.
        // after the first move sx is in cell0 and ax is in cell2, so the rest just adjusts those two cells.
        var sulfur = $"+}}>{Count(9)})>{Count(10)}'@";
        var oxygen = $"<<+}}>>{Count(10)}'@"; // sx 1 -> 2, ax 10 -> 20
        var hydrogen = $"<<+}}>>{new string('-', 10)}'@"; // sx 2 -> 3, ax 20 -> 10

        await Run(uid, sulfur + oxygen + hydrogen, seconds: 15f);

        await Pair.Server.WaitAssertion(() =>
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

        // sx = 1, tx = 0, ax = 100, so 373K. 50u of water needs ~4000J, which at the hotplate's 160J/s is ~25s
        await Run(uid, $"+}}>)>{Count(100)}'$", seconds: 40f);

        await Pair.Server.WaitAssertion(() =>
        {
            var solutions = Pair.Server.EntMan.System<SharedSolutionContainerSystem>();
            Assert.That(solutions.TryGetFitsInDispenser(beakers[1], out _, out var solution));
            Assert.That(solution.Temperature, Is.EqualTo(373f).Within(0.5f),
                "Heat instruction did not reach the temperature the registers asked for");
        });
    }

    /// <summary>
    /// Heating has to cost the same energy a hotplate would, so automating chemistry doesn't also make it
    /// faster than doing it by hand.
    /// </summary>
    [Test]
    public async Task HeatingIsNoFasterThanAHotplate()
    {
        var (uid, beakers) = await Setup(new()
        {
            [1] = new Solution(Water, FixedPoint2.New(50)),
        });

        var server = Pair.Server;
        var entMan = server.EntMan;
        var solutions = entMan.System<SharedSolutionContainerSystem>();

        await server.WaitAssertion(() =>
        {
            // the machine must be rated to exactly what a hotplate does, not something quietly better
            var protoMan = server.ResolveDependency<IPrototypeManager>();
            var compFactory = server.ResolveDependency<IComponentFactory>();
            var hotplate = protoMan.Index(Hotplate);
            Assert.That(hotplate.TryComp<SolutionHeaterComponent>(out var heater, compFactory),
                "The hotplate prototype no longer has a SolutionHeater to compare against");

            var comp = entMan.GetComponent<ChemiCompilerComponent>(uid);
            Assert.That(comp.HeatPerSecond, Is.EqualTo(heater!.HeatPerSecond),
                "The ChemiCompiler heats at a different rate to a hotplate");
        });

        // start heating 50u of water to 373K. that's ~50 J/K * ~80K = ~4000J, so ~25s at 160 J/s
        await server.WaitAssertion(() =>
        {
            var comp = entMan.GetComponent<ChemiCompilerComponent>(uid);
            var program = $"+}}>)>{Count(100)}'$";
            comp.Programs[0] = program;

            var active = entMan.AddComponent<ActiveChemiCompilerComponent>(uid);
            active.Program = program;
            active.JumpTable = ChemFuck.BuildJumpTable(program)!;
        });

        // a third of the way in it must be under way but nowhere near done
        await Pair.RunSeconds(10f);
        await Pair.RunTicksSync(1);

        await server.WaitAssertion(() =>
        {
            Assert.That(solutions.TryGetFitsInDispenser(beakers[1], out _, out var solution));
            Assert.Multiple(() =>
            {
                Assert.That(solution!.Temperature, Is.GreaterThan(300f),
                    "Heating had not started ramping the temperature");
                Assert.That(solution.Temperature, Is.LessThan(360f),
                    "Heating got most of the way there far quicker than a hotplate would");
            });
            Assert.That(entMan.HasComponent<ActiveChemiCompilerComponent>(uid),
                "The program finished before the heating could have");
        });

        await Pair.RunSeconds(30f);
        await Pair.RunTicksSync(1);

        await server.WaitAssertion(() =>
        {
            Assert.That(solutions.TryGetFitsInDispenser(beakers[1], out _, out var solution));
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

        await Pair.Server.WaitAssertion(() =>
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

        // sx = 1, tx = 11, ax = 30. the dosage limit is 20, so that's two pills
        await Run(uid, $"+}}>{Count(11)})>{Count(30)}'@");

        await Pair.Server.WaitAssertion(() =>
        {
            var entMan = Pair.Server.EntMan;
            var pills = 0;
            var query = entMan.EntityQueryEnumerator<PillComponent>();
            while (query.MoveNext(out _, out _))
            {
                pills++;
            }

            Assert.Multiple(() =>
            {
                Assert.That(pills, Is.EqualTo(2), "Pill generator did not split 30u across two pills");
                Assert.That(Quantity(beakers[1], Water), Is.EqualTo(FixedPoint2.New(20)),
                    "Pill generator did not take 30u from the source reservoir");
            });
        });

        // sx = 1, tx = 12, ax = 10
        await Run(uid, $"+}}>{Count(12)})>{Count(10)}'@");

        await Pair.Server.WaitAssertion(() =>
        {
            Assert.That(Quantity(beakers[1], Water), Is.EqualTo(FixedPoint2.New(10)),
                "Vial generator did not take 10u from the source reservoir");
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

        await Pair.Server.WaitAssertion(() =>
        {
            var water = Quantity(beakers[9], Water);
            var sulfur = Quantity(beakers[9], Sulfur);

            Assert.Multiple(() =>
            {
                Assert.That(water + sulfur, Is.EqualTo(FixedPoint2.New(10)),
                    "Isolate did not move exactly 10u");
                // whichever reagent is listed first, only that one should have moved
                Assert.That(water == FixedPoint2.Zero || sulfur == FixedPoint2.Zero,
                    "Isolate moved more than one kind of reagent");
            });
        });
    }

    /// <summary>
    /// Instructions have to cost time. Without this the machine finishes any program in the tick you start
    /// it, which makes it strictly better than a chemist at everything.
    /// </summary>
    [Test]
    public async Task InstructionsTakeTime()
    {
        var (uid, _) = await Setup(new());

        // 100 increments at the fast tier is about two seconds of work
        await Start(uid, Count(100));

        await Pair.RunSeconds(1f);
        await Pair.RunTicksSync(1);

        await Pair.Server.WaitAssertion(() =>
        {
            Assert.That(IsRunning(uid), Is.True,
                "A hundred instructions finished in under a second, so they are effectively free");
        });

        await Pair.RunSeconds(4f);
        await Pair.RunTicksSync(1);

        await Pair.Server.WaitAssertion(() =>
        {
            Assert.That(IsRunning(uid), Is.False, "The program never finished");
        });
    }

    /// <summary>
    /// Touching a beaker has to cost much more than shuffling numbers around, since that is the part that
    /// decides how fast the machine can actually do chemistry.
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
        await Pair.RunSeconds(1.5f);
        await Pair.RunTicksSync(1);

        await Pair.Server.WaitAssertion(() =>
        {
            Assert.That(IsRunning(uid), Is.False,
                "Setting registers alone took over a second and a half, so the cheap tiers are not cheap");
        });

        // the exact same work plus three transfers, which should add about another second and a half
        await Start(uid, registersOnly + "@@@");
        await Pair.RunSeconds(1.5f);
        await Pair.RunTicksSync(1);

        await Pair.Server.WaitAssertion(() =>
        {
            Assert.That(IsRunning(uid), Is.True,
                "Three transfers finished as quickly as the registers alone, so they cost nothing extra");
        });

        await Pair.RunSeconds(5f);
        await Pair.RunTicksSync(1);

        await Pair.Server.WaitAssertion(() =>
        {
            Assert.That(IsRunning(uid), Is.False, "The transfers never finished");
        });
    }

    /// <summary>
    /// Doing nothing on purpose still has to cost the slow tier, which is the only reason the instruction
    /// exists. It does no work, so nothing but the delay distinguishes it from a no-op.
    /// </summary>
    [Test]
    public async Task NopCostsTheSlowTier()
    {
        var (uid, _) = await Setup(new());

        // twenty fast instructions, about 0.4s
        await Start(uid, Count(20));
        await Pair.RunSeconds(0.8f);
        await Pair.RunTicksSync(1);

        await Pair.Server.WaitAssertion(() =>
        {
            Assert.That(IsRunning(uid), Is.False,
                "Twenty fast instructions took longer than expected, so this test proves nothing");
        });

        // the same work plus one nop, which should push it past a second on its own
        await Start(uid, Count(20) + "*");
        await Pair.RunSeconds(0.8f);
        await Pair.RunTicksSync(1);

        await Pair.Server.WaitAssertion(() =>
        {
            Assert.That(IsRunning(uid), Is.True,
                "A nop cost no more than a fast instruction, so it is in the wrong speed tier");
        });

        await Pair.RunSeconds(2f);
        await Pair.RunTicksSync(1);

        await Pair.Server.WaitAssertion(() =>
        {
            Assert.That(IsRunning(uid), Is.False, "The nop never finished");
        });
    }

    /// <summary>
    /// The runtime cap, not the instruction count, is what stops a stuck program now that instructions are
    /// slow enough that the old limit would take hours to reach.
    /// </summary>
    [Test]
    public async Task RuntimeLimitHaltsStuckPrograms()
    {
        var (uid, _) = await Setup(new());

        await Start(uid, "+[]", maxRuntime: TimeSpan.FromSeconds(3));

        await Pair.RunSeconds(8f);
        await Pair.RunTicksSync(1);

        await Pair.Server.WaitAssertion(() =>
        {
            var entMan = Pair.Server.EntMan;
            Assert.That(IsRunning(uid), Is.False, "A stuck program outlived its runtime limit");

            // it should be nowhere near the instruction limit, proving time is what stopped it
            var comp = entMan.GetComponent<ChemiCompilerComponent>(uid);
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
    /// The interface has to actually open on the client, which is the only thing that checks the BUI prototype
    /// points at a real class and that the window can be built.
    /// </summary>
    [Test]
    public async Task InterfaceOpensOnClient()
    {
        var (uid, _) = await Setup(new()
        {
            [1] = new Solution(Water, FixedPoint2.New(10)),
        });

        var server = Pair.Server;
        var netEnt = default(NetEntity);

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;

            // put the player next to the machine so it's actually in view, otherwise the client never hears about it
            var coords = entMan.GetComponent<TransformComponent>(uid).Coordinates;
            var player = entMan.SpawnAtPosition("MobHuman", coords);
            server.PlayerMan.SetAttachedEntity(ServerSession!, player);

            netEnt = entMan.GetNetEntity(uid);
        });

        await Pair.RunTicksSync(15);

        await server.WaitAssertion(() =>
        {
            var ui = server.EntMan.System<SharedUserInterfaceSystem>();
            ui.OpenUi(uid, ChemiCompilerUiKey.Key, ServerSession!);
        });

        await Pair.RunTicksSync(15);

        await Client.WaitAssertion(() =>
        {
            var entMan = Client.EntMan;
            var clientUid = entMan.GetEntity(netEnt);

            Assert.That(entMan.TryGetComponent<UserInterfaceComponent>(clientUid, out var ui),
                "Machine has no user interface component on the client");
            Assert.That(ui!.ClientOpenInterfaces.TryGetValue(ChemiCompilerUiKey.Key, out var bui),
                "The ChemiCompiler interface did not open on the client");
            Assert.That(bui, Is.TypeOf<ChemiCompilerBUI>(),
                "The interface prototype did not resolve to the ChemiCompiler BUI");
        });
    }

    /// <summary>
    /// Putting a beaker in must not blank out the saved programs on the client. Inserting is predicted, so the
    /// client also handles the container event, and it has no idea what the programs are.
    /// </summary>
    [Test]
    public async Task InsertingABeakerKeepsSavedPrograms()
    {
        var (uid, _) = await Setup(new());

        var server = Pair.Server;
        var entMan = server.EntMan;
        var netEnt = default(NetEntity);

        await server.WaitAssertion(() =>
        {
            var coords = entMan.GetComponent<TransformComponent>(uid).Coordinates;
            var player = entMan.SpawnAtPosition("MobHuman", coords);
            server.PlayerMan.SetAttachedEntity(ServerSession!, player);
            netEnt = entMan.GetNetEntity(uid);
        });

        await Pair.RunTicksSync(15);

        await server.WaitAssertion(() =>
        {
            var comp = entMan.GetComponent<ChemiCompilerComponent>(uid);
            comp.Programs[0] = "+++";

            entMan.System<SharedUserInterfaceSystem>().OpenUi(uid, ChemiCompilerUiKey.Key, ServerSession!);
            entMan.System<ChemiCompilerSystem>().UpdateUi((uid, comp));
        });

        await Pair.RunTicksSync(15);

        AssertSlotFilled(netEnt, "before inserting a beaker");

        // now put a beaker in, which is what used to wipe the interface
        await server.WaitAssertion(() =>
        {
            var comp = entMan.GetComponent<ChemiCompilerComponent>(uid);
            var beaker = entMan.SpawnAtPosition(Beaker, entMan.GetComponent<TransformComponent>(uid).Coordinates);
            Assert.That(entMan.System<ItemSlotsSystem>().TryInsert(uid, comp.SlotId(1), beaker, null));
        });

        await Pair.RunTicksSync(15);

        AssertSlotFilled(netEnt, "after inserting a beaker");
    }

    private void AssertSlotFilled(NetEntity netEnt, string when)
    {
        var entMan = Client.EntMan;
        var ui = entMan.System<SharedUserInterfaceSystem>();
        var clientUid = entMan.GetEntity(netEnt);

        Assert.That(ui.TryGetUiState<ChemiCompilerState>(clientUid, ChemiCompilerUiKey.Key, out var state),
            $"The client had no interface state {when}");
        Assert.That(state!.Filled[0], Is.True,
            $"The client forgot that slot 1 holds a program {when}");
    }

    /// <summary>
    /// Clicking the machine with a beaker should fill the next free reservoir, not swap with whatever is
    /// in the first one. Item slots swap by default, which is wrong for a machine with ten of them.
    /// </summary>
    [Test]
    public async Task BeakersFillTheNextFreeReservoir()
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var slots = entMan.System<ItemSlotsSystem>();

        var map = await Pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var uid = entMan.SpawnAtPosition(Machine, map.GridCoords);
            var comp = entMan.GetComponent<ChemiCompilerComponent>(uid);

            // fill them one at a time the way the interact handler does, and check nothing gets displaced
            for (var i = 1; i <= ChemiCompilerComponent.Reservoirs; i++)
            {
                var beaker = entMan.SpawnAtPosition(Beaker, map.GridCoords);
                Assert.That(slots.TryGetSlot(uid, comp.SlotId(i), out var slot));
                Assert.That(slots.CanInsert(uid, beaker, null, slot!, slot!.Swap), Is.True,
                    $"Reservoir {i} would not accept a beaker while empty");

                slots.TryInsert(uid, comp.SlotId(i), beaker, null);

                // now that it's full it must refuse, so the interact handler moves on to the next reservoir
                Assert.That(slots.CanInsert(uid, entMan.SpawnAtPosition(Beaker, map.GridCoords), null, slot, slot.Swap),
                    Is.False,
                    $"Reservoir {i} still accepted a beaker while full, so clicking would swap instead of filling the next one");
            }
        });
    }

    /// <summary>
    /// Runs a program, waits, and reports the line the machine has built up but not said yet.
    /// The program must still be running at that point, so end it with nops.
    /// </summary>
    private async Task<string> PendingOutput(EntityUid uid, string program, float seconds)
    {
        await Start(uid, program);
        await Pair.RunSeconds(seconds);
        await Pair.RunTicksSync(1);

        var buffer = string.Empty;
        await Pair.Server.WaitAssertion(() =>
        {
            Assert.That(IsRunning(uid), Is.True,
                "The program finished before its buffer could be looked at, so this proves nothing");
            buffer = Pair.Server.EntMan.GetComponent<ActiveChemiCompilerComponent>(uid).Output;
        });

        // let it run itself out so the next program can start
        await Pair.RunSeconds(8f);
        await Pair.RunTicksSync(1);
        return buffer;
    }

    /// <summary>
    /// The . instruction builds up a line, and a newline is what sends it. Anything still pending gets said
    /// when the program halts.
    /// </summary>
    [Test]
    public async Task OutputBuffersUntilNewline()
    {
        var (uid, _) = await Setup(new());

        await Pair.Server.WaitAssertion(() =>
        {
            // without this the machine has no voice and the bubble can never appear
            Assert.That(Pair.Server.EntMan.HasComponent<SpeechComponent>(uid), Is.True,
                "The machine can't speak, so . has nowhere to put its output");
        });

        // 'A' is 65, then bump to 'B'. the nops keep the program alive while the buffer is inspected.
        const string write = "." + "+" + ".";
        var nops = new string('*', 4);

        var pending = await PendingOutput(uid, Count(65) + write + nops, seconds: 3f);
        Assert.That(pending, Is.EqualTo("AB"), "Characters written with . did not build up into a line");

        // same again, but a newline (10) between the writing and the nops should have sent the line
        var flushed = await PendingOutput(uid, Count(65) + write + ">" + Count(10) + "." + nops, seconds: 3f);
        Assert.That(flushed, Is.Empty, "A newline did not send the line and clear the buffer");
    }

    /// <summary>
    /// Every sound has to point at a real file and be loud enough to actually hear.
    /// Volume here is gain = 10^(dB/10), not the usual 10^(dB/20), so the numbers drop away much faster than
    /// they look like they should — the machine has already been silently inaudible once because of it.
    /// </summary>
    [Test]
    public async Task SoundsAreAudibleAndExist()
    {
        var (uid, _) = await Setup(new());

        var server = Pair.Server;
        var resMan = server.ResolveDependency<IResourceManager>();

        await server.WaitAssertion(() =>
        {
            var comp = server.EntMan.GetComponent<ChemiCompilerComponent>(uid);

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
                    Assert.That(resMan.ContentFileExists(path), Is.True,
                        $"The {name} sound points at {path}, which does not exist");

                    var gain = SharedAudioSystem.VolumeToGain(sound.Params.Volume);
                    Assert.That(sound.Params.Volume, Is.GreaterThan(-10f),
                        $"The {name} sound is {sound.Params.Volume}dB, which is {gain:P1} gain and effectively silent");
                }
            });
        });
    }

    /// <summary>
    /// The guidebook entry has to parse. The upstream test that checks every guide entry is disabled,
    /// and this document is full of square brackets that the markup parser would otherwise choke on.
    /// </summary>
    [Test]
    public async Task GuidebookEntryParses()
    {
        var client = Client;
        await client.WaitIdleAsync();

        var protoMan = client.ResolveDependency<IPrototypeManager>();
        var resMan = client.ResolveDependency<IResourceManager>();
        var parser = client.ResolveDependency<DocumentParsingManager>();

        await client.WaitAssertion(() =>
        {
            var proto = protoMan.Index(GuideEntry);
            using var reader = resMan.ContentFileReadText(proto.Text);
            var text = reader.ReadToEnd();

            Assert.Multiple(() =>
            {
                Assert.That(parser.TryAddMarkup(new Document(), text),
                    "The ChemiCompiler guidebook entry could not be parsed");

                // guidebook documents look like XML but aren't, so entities render as literal "&gt;" to the player.
                // angle brackets have to be written as \> and \<, which is easy to forget in a document full of them.
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
        await Pair.Server.WaitAssertion(() =>
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
