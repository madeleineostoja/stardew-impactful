# Impactful

Impactful adds restrained, short camera impulses to make key actions feel grounded without turning ordinary play into constant motion. It only shifts the world render; HUD, menus, cursor, and gameplay coordinates are untouched.

## Triggers

- boulders, meteorites, and other large mineral clumps breaking under a pickaxe (ordinary rocks and ore nodes do not shake);
- successful defensive-sword parries (club specials retain their vanilla shake);
- damage taken by the local player;
- nearby explosions, including explosions owned by another farmer; and
- wild trees felled by the local player, when the trunk lands.

Mining, combat, and falling trees from remote farmers do not shake your camera. In split screen, each local view has its own shake controller.

## Install

Requires Stardew Valley 1.6.15+ and SMAPI 4.4+. Extract the release ZIP into your SMAPI `Mods` folder. [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098) is optional; when installed, its settings are registered on game launch.

In multiplayer, explosion feedback requires Impactful on the host and each client that wants it. Other locally detected effects work without the host installing the mod. The host relays explosions even if its own shake is disabled.

## Configuration

`config.json` contains only these fields and defaults:

```json
{
  "EnableScreenShake": true,
  "ShakeStrength": 100,
  "Mining": true,
  "Combat": true,
  "PlayerDamage": true,
  "Explosions": true,
  "Trees": true
}
```

`ShakeStrength` is clamped to 0–200%. Disabling shake or setting its strength to zero clears active impulses immediately.

## Test commands

Use `impactful_status` to report runtime settings and registered gameplay hooks. This distinguishes disabled effects or missing patches from an imperceptible impulse. Tree fall and landing decisions are also recorded in the SMAPI trace log.

Use `impactful_test [strength]` in the SMAPI console while a save is loaded and menus are closed. With no argument it plays a small test impulse; a positive value up to 28 requests that many viewport pixels of world-render offset, before zoom. The command obeys the global enabled and strength settings, but not category switches, and logs whether the impulse was queued or testing was blocked.

## Compatibility and scope

Impactful shifts the viewport only between SMAPI's world-render events and restores it before the HUD is drawn. Each impulse starts at full strength, eases down with a small rebound, and ends after 180 milliseconds of visible animation. Impulses wait while a native screen flash obscures most of the world, so explosion feedback is not lost behind the flash. Native camera interpolation cannot attenuate these render offsets. Menus, dialogue, and minigames suppress and clear impulses. Camera mods which change the viewport during world rendering may interfere.

Bombs use their original blast radius for strength and distance falloff. When explosion feedback is enabled, bomb damage does not add a separate player-damage shake; if explosions are disabled, player-damage feedback still applies. Other damage sources keep their normal feedback.

Fruit trees, tilled dirt, rumble, particles, flashes, shaders, other gameplay changes, and a general-purpose effects framework are out of scope.

## Build locally

Install the pinned SDK, then supply the Steam game path to MSBuild:

```sh
mise install
dotnet restore Impactful.sln -p:GamePath="$HOME/Library/Application Support/Steam/steamapps/common/Stardew Valley/Contents/MacOS"
dotnet test Impactful.sln -c Release -p:GamePath="$HOME/Library/Application Support/Steam/steamapps/common/Stardew Valley/Contents/MacOS"
```

### In-game verification

The automated tests cover shake aggregation, decay, clearing, direction fallback, and explosion falloff. Game-facing patches should also be checked in SMAPI:

- A defensive-sword parry should still work with player-damage shake disabled. Ordinary melee hits do not pause the game or add camera shake.
- Detonate each bomb just outside its blast radius, then take bomb damage. Each blast should produce only one explosion impulse. Disable explosions and repeat: bomb damage should still produce player-damage feedback.
- Fell a tree normally, then test leaving the location mid-fall and removing a falling tree through another mod. No delayed shake should occur after returning.
- Detonate a bomb near a farmhand with Impactful installed on both peers, including with host shake disabled. Other players' mining and tree falls should remain local to their own views.

For performance comparisons, use the same dense-combat location, heavily wooded farm, and bomb chain before and after changes; compare frame time and allocations rather than average FPS alone.

## Release

Set `<Version>` in `Impactful.csproj`, commit it, and push the change to `main`. The source manifest keeps a `%ProjectVersion%` token; CI substitutes the project version while packaging, creates `Impactful-<version>.zip`, then publishes a matching tag and GitHub release. Versions that already have a release are not republished.
