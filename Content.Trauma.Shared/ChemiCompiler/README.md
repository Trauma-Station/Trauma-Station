# ChemiCompiler

A chemistry machine you program instead of click. It holds ten beakers and runs short programs written in
**ChemFuck**, a Brainfuck derivative whose extra instructions move reagents between reservoirs, heat them,
isolate single reagents, and package results into pills and vials.

## Provenance and licensing

This is a clean-room reimplementation of the ChemiCompiler from **Goonstation**. The only source consulted
was the behavioural description on their wiki: <https://wiki.ss13.co/ChemiCompiler> — the opcode table,
register semantics, and audio cues. **No Goonstation code was read or ported.**

That distinction matters: Goonstation's code is CC BY-NC-SA 3.0, whose non-commercial clause is incompatible
with this repository's AGPL-3.0-or-later. Game mechanics aren't copyrightable, so an independent
implementation of a documented behaviour is clean. Every file here carries the repo's standard AGPL header,
and no wiki prose was copied — the guidebook entry is original text.

---

## File map

| Path | What it does |
| --- | --- |
| `Content.Trauma.Shared/ChemiCompiler/ChemFuck.cs` | The instruction set, program validation, bracket jump table. Pure static, no entity access. |
| `Content.Trauma.Shared/ChemiCompiler/ChemiCompilerComponent.cs` | Machine data: saved programs, limits, sounds. Also holds the UI key, state, and messages. |
| `Content.Trauma.Shared/ChemiCompiler/ActiveChemiCompilerComponent.cs` | Execution state. Present **only** while a program runs. |
| `Content.Trauma.Shared/ChemiCompiler/ChemiCompilerSystem.cs` | Interface handling: save, run, halt, beaker in/out, pushing UI state. |
| `Content.Trauma.Server/ChemiCompiler/ChemiCompilerRunnerSystem.cs` | The interpreter. Runs instructions, moves reagents, heats, spawns pills. |
| `Content.Trauma.Client/ChemiCompiler/UI/` | `ChemiCompilerBUI.cs` plus the window XAML. |
| `Resources/Prototypes/_Trauma/Entities/Structures/Machines/chemicompiler.yml` | The machine entity and its ten beaker slots. |
| `Resources/ServerInfo/_Trauma/Guidebook/ChemiCompiler.xml` | In-game guidebook entry. |
| `Content.IntegrationTests/Tests/_Trauma/ChemiCompilerTest.cs` | Test suite. |

### Why the server/shared split

The interpreter lives on the server, mirroring how the existing `Circuits` module splits
`CircuitEditorSystem` (shared) from `CircuitSystem` (server). Programs, the 1024-byte memory, and beaker
contents are far too big to network, and pill spawning has to be server-authoritative.

`ChemiCompilerComponent.Programs` is a `serverOnly` DataField. **A client that touches it sees an empty
array.** This caused a real bug: inserting a beaker is predicted, so the client also ran the
container-changed handler, called `UpdateUi`, and blanked out the save-slot buttons. `UpdateUi` now returns
early on the client. If you add anything that pushes UI state, keep it server-side.

Save, run, and halt are sent with `SendMessage` (unpredicted) for the same reason. Only the beaker
insert/eject uses `SendPredictedMessage`, since item slots are predicted.

---

## Execution model

`ChemiCompilerRunnerSystem.Update` enumerates `<ActiveChemiCompilerComponent, ChemiCompilerComponent>` —
active component first, so idle machines are never walked. Idle machines have no active component at all.

**One instruction runs per `NextStep`.** Every instruction sets its own delay, so a running machine works
through a program at a visible pace instead of finishing it in the tick you press the button. All timing
compares against `IGameTiming.CurTime`; **frametime is never used for game logic**, per `CONTRIBUTING.md`.
Time-sensitive fields are `[AutoPausedField]`.

Every instruction returns one of three outcomes:

- **Next** — carry on, after this instruction's tier delay.
- **Failed** — beep and carry on anyway (see *Failures* below).
- **Wait** — the instruction set a longer `NextStep` itself, so don't overwrite it. Used by `$` and `*`.

Characters that aren't instructions are skipped for free, so comments and whitespace cost nothing.

### Speed tiers

Costs come from `ChemFuck.SpeedOf`, resolved to a delay by `ChemiCompilerComponent.DelayFor`.

| Tier | Default | Instructions | Why |
| --- | --- | --- | --- |
| `Fast` | 0.02s | `>` `<` `+` `-` `[` `]` | `+` is the *only* way to write a number in this language. Charging full price would mean setting a register to 100 costs more than the chemistry it's for. |
| `Normal` | 0.1s | `}` `{` `)` `(` `'` `^` `.` `~` | Register shuffling and bookkeeping. |
| `Physical` | 1s | `@` `#` `,` | Reaching into a beaker. This is the tier that actually gates a program's output. |

