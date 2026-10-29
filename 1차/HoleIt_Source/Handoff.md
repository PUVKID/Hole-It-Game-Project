# HoleIt — Handoff / Project Notes (V7)

## Current build scope

This archive is the complete Unity source project (not an APK). It is intended to be extracted and opened with Unity `6000.3.6f1`. The project recreates the public core loop of a colorful mission-based black-hole puzzle using original procedural mesh models; it does not bundle the commercial game's proprietary source, 3D assets, textures, branding, exact level files, or animation clips.

**Unity Editor, Play Mode, Android build, and device runtime could not be run in this packaging environment.** The package was statically inspected only. Please treat successful local Unity compilation/scene generation as the final validation step, rather than assuming an APK has already been built.

## First open (do not skip)

1. Extract to an ASCII-only path, e.g. `C:\UnityProjects\HoleIt_Source`.
2. In Unity Hub, add/open the `HoleIt_Source` folder itself. That folder should directly contain `Assets`, `Packages`, and `ProjectSettings`.
3. Wait for package resolution and script compilation. The first-open hook detects a missing `Assets/Scenes/Main.unity`, generates all materials, meshes, prefabs, ScriptableObjects, effects, UI and the Main scene, then opens it automatically.
4. If setup prints an exception, open `Window > General > Console` and fix the first red error before pressing Play. Do not press Play on an empty scene.
5. If changing the generated project, back it up first. `HoleIt > 1. 프로젝트 전체 구성` regenerates the generated scene and data.
6. For APK creation, install Android Build Support for this editor version and select `HoleIt > 2. Android APK 빌드`. Output path: `Builds/HoleIt.apk`.

## V7 model / campaign refresh

- **46 object specifications**, with unique keys and all spawn/mission references statically checked.
- New custom-mesh objects: pizza slice with crust/pepperoni/basil; fries carton with separately posed fries; potted plant with lathed pot and two-sided leaf meshes; traffic signal with three coloured signal lamps.
- Refined cars/bus use custom lofted profiles, cabin/windows, wheels/hubs/spokes, bumpers, lights, grille, doors and mirrors. Tree uses a trunk/branches and tiered tapered canopy. House/tower have gabled roof, chimney/ridge, door/window details and antenna. Banana, burger and coffee cup use custom mesh silhouettes and layered details.
- Existing models cover torus donuts, waffles, cookies, cupcakes, layered cakes, ice cream, candy, cans, ice pops, milk cartons, cheese, sports balls, crates, rubber ducks, cooking/kitchen objects, refrigerator, stopwatch bonus and street props.
- **20 authored level definitions** are generated as ScriptableObjects, each with seeded object placement and 1–3 target missions. The later campaign includes Food Truck, Garden Picnic, Red Car Rush, Fry Day, Green Corner, Toy Store, Candy Factory, Crossroads, Skyline Garden and Ultimate Mix.
- Gameplay includes a fixed bottom-centre virtual joystick, mission progress, size-tier growth/access rules, time pickups, sequential attraction/falling/consumption, score, clear/fail, pause/resume, saved best scores and campaign unlocking.
- Render parts are combined by material into mesh assets to keep mobile renderer counts under control. Colliders are separate from visible meshes.

## Structure

- `Assets/Editor/HoleItBuilder*.cs`: full editor-time project generator, procedural custom mesh kit, UI build and APK build menu.
- `Assets/Editor/HoleItFirstOpenSetup.cs`: automatic first-open scene generation when Main scene is missing.
- `Assets/Scripts/Core`: game state, camera, input, level loading, high scores.
- `Assets/Scripts/Hole`: hole movement/visuals/swallow/consume.
- `Assets/Scripts/Data`: settings, size table, edible and level data.
- `Assets/Scripts/Mission`: mission tracking.
- `Assets/Scripts/UI`: title/HUD/result screens and bottom-centre joystick.
- `Assets/Scripts/Util`: pooling and runtime error display.
- `Assets/Shaders`: stencil hole, interior depth and rim materials.

## Input system

The generated project sets Active Input Handling to `Both` because the joystick's legacy input path and `StandaloneInputModule` need the old Input Manager, while a new-input-only configuration is also handled by `UIInputModuleSelector`/the conditional Input System path. If Unity asks to activate native input backends and restart, allow it, then re-open the project and rebuild the APK after setup is complete.

## Known validation limits

- Source shape/reference checks show 46 unique specs, no missing keys in spawn or mission data, 20 campaign definitions, and all model cases present (the generic `Wall` barrier uses the default switch branch).
- Archive/package JSON and basic C# brace balance were checked programmatically.
- A Unity compiler was unavailable here. First-open automation, custom mesh rendering, physics fit, actual gameplay balance, APK build, Android touch behaviour and FPS must still be tested on Unity 6000.3.6f1 and a device.
- This is a more complete genre-inspired implementation, not an exact 100% source/asset clone of proprietary commercial content. New model meshes and levels are original procedural work.

See `README_FIRST.txt` and `MODEL_REFRESH_V7.md` for the end-user workflow and model details.
