Hole It! - Edition 7
Unity 6000.3.6f1 with Android Build Support (SDK, NDK, OpenJDK).

Open Assets/Scenes/Main.unity to run the game. The generated content is included.
Build Android: run Build-Android.ps1. Output: Builds/Android/HoleIt.apk.
Android release builds target ARMv7 and ARM64 for phone compatibility. The unused x86_64
emulator-only target is omitted; no usable emulator was available for Android runtime QA.
For a clean Android build and packaging, at least 20 GB free space is recommended. The script limits
native build jobs to 8 by default; set process BEE_BUILD_THREADS=1 for serial jobs.
Rebuild the supplied-content catalog: ReferenceSeventhBuilder.Prepare (Unity executeMethod).
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

Fresh-build source provenance:
Earlier native/Gradle inputs were retired after a gameplay defect was found on rotated floor tiles.
The fix uses inverse authored TRS/local bounds for floor membership on the 27 affected campaign stages.
World AABBs remain coarse search bounds. A fresh full compile is required after this runtime change.
VerificationAtNativeCompile.cs.txt and VerificationSnapshotDifference.* are discarded-input history,
not a description of an intentional source/APK mismatch in the next release.

Release packaging requires a completed ../Verification/SourceBuildManifest.json matching all shipped
C# source hashes to a snapshot taken before the fresh build, plus successful build records and the
finished APK SHA256. This README alone does not assert that the replacement build has succeeded.
The manifest covers C# sources; it does not claim byte-for-byte asset or physical-device equivalence.
Reported automated test totals are source-test results, not Android runtime or physical-device results.