`$` and `*` never consult the table — they return `Wait` and set their own delays (the hotplate rate and
`NopDelay` respectively).

`NextStep` is advanced by `+= delay` from the deadline just met, not set to `CurTime + delay`. A tick is
~0.033s, so assigning from `CurTime` would round every sub-tick delay up to a whole tick and quietly make
`FastDelay` 0.033s instead of 0.02s. Accumulating means a tick can owe more than one instruction;
`MaxInstructionsPerTick` (64) bounds how many it will pay back at once so a server hitch catches up steadily
rather than dumping half a program.

For scale: the wiki's fluorosulfuric program is ~280 instructions, ~200 of them `+`. It takes roughly
40 seconds end to end, most of that the heating step.

A program halts when it runs off the end, exceeds `MaxRuntime` (10 min) or `MaxInstructions` (100,000),
loses power, or the player presses Halt. `MaxRuntime` is the limit that normally bites; the instruction
count is a backstop. On halt the active component is removed, the beaker slots unlock, and the final
register values are pushed to the interface.

While running, all ten reservoir slots are locked so a program can't have the rug pulled out from under it.

## Memory and registers

1024 single-byte cells (wrapping at 0 and 255) and a data pointer that wraps at both ends of memory.
There is no way to enter a number directly — you write that many `+` signs. That's the language, not a gap.

| Register | Meaning |
| --- | --- |
| `sx` | Source. Which reservoir (1–10) an instruction reads from. |
| `tx` | Target. Where output goes — see the port table below. |
| `ax` | Amount. Units to move, and the heating figure. |

Registers are only shown in the interface **when a program halts**, matching the original. That makes them
a debugger of last resort: run it, see where it stopped, work backwards.

## Instruction set

Any character that isn't an instruction is ignored, so programs can carry whitespace and notes.

| Op | Effect |
| --- | --- |
| `>` `<` | Move the data pointer right / left. |
| `+` `-` | Add / subtract one from the current cell. |
| `[` `]` | While loop. `[` skips past its `]` when the cell is zero; `]` jumps back when it isn't. |
| `}` `{` | Write / read `sx` from the current cell. |
| `)` `(` | Write / read `tx`. |
| `'` `^` | Write / read `ax`. |
| `,` | Measure how full reservoir `sx` is into `ax`. |
| `$` | Bring reservoir `sx` to `(273 - tx) + ax` Kelvin. |
| `@` | Move `ax` units from `sx` to wherever `tx` points. |
| `#` | Move `ax` units of **one** reagent from `sx` to `tx`. Which reagent is the current cell's value, a 1-based index into the beaker's contents. |
| `.` | Append the current cell to the output text as a character. |
| `~` | Lock this program so Load can't read it back. It can still be overwritten. |
| `*` | Do nothing for a moment, for reactions that need time. |

Loops nest. Brackets are matched once when the program starts, into a jump table, so `]` doesn't rescan the
program every iteration — that matters when a loop runs thousands of times.

### Target ports

| `tx` | Destination |
| --- | --- |
| 1–10 | That reservoir. |
| 11 | Pill generator. Splits across pills of at most `PillDosage` (20u) each. |
| 12 | Vial generator. Spawns a vial and fills it. |
| 13 | Ejection port. Destroys what it's given. |

Anything that won't fit in the target goes back to the source beaker. Reagents are never silently lost —
except at the ejection port, which is the point of the ejection port.

## Heating

`$` uses **the hotplate's energy model** so automating chemistry doesn't also make it faster than doing it
by hand:

```
energy  = solution heat capacity × |target − current temperature|
duration = energy ÷ HeatPerSecond
```

`HeatPerSecond` defaults to **160 J/s**, the same figure as `SolutionHeaterComponent` on the hotplate
prototype. Heating 100u of a default-specific-heat reagent from room temperature to 373 K takes about
50 seconds — the same as leaving that beaker on a hotplate.

The temperature is walked toward the target across the wait rather than snapped at the end, so reactions
with a temperature requirement fire on the way up, again matching a hotplate. Cooling uses the same rate in
reverse. `MinHeatDelay` (1 s) stops fractional adjustments from being free.

## Failures

If an instruction can't be carried out — usually `sx` or `tx` pointing at an empty reservoir — the machine
plays the fail sound and **continues to the next instruction**. It does not stop.

This is deliberate and follows the wiki, which describes "a string of loud beeps" from a single run. A
program that buzzes repeatedly is doing nothing at all, very confidently.

## Sounds

