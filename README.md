# Impactful

Impactful adds restrained, short camera impulses to make key actions feel grounded without turning ordinary play into constant motion. It only shifts the world render; HUD, menus, cursor, and gameplay coordinates are untouched.

## Triggers

- pickaxe impacts on breakable stones and stone breaks;
- successful local-player melee hits;
- damage taken by the local player; and
- nearby explosions, including explosions owned by another farmer.

Mining and combat from remote farmers do not shake your camera. In split screen, each local view has its own controller.

## Install

Requires Stardew Valley 1.6.15+ and SMAPI 4.4+. Extract the release ZIP into your SMAPI `Mods` folder. [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098) is optional; when installed, its settings are registered on game launch.

## Configuration

`config.json` contains only these fields and defaults:

```json
{
  "EnableScreenShake": true,
  "ShakeStrength": 100,
  "Mining": true,
  "Combat": true,
  "PlayerDamage": true,
  "Explosions": true
}
```

`ShakeStrength` is clamped to 0–200%. Disabling shake or setting its strength to zero clears active impulses immediately.

## Test command

Use `impact_test [strength]` in the SMAPI console. With no argument it plays a medium impulse; a positive value up to 6 requests that many base screen pixels. It obeys the global enabled and strength settings, but not category switches.

## Compatibility and scope

Impactful applies and removes additive viewport offsets during world rendering, which is intended to cooperate with camera mods. It accounts for the current viewport and output dimensions so zoom and Cinderbox render scaling remain comparable. Menus and dialogue suppress application while impulses continue to decay.

Version 1 deliberately excludes forestry, resource clumps, hit-stop, rumble, particles, flashes, shaders, gameplay changes, and a general-purpose effects framework.

## Build locally

Install the pinned SDK, then supply the Steam game path to MSBuild:

```sh
mise install
dotnet restore Impactful.sln -p:GamePath="$HOME/Library/Application Support/Steam/steamapps/common/Stardew Valley/Contents/MacOS"
dotnet test Impactful.sln -c Release -p:GamePath="$HOME/Library/Application Support/Steam/steamapps/common/Stardew Valley/Contents/MacOS"
```

## Release

Set `manifest.json` to the release version, commit it, and push a matching `v<manifest version>` tag. CI builds, tests, creates `Impactful-<version>.zip`, uploads it as an artifact, and publishes it for matching tags.
