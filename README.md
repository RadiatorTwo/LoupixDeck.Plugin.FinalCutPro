# LoupixDeck.Plugin.FinalCutPro

Runs Final Cut Pro commands from a LoupixDeck device. macOS only.

## Command

`FinalCutPro.Shortcut(<CommandId>)` presses the keyboard shortcut that Final Cut Pro's **active command
set** gives the command. `<CommandId>` is Apple's command-set id, e.g. `SelectToolBlade`, `PlayPause`,
`JumpToNextFrame` or `ZoomIn`. A shortcut remapped in Final Cut Pro's Command Editor keeps working
without touching the button; the command set is re-read when its file changes.

The active set is the `Active Command Set` preference of `com.apple.FinalCut`; without it the plugin
uses the `Default.commandset` inside the Final Cut Pro app bundle.

Loupedeck profiles made for Final Cut Pro store the same ids, so LoupixDeck's Loupedeck profile
import maps their shortcuts and most of their dials to this command. Colour wheels, 360° controls and
inspector tabs have no command-set equivalent and are not supported.

## Build

```bash
dotnet build -c Release
```

## Try it locally

```bash
loupix install          # packs the plugin and copies it into the LoupixDeck plugins folder
```

Restart LoupixDeck afterwards. `loupix install --debug` targets the plugins folder of a Debug build.

### Without Final Cut Pro

Set `LOUPIX_FCP_COMMANDSET` to a `.commandset` file to use it instead of the active set, on any
platform. `test/Test.commandset` holds a few harmless shortcuts (letters, space, arrows) to try the
plugin in a text editor. To load the plugin on Windows or Linux, set `platform` in `plugin.json` to
`All` locally; do not commit that.

## Release

`loupix pack` writes the Plugin Store files to `dist/`. For a store release, bump `version` in
`plugin.json`, then publish a GitHub release tagged `v<version>`; the workflow in
`.github/workflows/release.yml` builds and attaches the files.
