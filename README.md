# Impactful

Impactful adds restrained, short camera impulses to make key actions feel grounded without turning ordinary play into constant motion. It only shifts the world render; HUD, menus, cursor, and gameplay coordinates are untouched.

## Triggers

- ordinary rocks and large mineral clumps breaking under a pickaxe, with a slightly stronger shake for boulders, meteorites, and other large nodes;
- artifact spots dug up with a hoe, but not ordinary dirt;
- successful defensive-sword parries (club specials retain their vanilla shake);
- damage taken by the local player;
- nearby explosions, including explosions owned by another farmer; and
- wild trees felled by the local player, when the trunk lands.

Mining, combat, and falling trees from remote farmers do not shake your camera. In split screen, each local view has its own shake controller.

In single-player, successful melee hits pause the game for one update frame, or two frames for clubs, without adding camera shake. Lethal hits pause for three or four frames respectively, and successful parries pause for four. Hit stop is disabled in multiplayer and split-screen.

## Install

Requires Stardew Valley 1.6.15+ and SMAPI 4.4+. Extract the release ZIP into your SMAPI `Mods` folder. [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098) is optional; when installed, its settings are registered on game launch.

## Configuration

`config.json` contains only these fields and defaults:

```json
{
  "EnableScreenShake": true,
  "ShakeStrength": 100,
  "HitStop": true,
  "Mining": true,
  "ArtifactSpots": true,
  "Combat": true,
  "PlayerDamage": true,
  "Explosions": true,
  "Trees": true
}
```

`ShakeStrength` is a global intensity multiplier clamped to 0–200%. Base impulses are normalized to the camera viewport, so the same setting remains comparable across resolutions and zoom levels. Disabling shake or setting its strength to zero clears active impulses immediately. `HitStop` is independent of screen shake and has no effect outside single-player.

## Test command

Use `impactful_test [strength]` in the SMAPI console. With no argument it plays a small test impulse; a positive value up to 28 sets its relative intensity. Impulses are calibrated against a 1080-high camera viewport and scale with the current view. The command obeys the global enabled and intensity settings, but not category switches.

## Compatibility and scope

Impactful uses the same camera behavior as Stardew Valley's club special: it applies a one-time additive viewport kick immediately before the native camera update, then leaves the normal interpolation to attenuate, overshoot, and settle naturally. Menus and dialogue suppress queued impulses. Camera mods which replace Stardew Valley's viewport interpolation may change the settling behavior.

Hit stop temporarily uses Stardew Valley's native single-player pause path. It is deliberately disabled in multiplayer and split-screen to avoid pausing or desynchronizing other players.

Fruit trees, tilled dirt, rumble, particles, flashes, shaders, other gameplay changes, and a general-purpose effects framework are out of scope.

## Build locally

Install the pinned SDK, then supply the Steam game path to MSBuild:

```sh
mise install
dotnet restore Impactful.sln -p:GamePath="$HOME/Library/Application Support/Steam/steamapps/common/Stardew Valley/Contents/MacOS"
dotnet test Impactful.sln -c Release -p:GamePath="$HOME/Library/Application Support/Steam/steamapps/common/Stardew Valley/Contents/MacOS"
```

## Release

Set `<Version>` in `Impactful.csproj`, commit it, and push the change to `main`. The source manifest keeps a `%ProjectVersion%` token; CI substitutes the project version while packaging, creates `Impactful-<version>.zip`, then publishes a matching tag and GitHub release. Versions that already have a release are not republished.
