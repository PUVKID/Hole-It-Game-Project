Gaeun's Hole · edition 4 · Unity 6000.3.6f1

Open this Source folder in Unity with Android Build Support, SDK/NDK/OpenJDK.
Main scene: Assets/Scenes/Main.unity. Runtime: Assets/Scripts/ReferenceGame.cs.
Build: powershell -ExecutionPolicy Bypass -File .\Build-Android.ps1
Output: Builds/Android/HoleIt.apk. Uses a personal test signature.

Assets/ReferenceContent contains 259 item models and the first 30 locally bundled
stage layouts converted from the user-supplied Hole it! 1.31.17 XAPK. Textures,
goal icons, environment geometry, hole geometry and selected UI sprites are from
that supplied app. The runtime C# code is this project's separate implementation.
The original app's C# source was not recovered. Raw native code, ad SDKs, account
configuration and the original APK are not included in this source project.

Growth uses per-placement points, subtracts the current level's cost, carries
remaining points forward, and caps physical growth at ten increments. The visual
scale increment is .25 and movement radius increment is .223, from a .7 baseline.
The original IL2CPP growth methods were inspected to distinguish sequential costs
from cumulative thresholds. See Verification/Reference-Comparison.txt.

The provided assets retain their original ownership. Nanum Gothic Bold and its
OFL license are bundled under Assets/Resources/Fonts.

Edition 4 fixes the disabled input backend and displays the joystick at idle.
Input tests inject TouchState into an actual Input System Touchscreen device,
including PLAY/PAUSE/RESUME UI clicks, drag movement, release, cancellation and
second-finger ownership. Tests do not replace the movement input with TestDirection.
