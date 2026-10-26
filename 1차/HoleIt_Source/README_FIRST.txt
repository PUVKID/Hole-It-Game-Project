HOLEIT — COMPLETE UNITY SOURCE PROJECT (MODEL REFRESH V7)
========================================================
Target Unity version: 6000.3.6f1

WHAT THIS IS
------------
A complete source project for a stylized mobile black-hole mission puzzle. The project uses original editor-generated mesh models and level data inspired by publicly described gameplay patterns. It does not contain proprietary code, models, textures, or original level data from the commercial Hole it! release.

OPEN AND RUN
------------
1. Extract this ZIP into an ASCII-only path, e.g. C:\UnityProjects\HoleIt_Source
2. In Unity Hub choose Add / Open project and select the HoleIt_Source folder itself.
   It must directly contain Assets, Packages, and ProjectSettings.
3. Wait for Package Manager resolution and script compilation to finish.
4. On a fresh first open, the project automatically generates model meshes, materials,
   edible prefabs, ScriptableObjects, UI, gameplay systems, and Assets/Scenes/Main.unity.
   The generated scene is opened automatically; no menu step is normally required.
5. If it opens without showing Main, or setup reports an exception, open Window > General > Console
   and fix the FIRST red error before pressing Play.
6. Press Play in Game view. The UI uses a fixed thumb joystick at the bottom-centre.
7. To build Android, install Android Build Support for this editor version and use:
   HoleIt > 2. Android APK 빌드
   Output: Builds/HoleIt.apk

CONTROLS / GAME LOOP
--------------------
- Fixed joystick at the bottom centre; move only the knob.
- Check the top mission counters and collect required object types.
- Hole size grows as valid objects are swallowed; larger objects require higher tiers.
- Swallowing is sequenced through attraction, falling, consumption, score and mission update.
- Time-boost pickups extend the timer on timed stages.
- 20 authored level configurations, pause/resume, clear/fail screens, score and saved progression.

MODELS
------
The editor builder writes custom meshes under Assets/HoleIt/Meshes and prefabs under
Assets/HoleIt/Prefabs. Visible object designs include shaped cars/bus with glass/wheels/details,
a gabled house and tower, layered trees, torus donuts, waffles, cupcakes, cakes, ice cream,
banana, burger, coffee cup, pizza slice, fries carton, potted plant, milk carton, cheese wedge,
rubber duck, toy piano, frying pan, scissors, refrigerator, stopwatch booster, and street props.
These models are low-poly stylized game assets, not copied original commercial assets.

IMPORTANT
---------
- Keep the project path ASCII-only for Android NDK compatibility.
- Back up custom work before running "HoleIt > 1. 프로젝트 전체 구성", because it regenerates
  the generated scene and data assets.
- TextMeshPro is a package dependency. The builder resolves its default font from the package;
  there is no need to create the Main scene manually.
- If the project was previously opened and automatic setup failed, use a clean extraction folder
  for this package or check the first red Console error before retrying.
- This project has been statically inspected here. Unity Editor compilation, scene generation,
  Play Mode, and APK output could not be executed in this environment; validate them locally.

See MODEL_REFRESH_V7.md for detailed model and campaign changes.
