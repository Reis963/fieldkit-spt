# FieldKit

FieldKit is a lightweight in-raid utility mod for SPT.

This branch targets SPT 4.1.5.

## Features

- Character health regeneration, energy drain, and hydration drain
- Character box ESP with visibility checking, configurable target information,
  and magnified-optic projection
- Active-quest highlighting for required items, visit objectives, and
  place-or-repair locations
- Menu font, scale, primary color, and hotkey customization

## Installation

Extract the release package into your SPT installation directory, then restart
the game. FieldKit installs its client plugin at:

```text
BepInEx/plugins/Hysocs-FieldKit/FieldKit.dll
```

To uninstall FieldKit, remove the `Hysocs-FieldKit` folder shown above.

## Usage

- Press `Insert` while in a raid to open or close the FieldKit menu.
- Press `Home` to toggle character ESP.
- Press `F12` to open the BepInEx configuration menu, where FieldKit settings
  and hotkeys can also be changed.

## Building

Open `FieldKit.sln` and build the project in the `Release` configuration, or
run:

```powershell
dotnet build FieldKit.sln -c Release -p:SkipDeploy=true
```

`SkipDeploy=true` prevents the build from copying the client DLL into the
configured SPT installation.

The project and client source are under `FieldKit/`. Release output is written
to `FieldKit/bin/Release/netstandard2.1/`.

Local compile-time DLLs are organized by category under `references/Bepinex`
and `references/Tarkov`. These game-provided assemblies are referenced with
copy-local disabled and are not part of the release package.

## License

FieldKit is licensed under the [Apache License 2.0](LICENSE).
Third-party components and adaptations are listed in
[THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