All five cues are `DataField`s with explicit negative volumes, because a program can fire hundreds of them.
The transfer hum and the fail beep are each rate-limited to one per `SoundCooldown` (1 s), tracked
separately so a chatty program can't drown out its own errors.

| Cue | Sound | Volume |
| --- | --- | --- |
| Program start | `twobeep.ogg` | −10 dB |
| Instruction failed | `buzz-two.ogg` | −8 dB (loudest on purpose) |
| Reagents moved | `beep.ogg` | −16 dB |
| Heating start/end | `button.ogg` | −12 dB |
| Program finished | `chime.ogg` | −10 dB |

## Interface

Reproduces the original's layout: a code editor on a dark panel; **Save** and **Load** buttons that arm the
six numbered slot buttons; ten reservoir buttons in two rows of five; and the register readout.

The slot buttons do whatever Save/Load say they do:

- Neither pressed — clicking a slot **runs** it.
- Save pressed — clicking a slot stores the editor's contents there.
- Load pressed — clicking a slot pulls its code into the editor.

Slots holding a program glow green; reservoirs holding a beaker glow blue. Load is handled entirely on the
client from the last received state, so it costs no round trip. Locked programs send `null` instead of their
code, so the button still lights up but the text can't be recovered.

Clicking the machine itself with a beaker fills the **next free** reservoir. This needs `swap: false` on the
slots — item slots swap by default, which meant every click traded with reservoir 1.

## Getting one

Print a **ChemiCompiler CCS1000 machine board** at a medical lathe and build it like any machine. No
research gate. The board is in the `TraumaMedicalBoardsStatic` lathe pack, which feeds upstream's
`MedicalBoardsStatic`.

The machine currently reuses `Structures/Machines/mixer.rsi` with a blue tint. **Dedicated sprites are the
main outstanding piece of polish.**

---

## Deviations from Goonstation

Three, all deliberate:

1. **Unbalanced brackets are refused up front.** The wiki says a stray `]` hangs their machine indefinitely.
   Refusing to start beats a permanently bricked machine, so `ChemFuck.BuildJumpTable` returns null and the
   run is rejected with a popup.
2. **The ejection port deletes reagents** rather than spilling them. There's no shared spill API, and a loop
   dumping fluorosulfuric acid across the chem lab floor is a griefing amplifier.
3. **Registers are 8/16-bit in name only.** Memory cells are bytes; the registers hold whatever a cell can.

### Goon programs are not portable

The *machine* is faithful. The *chemistry* is not — SS14's reaction database isn't Goonstation's. Sulfuric
acid here is 1 hydrogen : 1 sulfur : 2 oxygen yielding 3, and fluorosulfuric is 1:1:1:1 yielding 4. The
worked example in the wiki article will run, but it won't produce what the article says it does. Programs
have to be written against SS14 recipes.

## Tuning knobs

All `DataField`s on `ChemiCompilerComponent`, changeable in YAML without touching C#:

`MaxProgramLength` (8192) · `MaxRuntime` (10 min) · `MaxInstructions` (100000) ·
`FastDelay` (0.02 s) · `InstructionDelay` (0.1 s) · `PhysicalDelay` (1 s) · `MaxInstructionsPerTick` (64) ·
`MaxOutputLength` (256) · `PillDosage` (20u) · `PillPrototype` · `VialPrototype` ·
`HeatPerSecond` (160) · `MinHeatDelay` (1 s) · `NopDelay` (1 s) · `SoundCooldown` (1 s) · all five sounds.

`PhysicalDelay` is the main balance lever — it decides how fast the machine can do chemistry. `FastDelay`
mostly decides how tedious large constants feel.

Constants that aren't tunable, because the UI and memory model depend on them: `CodeSlots` (6),
`Reservoirs` (10), `MemorySize` (1024).

## Tests

`Content.IntegrationTests/Tests/_Trauma/ChemiCompilerTest.cs` covers transfers, loops, real reactions firing
in reservoirs, heating to an exact temperature, the pill/vial/ejection ports, isolation, the instruction
limit cutting off infinite loops, bracket rejection, the interface opening on a real client, saved programs
surviving a beaker insert, and beakers filling the next free reservoir.

Two of those are regression tests for bugs found in play, and both were confirmed to fail with their fix
reverted. The guidebook test also asserts the document contains no HTML entities — guidebook files look like
XML but aren't parsed as XML, so `&gt;` reaches the player verbatim.

```bash
dotnet test Content.IntegrationTests --filter "FullyQualifiedName~ChemiCompilerTest"
```

## Known gaps

- No dedicated sprite.
- No portable variant (the wiki mentions a 6-reservoir version).
- `.` writes to an output buffer shown in the interface; there's no paper printout.
- The interface has not been driven by hand in a running client — the tests confirm it opens and receives
  correct state, but layout and feel are unverified.
