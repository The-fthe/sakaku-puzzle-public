---
name: unityroom-webgl-build
description: Build, locally verify, and package this Unity project for unityroom WebGL upload using the official unityroom build guide and the repository's centralized Builds directory. Use when asked to create, rebuild, validate, locate Gzip files, or ZIP a unityroom WebGL release with the saved unityroom Mobile Release Build Profile, Unity 6000.5.x, Gzip compression, disabled Decompression Fallback, and disabled Development Build.
---

# unityroom WebGL Build

Create the same unityroom-compatible artifact every time. Follow the
[official unityroom guide](https://help.unityroom.com/unityroom-351dc3ed5de980eebd79eef3b153be31)
and preserve unrelated worktree changes.

## Output layout

Keep every generated player build and build archive under `Builds`:

```text
Builds/
├── Android/
├── Windows/
└── WebGL/
    ├── unityroom_build/
    │   ├── Build/
    │   ├── StreamingAssets/
    │   ├── TemplateData/
    │   └── index.html
    └── Packages/
```

The unityroom Gzip files always belong in
`Builds/WebGL/unityroom_build/Build`.

## Build

1. Confirm `ProjectSettings/ProjectVersion.txt` uses `6000.5.x`.
2. Confirm `Assets/Settings/Build Profiles/unityroom.asset` is the active Web
   `Mobile - Release` profile.
3. Confirm the saved profile has:
   - Development Build disabled
   - Compression Format set to Gzip
   - Decompression Fallback disabled
4. Confirm `Assets/Editor/GameBuild.cs` builds that profile to
   `Builds/WebGL/unityroom_build`. Use `BuildOptions.AutoRunPlayer` outside
   batch mode so the build is launched like the guide's **Build And Run**
   operation.
5. Clear the Console and compile after C# changes:

```powershell
npx --yes uloop-cli@2.2.0 clear-console --add-confirmation-message true
npx --yes uloop-cli@2.2.0 compile --force-recompile true --wait-for-domain-reload true
```

6. Run the build in the already-open Editor:

```powershell
npx --yes uloop-cli@2.2.0 execute-dynamic-code `
  --code-file ".codex/skills/unityroom-webgl-build/scripts/Invoke-UnityroomWebGLBuild.csx"
```

The uLoop request can time out after 180 seconds while Unity continues building.
Do not start another build. Inspect `%LOCALAPPDATA%\Unity\Editor\Editor.log` and
wait for `WebGL build created:` or a build failure. The canonical output is
`Builds/WebGL/unityroom_build`.

## Package and verify

After a successful build, run:

```powershell
& ".codex/skills/unityroom-webgl-build/scripts/New-UnityroomWebGLPackage.ps1"
```

The script requires `index.html`, `TemplateData`, and `Build` under
`Builds/WebGL/unityroom_build`. It also requires exactly these four files under
`Builds/WebGL/unityroom_build/Build`:

- `*.loader.js`
- `*.data.gz`
- `*.framework.js.gz`
- `*.wasm.gz`

It verifies Gzip magic bytes and the required tutorial videos, creates a ZIP
whose root contains the complete deliverable `unityroom_build` folder, excludes
Unity's generated `*_BurstDebugInformation_DoNotShip` directory, and checks the
recursive ZIP manifest. Report the complete build root, the upload folder, and
the ZIP path under `Builds/WebGL/Packages`. In unityroom's Web build upload
screen, drag and drop the complete `Builds/WebGL/unityroom_build` folder, or the
same folder after extracting the ZIP. Do not upload only its `Build` subfolder,
because runtime `StreamingAssets` such as tutorial videos would be omitted.

## Verification

- Read Unity Console errors after the build; require zero.
- Confirm all four files have the current build timestamp.
- Confirm the package script exits successfully.
- Confirm the locally launched `index.html` loads the game without browser
  console errors.
- Do not upload automatically unless the user explicitly requests it.
