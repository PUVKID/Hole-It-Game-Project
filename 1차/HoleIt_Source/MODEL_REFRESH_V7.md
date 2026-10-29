# HoleIt — Model & Campaign Refresh V7

## Direction

This refresh moves the project away from placeholder-looking primitive piles toward purpose-built stylized 3D mesh assets, richer per-object silhouettes, and a longer mission-based campaign. The commercial game's proprietary meshes, textures, source code, level files, branding, and animation assets are not copied. The project recreates the public game-loop ideas with original models and data.

## New model work

The editor-time asset builder now authors and saves bespoke meshes to `Assets/HoleIt/Meshes/`, assembles visible parts into material batches, and saves each resulting object as a prefab under `Assets/HoleIt/Prefabs/`.

Added / refined in V7:
- **Red car / sports car / city bus:** lofted profile meshes for the vehicle shell, custom cabin, glass, door seams and handles, mirrors, bumpers, lights, grille, wheels, alloy hubs and spokes. The bus uses a distinct longer body profile with passenger windows.
- **Tree:** tapered trunk and branch shoulders with three separate tapered canopy tiers and fruit details, rather than a stack of round blobs.
- **House / tower:** gabled roof mesh, chimney, roof ridge, front step, door hardware, window frames and crossbars, flower box and petals; tall tower has floor-based windows and an antenna.
- **Banana:** curved tubular mesh with tapered tips and freckles.
- **Burger:** lathed bun meshes with patty, cheese, tomato, lettuce ring and sesame seeds.
- **Coffee cup:** tapered cup, sleeve, lid, coffee face, handle ring and logo badge.
- **Pizza slice:** custom triangular-prism mesh with crust, pepperoni disks and basil details.
- **French fries:** outward-flared carton mesh with label and individually posed fries.
- **Potted plant:** lathed tapered pot, soil, custom two-sided leaf meshes, stems and flower head.
- Existing torus donuts, gridded waffles, cookies, cupcakes, cakes, ice cream, candy wrappers, soda cans, stopwatch booster, milk carton, cheese wedge, rubber duck, frying pan, toy piano, scissors, fridge, cones, street lamps, bench, hydrant, bollards and road barriers remain.

The builder merges visible parts by material to reduce draw calls. Colliders remain separate from render meshes so swallowed-object detection is still handled by the existing `EdibleObject`, `HoleSwallow`, and `HoleConsume` code.

## Campaign

The campaign expands from 10 to **20 hand-designed level definitions** with fixed seeds, varied object groups, mission combinations, progression tiers and optional time-boost pickups. Added themes include Food Truck, Garden Picnic, Red Car Rush, Fry Day, Green Corner, Toy Store, Candy Factory, Crossroads, Skyline Garden and Ultimate Mix.

Core loop preserved / strengthened:
- one-finger/fixed bottom-centre joystick control
- mission-first target selection rather than mindlessly consuming every object
- objects gated by hole size tier
- smaller edible objects provide growth before larger required objects become accessible
- sequential attraction/falling/consumption sequence
- timer and optional time boosts
- mission progress, score, clear/fail, pause and saved progression

## First open

1. Extract to an ASCII-only path such as `C:\UnityProjects\HoleIt_Source`.
2. Open the folder containing `Assets`, `Packages`, and `ProjectSettings` in Unity Hub (Unity `6000.3.6f1`).
3. Wait for Package Manager and script compilation to finish. If `Assets/Scenes/Main.unity` is missing, the first-open editor bootstrap invokes the builder and opens the generated scene automatically.
4. If automatic setup fails, use the first red Console error as the actual source of truth; don't press Play on an empty scene.
5. To regenerate intentionally, back up any edits first, then select `HoleIt > 1. 프로젝트 전체 구성`.
6. Build Android using `HoleIt > 2. Android APK 빌드`.

## Validation status

This packaging environment does not have Unity Editor available. Structural/static checks were performed on the source tree, package manifest, shape coverage, object keys, level references, brace balance, and archive contents. **Unity compilation, first-open scene generation, Play Mode, and the final APK have not been executed here and still require confirmation in Unity 6000.3.6f1.**
