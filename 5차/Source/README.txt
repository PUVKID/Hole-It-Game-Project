Gaeun's Hole — edition 5 — Unity 6000.3.6f1
Open Assets/Scenes/Main.unity. Build with Build-Android.ps1 and Android Build Support.
App version 5.0.0 (7), com.pocketplayground.pocketpit; ARM64 and ARMv7.
Input System Package (New) is required. ReferenceInputBuildGuard validates it.
Run ReferenceTests, ReferenceTouchTests, ReferenceFifthTests, ReferenceProfileTests.

Runtime: ReferenceGame and its Physics, Presentation, Profile partial files.
Content: ReferenceCatalog, 465 main levels and 869 collectable models. Export data
and built assets are included. No original APK or original native binaries are required.
English UI uses bundled Nanum Gothic Bold and OFL. CotonArt contains newly generated
loading/home art; see Art/Coton-Art-Prompts.txt. Reference audio and visual assets
are derived from the supplied game and remain their owners' content.

The serialized catalog is ready to build. ReferenceBuilder.Prepare reconstructs
content from JSON but resets some presentation settings. For a content regeneration,
run ReferenceFifthBuilder.Prepare then ReferenceFifthBuilder.RefreshAudio.
ReferenceBuilder.BuildAndroid is the release build entry. Do not use legacy builders.

See Verification/Reference-Comparison.txt for retained data, approximated behavior,
and verification limits. Windows previews are not Android screenshots. No physical
Android device was connected during this build.
