# Trauma Station contribution guidelines and standards

## Core guidelines

1. Do not rely on language models to do everything for you. You will be ridiculed for this.
2. If your code is shit, you will be forced to improve it. You will naturally improve as time goes on and you learn more.

## Making a PR

For non-trivial changes, include screenshots or videos showing it works.
Most PRs will be squash-merged so you don't need to rebase to clean your history up before merging.
When your PR has changes requested, go through this process:
1. If what to do is obvious, make the requested change and mark it as resolved when done.
2. If what to do is not obvious, comment on the request asking for clarification.
3. Once all requested changes are complete, request another review.

Remember to test your PR again after making changes to it! Not doing so is one of the most common sources of bugs.

## Programming

### C#

1. All new C# code must go in the `Content.Trauma.*` modules.
2. Try to only add events to `Content.Trauma.Common`, to allow for deocoupling for upstream's and your logic.
3. If you absolutely need to use an upstream `Content.Shared` type from `Content.*.Common`, you *may* move it to `Content.Common` without changing its namespace to keep code compatible.
4. If you are adding new methods, fields, etc. in upstream files make a partial class with the same filename but with `.Trauma.cs`. If it isn't partial already, make it partial with a comment.
5. Do not add new event handlers to upstream systems, make your own system in `Content.Trauma.*` instead.
6. Always use proxy methods when they are available, e.g. `TryComp` instead of `EntityManager.TryGetComponent`. This also means don't depend on `EntityManager` when you are in a `EntitySystem` or a BUI.

### Resources

All resources go in a `_Trauma` subdirectory inside the resource's folder, e.g. `Resources/Prototypes/_Trauma` for all YML prototypes.

For entity prototypes, keep everything consistent and sort the top level fields by `abstract > categories > parent > id > name > suffix > description > placement > categories`.

### Update logic

When querying for entities to update, the first component in an `EntityQueryEnumerator` should be the least common. The `ActiveXComponent` pattern is great for this, so you only ever query components that need to be updated.
This also means never ever do something like `EntityQueryEnumerator<TransformComponent, MyComponent>` as it will go through every entity in the game to check if it has your component!

Do not use frametime for game logic at all. Compare timespans with `IGameTiming.CurTime` instead, with `AutoPausedField` on component fields where necessary.

In hot code paths which are executed a lot, use EntityQuery and other micro-optimisations to reduce burden on the server and/or clients.

### Component networking

Most components that can be shared should be in shared. Also network them unless their lifetimes are extremely short or you have another good reason for it.

Your fields only need to be networked if either:
1. You change them in your code
2. You add the component with modified fields in e.g. a ComponentRegistry. These need to be networked or clients may only get the default values.

If you have many fields, use `fieldDeltas: true` and `DirtyField(ent, ent.Comp, nameof(MyComponent.MyField))` after changing `MyField`.
This minimizes bandwidth usage compared to sending the entire component state for a tiny change.

### Sounds

Sounds played globally (lobby music, antag briefings, etc.) should be stereo. This is usually how you get sounds from the internet anyway.

Sounds played positionally (most ingame objects do this) must be mono. Use ffmpeg or similar tools to convert it to mono if your sound is stereo.

### UI

If you need to add elements to an upstream UI, e.g. game bar buttons, try to inject it where possible to keep your code separate from upstream.
For example, you can add a `public static event Action<MyControl>? OnCreated;` then call `OnCreated?.Invoke(this)` at the end of `MyControl`'s constructor.
Then in a UI controller, system, etc. add a handler for `MyControl.OnCreated` and add your custom controls as children to it.
Doing this eliminates the need for upstream code to be dependent on your random systems, or those random systems to have any code in `Trauma.Common`.

### Prediction

All interactions must be predicted unless you have a very good reason not to do it.

All code should be in shared unless they have a hard dependency in server/client or are only used clientside, with no need to have the server control its existence.

## Commenting changes

Changes to upstream files must be commented properly.
For single line changes use `// Trauma - explanation` or in YML, `# Trauma - explanation`.
This should basically be a single-line diff explaining what you changed, e.g. `// Trauma - removed Access` would clearly mean the `[Access]` attribute on a class was removed.
If you are changing a value say what it used to be, and optionally why it was changed. e.g. `attackRate: 1 # Trauma - was 2, nerfed for being op`

For multi-line changes or replacements use the tag-like `// <Trauma>` `// </Trauma>` comment style.
When removing entire sections of code use `/* Trauma` ... `*/`, assuming there are no multiline comments inside of that code.

When adding things to a list where the order is not important, e.g. file imports, components in an entity prototype, always put them at the top to minimize the chances of conflicts.
Examples of this:
```cs
// <Trauma>
using Content.Shared.Examine;
using Robust.Shared.Prototypes;
// </Trauma>
using Content.Shared.Actions; // upstream's imports follow...
...
```

```yml
- type: entity
  parent: ...
  id: MobHuman
  name: Urist McHands
  components:
  # <Trauma>
  - type: Mutatable
    ...
  - type: Skinnable
    ...
  # </Trauma>
  - type: ... # upstream's components below
```

This causes less conflicts with upstream for 2 reasons:
1. Having all additions in 1 block means there is only 1 place it can conflict, as opposed to placing them randomly
2. When new additions are slapped onto existing prototypes etc, it's almost always added to the bottom or alphabetically sorted etc. It's extremely rare that someone would put it at the top to spite you.

## Changelogs

Firstly do not make changelogs for irrelevant things players won't notice.
This means code refactors, extremely niche stuff, etc.

Changelogs support writing to several different changelog files per pr.
Start with `:cl:`, optionally followed by a custom name for the changelog, on the first line.
Then each changelog line should be like this:
```
- add: Added something.
- remove: Removed something.
- fix: Fixed something.
- tweak: Changed something.
```

For mapping PRs, only sweeping changes to the map pool should be in the main changelog. Smaller things go in the `MAPS` changelog, for example:
```
:cl:
MAPS:
- tweak: Bagel: Fixed door access in engi.
```

Other changelogs you may use include `ADMIN` and `RULES`. once you set it with `NAME:`, any changes following it will use that file unless you switch it later.
