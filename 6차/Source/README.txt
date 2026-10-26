Gaeun's Hole - Edition 6
Unity 6000.3.6f1 with Android Build Support (SDK, NDK, OpenJDK).

Open Assets/Scenes/Main.unity to run the game. The generated content is included.
Build Android: run Build-Android.ps1. Output: Builds/Android/HoleIt.apk.
Rebuild the supplied-content catalog: ReferenceSixthBuilder.Prepare (Unity executeMethod).
Do not run the archived HoleItBuilder / ReleasePreparation.Prepare entry points: those produce the earlier game.

Active source:
- Assets/Scripts/ReferenceGame*.cs: gameplay, touch controls, rendering, menus.
- ReferenceAttendance.cs / ReferenceProfile.cs: persistent daily rewards and wallet.
- Assets/ReferenceContent: meshes, textures, 465 campaign layouts, 111 extra layouts.
- Assets/Resources/ReferenceTypography: original static Linik Sans Black SDF atlas and metrics.
- Assets/Resources/CotonArt: custom Coton loading/home artwork.
- Assets/Tests/Reference: gameplay, native touch, attendance, content and typography tests.

Daily attendance is cumulative, 31 days per cycle. Missing a date preserves progress.
One claim per local calendar date pays exactly 1,000 in-game coins. The next eligible date after day31 begins a new cycle.
Challenges are free practice and do not award campaign coins or change campaign unlocks.
The 3D collection includes all 1,010 locally available collectible models.

The APK stores actual game resources and native libraries without ZIP compression.
This makes the APK larger without dummy padding. File size alone is not a measure of visual quality.
Library, Temp, build caches and duplicate APK intermediates are excluded from the release ZIP.
