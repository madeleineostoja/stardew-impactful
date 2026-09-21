# Impactful

Impactful adds restrained, short camera impulses to make key actions feel grounded without turning ordinary play into constant motion. It only shifts the world render; HUD, menus, cursor, and gameplay coordinates are untouched.

## Triggers

- pickaxe impacts on breakable stones and stone breaks;
- successful local-player melee hits;
- damage taken by the local player;
- nearby explosions, including explosions owned by another farmer; and
- wild trees felled by the local player, when the trunk lands.

Mining, combat, and falling trees from remote farmers do not shake your camera. In split screen, each local view has its own shake controller.

In single-player, successful melee hits also pause the game for one update frame, or two frames for clubs. Hit stop is disabled in multiplayer and split-screen.

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
  "Combat": true,
  "PlayerDamage": true,
  "Explosions": true,
  "Trees": true
}
```

`ShakeStrength` is clamped to 0–200%. Disabling shake or setting its strength to zero clears active impulses immediately. `HitStop` is independent of screen shake and has no effect outside single-player.

## Test command

Use `impact_test [strength]` in the SMAPI console. With no argument it plays a medium impulse; a positive value up to 6 requests that many base screen pixels. It obeys the global enabled and strength settings, but not category switches.

## Compatibility and scope

Impactful applies and removes additive viewport offsets during world rendering, which is intended to cooperate with camera mods. It accounts for the current viewport and output dimensions so zoom and Cinderbox render scaling remain comparable. Menus and dialogue suppress application while impulses continue to decay.

Hit stop temporarily uses Stardew Valley's native single-player pause path. It is deliberately disabled in multiplayer and split-screen to avoid pausing or desynchronizing other players.

Fruit trees, resource clumps, rumble, particles, flashes, shaders, other gameplay changes, and a general-purpose effects framework are out of scope.

## Build locally

Install the pinned SDK, then supply the Steam game path to MSBuild:

```sh
mise install
dotnet restore Impactful.sln -p:GamePath="$HOME/Library/Application Support/Steam/steamapps/common/Stardew Valley/Contents/MacOS"
dotnet test Impactful.sln -c Release -p:GamePath="$HOME/Library/Application Support/Steam/steamapps/common/Stardew Valley/Contents/MacOS"
```

## Release

Set `manifest.json` to a new release version, commit it, and push the change to `main`. CI builds and tests the project, creates `Impactful-<version>.zip`, then publishes a matching `v<manifest version>` tag and GitHub release. Versions that already have a release are not republished.
