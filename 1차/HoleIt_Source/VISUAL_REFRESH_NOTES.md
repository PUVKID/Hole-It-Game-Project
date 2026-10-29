# Hole It Visual Refresh + Fixed Joystick

## What changed

- Rebuilt the generated UI in `Assets/Editor/HoleItBuilder.UI.cs`: rounded title card, colorful decor, animated buttons, high-contrast level selector, top HUD badges, mission rows, size gauge, clear/fail/pause overlays.
- Added a fixed virtual joystick at horizontal center and roughly 14.5% screen height from the bottom. Its base stays still; only the knob moves.
- Updated `InputManager` so touch/mouse input starts on the fixed pad; UI taps remain independent; release/pause clears movement.
- Updated procedural textures/material colours for a saturated violet checkerboard arena and cyan rim.
- Improved placeholder prop silhouettes with toy-like details: donut icing and centre, box lid braces, layered tree canopy, car wheels/windows/lights, and building windows.
- Merged the LevelData recovery fix: generated levels now live under `Assets/HoleIt/Resources/Data/Levels`; runtime loads them if scene references are missing.

## Apply to an existing copy

1. Back up the Unity project first.
2. Extract the patch archive into the project root and overwrite matching files under `Assets/`.
3. Open Unity 6000.3.6f1 and wait for scripts to compile.
4. Import TMP Essential Resources if Unity asks for them (`Window > TextMeshPro > Import TMP Essential Resources`).
5. Run `HoleIt > 1. 프로젝트 전체 구성`. This rebuilds `Assets/Scenes/Main.unity` and generated assets under `Assets/HoleIt/`; back up manually edited scenes/assets first.
6. Open `Assets/Scenes/Main.unity`, inspect it in Play Mode, then run `HoleIt > 2. Android APK 빌드`.

## Test status / limitations

- This environment does not have the Unity Editor or Android build toolchain available, so no Play Mode or APK build was executed here.
- The UI/prop art is procedurally generated and intended as a polished stylized prototype; it does not use or copy Hole it!'s proprietary sprites, models, logos, or textures.
- The actual Hole it! store page describes one-finger drag movement. This project keeps a fixed joystick because that is the requested control scheme.
