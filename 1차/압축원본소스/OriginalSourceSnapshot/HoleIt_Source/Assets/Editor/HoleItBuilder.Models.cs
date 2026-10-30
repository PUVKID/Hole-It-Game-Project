#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Editor-time stylized model kit for HoleIt. Models are authored as real meshes and saved as
/// Mesh assets; primitive GameObjects are not used as the visible model pieces. Parts are merged
/// by material before the prefab is saved, keeping draw calls low on mobile.
/// </summary>
public static partial class HoleItBuilder
{
    private static readonly Dictionary<string, Mesh> modelMeshCache = new Dictionary<string, Mesh>();

    private static Mesh ModelMesh(string key, Func<Mesh> factory)
    {
        string safe = SafeAssetName(key);
        string path = MeshDir + "/" + safe + ".asset";
        Mesh cached;
        if (modelMeshCache.TryGetValue(safe, out cached) && cached != null) return cached;
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null && IsFiniteBounds(existing.bounds))
        {
            modelMeshCache[safe] = existing;
            return existing;
        }
        // 이전 빌드에서 NaN 정점으로 저장된 메쉬는 재사용하지 않고 다시 만든다 (SaveAsset 이 기존 에셋을 덮어씀)
        Mesh mesh = factory();
        mesh.name = safe;
        if (mesh.vertexCount > 65535) mesh.indexFormat = IndexFormat.UInt32;
        mesh.RecalculateBounds();
        Mesh saved = SaveAsset(mesh, path);
        modelMeshCache[safe] = saved;
        return saved;
    }

    private static bool IsFiniteBounds(Bounds b)
    {
        return !(float.IsNaN(b.center.x) || float.IsNaN(b.center.y) || float.IsNaN(b.center.z) ||
                 float.IsNaN(b.size.x) || float.IsNaN(b.size.y) || float.IsNaN(b.size.z) ||
                 float.IsInfinity(b.size.x) || float.IsInfinity(b.size.y) || float.IsInfinity(b.size.z));
    }

    private static string SafeAssetName(string value)
    {
        char[] chars = value.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
            if (!(char.IsLetterOrDigit(chars[i]) || chars[i] == '_' || chars[i] == '-')) chars[i] = '_';
        return new string(chars);
    }

    private static GameObject AddModelPart(Transform parent, string name, Mesh mesh, Material material,
        Vector3 position, Vector3 scale, Quaternion rotation)
    {
        GameObject part = new GameObject(name);
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        part.transform.localRotation = rotation;
        MeshFilter mf = part.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;
        MeshRenderer mr = part.AddComponent<MeshRenderer>();
        mr.sharedMaterial = material;
        mr.shadowCastingMode = ShadowCastingMode.On;
        mr.receiveShadows = true;
        return part;
    }

    private static void PartBox(Transform p, string n, Material m, Vector3 pos, Vector3 scale, float bevel = 0.12f, Quaternion? rot = null)
    {
        AddModelPart(p, n, ModelMesh("RoundedBox_B" + Mathf.RoundToInt(bevel * 100f), delegate { return BuildRoundedBox(bevel, 3); }), m,
            pos, scale, rot ?? Quaternion.identity);
    }

    private static void PartSphere(Transform p, string n, Material m, Vector3 pos, Vector3 scale, bool lowPoly = false)
    {
        Mesh mesh = ModelMesh(lowPoly ? "IcoSphere_Low" : "Sphere_UV_20x14", delegate { return lowPoly ? BuildIcoSphere(2) : BuildUvSphere(20, 14); });
        AddModelPart(p, n, mesh, m, pos, scale, Quaternion.identity);
    }

    private static void PartCylinder(Transform p, string n, Material m, Vector3 pos, Vector3 scale,
        float bottomRadius = 0.5f, float topRadius = 0.5f, Quaternion? rot = null)
    {
        string key = "Lathe_B" + Mathf.RoundToInt(bottomRadius * 100f) + "_T" + Mathf.RoundToInt(topRadius * 100f) + "_S28";
        Mesh mesh = ModelMesh(key, delegate { return BuildCylinder(bottomRadius, topRadius, 28); });
        AddModelPart(p, n, mesh, m, pos, scale, rot ?? Quaternion.identity);
    }

    private static void PartTorus(Transform p, string n, Material m, Vector3 pos, Vector3 scale, float major = 0.34f, float tube = 0.13f)
    {
        string key = "Torus_M" + Mathf.RoundToInt(major * 100f) + "_T" + Mathf.RoundToInt(tube * 100f) + "_32x10";
        Mesh mesh = ModelMesh(key, delegate { return BuildTorus(major, tube, 32, 10); });
        AddModelPart(p, n, mesh, m, pos, scale, Quaternion.identity);
    }

    private static GameObject BuildDetailedEdiblePrefab(Spec sp, EdibleData data, Dictionary<string, Material> mats)
    {
        Material body = ColorMat(mats, sp.key, sp.color);
        Material dark = ColorMat(mats, "Detail_DarkRubber", new Color(0.055f, 0.065f, 0.09f));
        Material glass = ColorMat(mats, "Detail_GlassBlue", new Color(0.10f, 0.56f, 0.78f));
        Material paleGlass = ColorMat(mats, "Detail_GlassHighlight", new Color(0.53f, 0.91f, 1.0f));
        Material cream = ColorMat(mats, "Food_Cream", new Color(1.00f, 0.88f, 0.62f));
        Material white = ColorMat(mats, "Detail_White", new Color(0.96f, 0.97f, 1f));
        Material wood = ColorMat(mats, "Detail_Wood", new Color(0.48f, 0.25f, 0.12f));
        Material metal = ColorMat(mats, "Detail_Metal", new Color(0.62f, 0.70f, 0.78f));
        Material leafLight = ColorMat(mats, "Detail_LeafLight", new Color(0.42f, 0.82f, 0.23f));
        Material leafDark = ColorMat(mats, "Detail_LeafDark", new Color(0.10f, 0.48f, 0.22f));

        float s = sp.size;
        float fx = s, fz = s, h = s;
        GameObject root = new GameObject("Edible_" + sp.key);
        Transform t = root.transform;

        switch (sp.shape)
        {
            case "Ball":
                // Toy sports ball with real curved panel seams, not a naked sphere.
                PartSphere(t, "BallShell", body, new Vector3(0f, s * 0.5f, 0f), Vector3.one * s, true);
                Material seam = ColorMat(mats, "Ball_Seams_" + sp.key, new Color(0.10f, 0.11f, 0.16f));
                Mesh ballSeam = ModelMesh("BallSeamTorus", delegate { return BuildTorus(0.472f, 0.016f, 36, 8); });
                AddModelPart(t, "EquatorSeam", ballSeam, seam, new Vector3(0f, s * 0.5f, 0f), Vector3.one * s, Quaternion.identity);
                AddModelPart(t, "MeridianSeamA", ballSeam, seam, new Vector3(0f, s * 0.5f, 0f), Vector3.one * s, Quaternion.Euler(90f, 0f, 0f));
                AddModelPart(t, "MeridianSeamB", ballSeam, seam, new Vector3(0f, s * 0.5f, 0f), Vector3.one * s, Quaternion.Euler(0f, 0f, 90f));
                h = s;
                break;

            case "Box":
                // A toy storage crate: slatted planks, reinforced corner posts, metal fasteners.
                PartBox(t, "CrateCore", body, new Vector3(0f, s * 0.5f, 0f), new Vector3(s, s, s), 0.10f);
                Material plank = ColorMat(mats, "CratePlank_" + sp.key, Color.Lerp(sp.color, new Color(0.9f, 0.58f, 0.24f), 0.35f));
                Material brace = ColorMat(mats, "CrateBrace_" + sp.key, Color.Lerp(sp.color, Color.black, 0.24f));
                for (int side = -1; side <= 1; side += 2)
                {
                    for (int row = 0; row < 3; row++)
                        PartBox(t, "FrontSlat_" + side + "_" + row, plank,
                            new Vector3(0f, s * (0.22f + row * 0.27f), side * s * 0.505f),
                            new Vector3(s * 0.82f, s * 0.18f, s * 0.035f), 0.06f);
                    PartBox(t, "SideRailA_" + side, brace, new Vector3(side * s * 0.505f, s * 0.5f, 0f), new Vector3(s * 0.035f, s * 0.92f, s * 0.9f), 0.06f);
                    PartBox(t, "SideRailB_" + side, brace, new Vector3(0f, s * 0.5f, side * s * 0.505f), new Vector3(s * 0.9f, s * 0.92f, s * 0.035f), 0.06f);
                }
                PartBox(t, "TopRim", plank, new Vector3(0f, s * 0.99f, 0f), new Vector3(s * 0.94f, s * 0.045f, s * 0.94f), 0.08f);
                h = s;
                break;

            case "Donut":
                // Proper toroidal pastry, icing ring, icing drops and colourful sprinkles.
                Material dough = ColorMat(mats, "DonutDough", new Color(0.72f, 0.34f, 0.13f));
                Material icing = ColorMat(mats, "DonutIcing_" + sp.key, sp.color);
                PartTorus(t, "BakedDough", dough, new Vector3(0f, s * 0.165f, 0f), Vector3.one * s, 0.335f, 0.145f);
                PartTorus(t, "IcingGlaze", icing, new Vector3(0f, s * 0.208f, 0f), Vector3.one * s, 0.325f, 0.112f);
                for (int i = 0; i < 7; i++)
                {
                    float a = i * Mathf.PI * 2f / 7f + 0.2f;
                    float r = s * (i % 2 == 0 ? 0.31f : 0.34f);
                    PartSphere(t, "IcingDrip_" + i, icing, new Vector3(Mathf.Cos(a) * r, s * (0.15f + (i % 3) * 0.012f), Mathf.Sin(a) * r),
                        new Vector3(s * 0.09f, s * 0.12f, s * 0.09f), false);
                }
                Color[] sprinkles = { new Color(1f, 0.21f, 0.31f), new Color(1f, 0.83f, 0.19f), new Color(0.18f, 0.92f, 0.69f), new Color(0.23f, 0.58f, 1f), Color.white };
                Mesh sprinkleMesh = ModelMesh("SprinkleRoundedBox", delegate { return BuildRoundedBox(0.18f, 2); });
                for (int i = 0; i < 11; i++)
                {
                    float a = i * 2.39996323f;
                    float radiusN = 0.255f + 0.09f * Mathf.Abs(Mathf.Sin(i * 1.73f));
                    float ringOffset = (radiusN - 0.325f) / 0.112f;
                    float surfaceY = 0.208f + 0.112f * Mathf.Sqrt(Mathf.Max(0f, 1f - ringOffset * ringOffset));
                    float r = s * radiusN;
                    Material sm = ColorMat(mats, "SprinkleColor" + (i % sprinkles.Length), sprinkles[i % sprinkles.Length]);
                    AddModelPart(t, "Sprinkle_" + i, sprinkleMesh, sm,
                        new Vector3(Mathf.Cos(a) * r, s * (surfaceY + 0.012f), Mathf.Sin(a) * r),
                        new Vector3(s * 0.045f, s * 0.035f, s * 0.10f), Quaternion.Euler(0f, -a * Mathf.Rad2Deg, (i % 2 == 0 ? 24f : -24f)));
                }
                h = s * 0.43f;
                break;

            case "Waffle":
                Material waffle = ColorMat(mats, "Food_Waffle", new Color(0.87f, 0.55f, 0.18f));
                Material waffleGroove = ColorMat(mats, "Food_WaffleGroove", new Color(0.60f, 0.31f, 0.08f));
                Material syrup = ColorMat(mats, "Food_Syrup", new Color(0.55f, 0.19f, 0.045f));
                PartBox(t, "GoldenWaffle", waffle, new Vector3(0f, s * 0.13f, 0f), new Vector3(s, s * 0.24f, s), 0.13f);
                // Raised grid bars create a real waffle silhouette while combining to one material draw call.
                for (int i = -2; i <= 2; i++)
                {
                    PartBox(t, "WaffleGrooveX_" + i, waffleGroove, new Vector3(i * s * 0.16f, s * 0.257f, 0f), new Vector3(s * 0.032f, s * 0.018f, s * 0.83f), 0.24f);
                    PartBox(t, "WaffleGrooveZ_" + i, waffleGroove, new Vector3(0f, s * 0.258f, i * s * 0.16f), new Vector3(s * 0.83f, s * 0.018f, s * 0.032f), 0.24f);
                }
                PartSphere(t, "ButterPat", cream, new Vector3(-s * 0.11f, s * 0.31f, s * 0.09f), new Vector3(s * 0.22f, s * 0.07f, s * 0.20f));
                PartSphere(t, "SyrupDropA", syrup, new Vector3(s * 0.25f, s * 0.29f, -s * 0.16f), Vector3.one * s * 0.08f);
                PartSphere(t, "BlueberryA", ColorMat(mats, "Food_BerryBlue", new Color(0.15f, 0.20f, 0.68f)), new Vector3(s * 0.18f, s * 0.31f, s * 0.12f), Vector3.one * s * 0.12f, true);
                h = s * 0.36f;
                break;

            case "Cookie":
                Material cookie = ColorMat(mats, "Food_CookieBaked", new Color(0.76f, 0.43f, 0.18f));
                Material chips = ColorMat(mats, "Food_Chocolate", new Color(0.19f, 0.065f, 0.035f));
                PartCylinder(t, "CookieBody", cookie, new Vector3(0f, s * 0.105f, 0f), new Vector3(s, s * 0.21f, s), 0.48f, 0.50f);
                for (int i = 0; i < 7; i++)
                {
                    float a = i * Mathf.PI * 2f / 7f;
                    float r = s * (0.12f + (i % 2) * 0.09f);
                    PartSphere(t, "ChocolateChip_" + i, chips, new Vector3(Mathf.Cos(a) * r, s * 0.218f, Mathf.Sin(a) * r),
                        new Vector3(s * 0.075f, s * 0.026f, s * 0.075f), true);
                }
                h = s * 0.24f;
                break;

            case "Cupcake":
                Material wrapper = ColorMat(mats, "Food_CupcakeWrapper", new Color(0.96f, 0.50f, 0.18f));
                Material wrapperLine = ColorMat(mats, "Food_CupcakeFlute", new Color(1.00f, 0.76f, 0.35f));
                Material frosting = ColorMat(mats, "Food_CupcakeFrosting_" + sp.key, sp.color);
                PartCylinder(t, "CupcakeWrapper", wrapper, new Vector3(0f, s * 0.25f, 0f), new Vector3(s * 0.78f, s * 0.50f, s * 0.78f), 0.28f, 0.47f);
                for (int i = 0; i < 9; i++)
                {
                    float a = i * Mathf.PI * 2f / 9f; float r = s * 0.31f;
                    PartBox(t, "WrapperRidge_" + i, wrapperLine, new Vector3(Mathf.Cos(a) * r, s * 0.25f, Mathf.Sin(a) * r),
                        new Vector3(s * 0.025f, s * 0.39f, s * 0.025f), 0.4f, Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f));
                }
                PartSphere(t, "FrostingBase", frosting, new Vector3(0f, s * 0.56f, 0f), new Vector3(s * 0.91f, s * 0.39f, s * 0.91f));
                PartSphere(t, "FrostingSwirlA", frosting, new Vector3(0f, s * 0.72f, 0f), new Vector3(s * 0.55f, s * 0.34f, s * 0.55f));
                PartSphere(t, "FrostingTip", frosting, new Vector3(s * 0.03f, s * 0.86f, 0f), new Vector3(s * 0.22f, s * 0.22f, s * 0.22f));
                PartSphere(t, "Cherry", ColorMat(mats, "Food_Cherry", new Color(0.90f, 0.035f, 0.08f)), new Vector3(0f, s * 0.97f, 0f), Vector3.one * s * 0.14f, true);
                h = s * 1.05f;
                break;

            case "Cake":
                Material cakeBase = ColorMat(mats, "Food_CakeBase", new Color(0.93f, 0.56f, 0.29f));
                Material cakeIcing = ColorMat(mats, "Food_CakeIcing_" + sp.key, sp.color);
                PartCylinder(t, "CakeLowerLayer", cakeBase, new Vector3(0f, s * 0.20f, 0f), new Vector3(s, s * 0.40f, s), 0.48f, 0.48f);
                PartCylinder(t, "CakeFilling", cream, new Vector3(0f, s * 0.34f, 0f), new Vector3(s * 1.01f, s * 0.08f, s * 1.01f), 0.49f, 0.49f);
                PartCylinder(t, "CakeUpperLayer", cakeBase, new Vector3(0f, s * 0.48f, 0f), new Vector3(s * 0.76f, s * 0.28f, s * 0.76f), 0.48f, 0.48f);
                PartCylinder(t, "FrostingTop", cakeIcing, new Vector3(0f, s * 0.635f, 0f), new Vector3(s * 0.80f, s * 0.065f, s * 0.80f), 0.48f, 0.48f);
                PartTorus(t, "FrostingPiping", white, new Vector3(0f, s * 0.65f, 0f), Vector3.one * s * 0.80f, 0.42f, 0.042f);
                for (int i = 0; i < 5; i++)
                {
                    float a = i * Mathf.PI * 2f / 5f;
                    PartSphere(t, "Berry_" + i, ColorMat(mats, "CakeBerry_" + i, i % 2 == 0 ? new Color(0.9f, 0.05f, 0.16f) : new Color(0.17f, 0.3f, 0.82f)),
                        new Vector3(Mathf.Cos(a) * s * 0.22f, s * 0.72f, Mathf.Sin(a) * s * 0.22f), Vector3.one * s * 0.12f, true);
                }
                h = s * 0.80f;
                break;

            case "IceCream":
                Material cone = ColorMat(mats, "Food_WaffleCone", new Color(0.66f, 0.34f, 0.12f));
                Material scoop = ColorMat(mats, "Food_IceCream_" + sp.key, sp.color);
                PartCylinder(t, "Cone", cone, new Vector3(0f, s * 0.30f, 0f), new Vector3(s * 0.78f, s * 0.62f, s * 0.78f), 0.035f, 0.49f);
                // Cone cross-hatch in relief.
                for (int i = 0; i < 5; i++)
                {
                    float y = s * (0.12f + i * 0.075f); float r = s * (0.12f + i * 0.058f);
                    PartTorus(t, "ConeWaferRing_" + i, ColorMat(mats, "Food_ConeLines", new Color(0.85f, 0.54f, 0.22f)),
                        new Vector3(0f, y, 0f), Vector3.one * r * 2f, 0.34f, 0.025f);
                }
                PartSphere(t, "ScoopMain", scoop, new Vector3(0f, s * 0.67f, 0f), new Vector3(s * 0.77f, s * 0.57f, s * 0.77f));
                PartSphere(t, "ScoopSide", scoop, new Vector3(s * 0.15f, s * 0.77f, s * 0.12f), new Vector3(s * 0.48f, s * 0.43f, s * 0.48f));
                PartSphere(t, "Cherry", ColorMat(mats, "Food_Cherry", new Color(0.9f, 0.035f, 0.08f)), new Vector3(s * 0.10f, s * 1.00f, 0f), Vector3.one * s * 0.14f, true);
                h = s * 1.12f;
                break;

            case "Candy":
                Material wrapperMat = ColorMat(mats, "Food_CandyWrapper_" + sp.key, sp.color);
                PartSphere(t, "CandyCenter", body, new Vector3(0f, s * 0.5f, 0f), new Vector3(s * 0.66f, s * 0.64f, s * 0.66f));
                PartBox(t, "TwistLeft", wrapperMat, new Vector3(-s * 0.40f, s * 0.5f, 0f), new Vector3(s * 0.38f, s * 0.28f, s * 0.18f), 0.2f, Quaternion.Euler(0f, 0f, -24f));
                PartBox(t, "TwistRight", wrapperMat, new Vector3(s * 0.40f, s * 0.5f, 0f), new Vector3(s * 0.38f, s * 0.28f, s * 0.18f), 0.2f, Quaternion.Euler(0f, 0f, 24f));
                h = s;
                break;

            case "Soda":
                Material label = ColorMat(mats, "Food_SodaLabel_" + sp.key, sp.color);
                PartCylinder(t, "CanBody", label, new Vector3(0f, s * 0.46f, 0f), new Vector3(s * 0.62f, s * 0.92f, s * 0.62f), 0.43f, 0.43f);
                PartCylinder(t, "CanTop", metal, new Vector3(0f, s * 0.93f, 0f), new Vector3(s * 0.55f, s * 0.06f, s * 0.55f), 0.5f, 0.5f);
                PartCylinder(t, "CanBottom", metal, new Vector3(0f, s * 0.03f, 0f), new Vector3(s * 0.55f, s * 0.06f, s * 0.55f), 0.5f, 0.5f);
                PartBox(t, "CanLogoStripe", white, new Vector3(0f, s * 0.52f, s * 0.285f), new Vector3(s * 0.31f, s * 0.23f, s * 0.018f), 0.16f, Quaternion.Euler(0f, -8f, 0f));
                h = s;
                break;

            case "Banana":
                // Curved banana as an authored tubular mesh, with distinct tapered tips and freckles.
                Material bananaPeel = ColorMat(mats, "Food_BananaPeel", new Color(1.00f, .78f, .08f));
                Material bananaEnd = ColorMat(mats, "Food_BananaStem", new Color(.30f, .19f, .07f));
                AddModelPart(t, "CurvedBananaBody", ModelMesh("BananaCurvedMesh_V7", BuildBananaMesh), bananaPeel,
                    new Vector3(0f, s * .02f, 0f), new Vector3(s, s, s), Quaternion.identity);
                PartBox(t, "BananaStem", bananaEnd, new Vector3(-s * .45f, s * .18f, 0f), new Vector3(s * .12f, s * .07f, s * .11f), .22f, Quaternion.Euler(0f, 0f, -18f));
                PartSphere(t, "BananaTip", bananaEnd, new Vector3(s * .45f, s * .18f, 0f), new Vector3(s * .075f, s * .055f, s * .065f), true);
                Material spot = ColorMat(mats, "Food_BananaFreckle", new Color(.46f, .26f, .045f));
                for (int i = 0; i < 5; i++)
                {
                    float x = -.28f + i * .14f;
                    float u = (x + .47f) / .94f;
                    float y = .19f + .43f * 4f * u * (1f - u) + .035f;
                    PartSphere(t, "BananaFreckle_" + i, spot, new Vector3(x * s, y * s, s * .075f), new Vector3(s * .025f, s * .02f, s * .012f), true);
                }
                h = s * .70f; fx = s * .94f; fz = s * .32f;
                break;

            case "Burger":
                // Layered burger with rounded lathed buns, patty, cheese, lettuce, tomato and sesame.
                Material bun = ColorMat(mats, "Food_BurgerBun", new Color(.78f, .40f, .13f));
                Material bunLight = ColorMat(mats, "Food_BurgerToast", new Color(.98f, .66f, .28f));
                Material patty = ColorMat(mats, "Food_BurgerPatty", new Color(.25f, .095f, .045f));
                Material lettuce = ColorMat(mats, "Food_BurgerLettuce", new Color(.15f, .77f, .23f));
                Material tomato = ColorMat(mats, "Food_BurgerTomato", new Color(.95f, .12f, .10f));
                Material cheeseSlice = ColorMat(mats, "Food_BurgerCheese", new Color(1f, .78f, .10f));
                AddModelPart(t, "LowerBun", ModelMesh("BurgerLowerBun_V7", BuildBurgerLowerBun), bun,
                    new Vector3(0f, 0f, 0f), Vector3.one * s, Quaternion.identity);
                PartCylinder(t, "GrilledPatty", patty, new Vector3(0f, s * .235f, 0f), new Vector3(s * .85f, s * .13f, s * .85f), .46f, .48f);
                PartBox(t, "CheeseSlice", cheeseSlice, new Vector3(0f, s * .31f, 0f), new Vector3(s * .86f, s * .045f, s * .86f), .03f, Quaternion.Euler(0f, 12f, 0f));
                PartTorus(t, "LettuceEdge", lettuce, new Vector3(0f, s * .345f, 0f), Vector3.one * s * .92f, .405f, .045f);
                PartTorus(t, "TomatoSlice", tomato, new Vector3(0f, s * .375f, 0f), Vector3.one * s * .83f, .38f, .035f);
                AddModelPart(t, "UpperBun", ModelMesh("BurgerUpperBun_V7", BuildBurgerUpperBun), bunLight,
                    new Vector3(0f, 0f, 0f), Vector3.one * s, Quaternion.identity);
                Material sesame = ColorMat(mats, "Food_BurgerSesame", new Color(1f, .88f, .55f));
                Vector3[] seeds = {
                    new Vector3(-.23f,.49f,-.14f), new Vector3(-.08f,.56f,-.19f), new Vector3(.13f,.55f,-.18f),
                    new Vector3(.27f,.49f,-.07f), new Vector3(-.18f,.55f,.04f), new Vector3(.02f,.585f,.09f),
                    new Vector3(.20f,.53f,.12f), new Vector3(-.30f,.455f,.035f), new Vector3(.04f,.57f,-.04f)
                };
                for (int i = 0; i < seeds.Length; i++)
                    PartBox(t, "SesameSeed_" + i, sesame, seeds[i] * s, new Vector3(s * .055f, s * .018f, s * .028f), .28f, Quaternion.Euler(0f, (i % 2 == 0) ? 22f : -18f, 0f));
                h = s * .66f; fx = s * .96f; fz = s * .96f;
                break;

            case "Coffee":
                // A café takeaway cup with tapered wall, rolled rim, sip lid, sleeve and a sculpted handle.
                Material cupBody = ColorMat(mats, "Food_CoffeeCupBody", new Color(.98f, .94f, .84f));
                Material cupLid = ColorMat(mats, "Food_CoffeeCupLid", new Color(.16f, .18f, .23f));
                Material cupSleeve = ColorMat(mats, "Food_CoffeeSleeve", new Color(.69f, .39f, .17f));
                Material coffeeTop = ColorMat(mats, "Food_CoffeeSurface", new Color(.20f, .085f, .035f));
                PartCylinder(t, "TaperedPaperCup", cupBody, new Vector3(0f, s * .31f, 0f), new Vector3(s * .72f, s * .62f, s * .72f), .36f, .48f);
                PartCylinder(t, "CupSleeve", cupSleeve, new Vector3(0f, s * .30f, 0f), new Vector3(s * .745f, s * .25f, s * .745f), .405f, .435f);
                PartCylinder(t, "DarkCupLid", cupLid, new Vector3(0f, s * .635f, 0f), new Vector3(s * .78f, s * .075f, s * .78f), .49f, .47f);
                PartCylinder(t, "CoffeeSurface", coffeeTop, new Vector3(0f, s * .679f, 0f), new Vector3(s * .64f, s * .018f, s * .64f), .47f, .47f);
                PartTorus(t, "CupRolledRim", white, new Vector3(0f, s * .665f, 0f), Vector3.one * s * .79f, .46f, .025f);
                Mesh handleRingMesh = ModelMesh("CoffeeHandleRing_V7", delegate { return BuildTorus(.32f, .075f, 28, 10); });
                AddModelPart(t, "CupHandle", handleRingMesh, cupBody, new Vector3(s * .34f, s * .36f, 0f), new Vector3(s * .48f, s * .52f, s * .23f), Quaternion.Euler(90f, 0f, 0f));
                PartBox(t, "CupLogoPanel", white, new Vector3(0f, s * .34f, s * .366f), new Vector3(s * .26f, s * .13f, s * .012f), .05f);
                PartSphere(t, "CoffeeLogoBeanA", coffeeTop, new Vector3(-s * .04f, s * .35f, s * .378f), new Vector3(s * .05f, s * .075f, s * .012f), true);
                PartSphere(t, "CoffeeLogoBeanB", coffeeTop, new Vector3(s * .035f, s * .35f, s * .378f), new Vector3(s * .05f, s * .075f, s * .012f), true);
                h = s * .72f; fx = s * 1.02f; fz = s * .83f;
                break;

            case "Pizza":
                // A purpose-built triangular pizza slice mesh with raised crust, sauce, pepperoni and basil.
                Material pizzaCheese = ColorMat(mats, "Food_PizzaCheese", new Color(1.00f, .69f, .20f));
                Material pizzaCrust = ColorMat(mats, "Food_PizzaCrust", new Color(.63f, .30f, .10f));
                Material pizzaSauce = ColorMat(mats, "Food_PizzaSauce", new Color(.80f, .09f, .09f));
                Material pepperoni = ColorMat(mats, "Food_Pepperoni", new Color(.67f, .035f, .045f));
                Material basil = ColorMat(mats, "Food_Basil", new Color(.09f, .39f, .12f));
                AddModelPart(t, "TriangularPizzaSlice", ModelMesh("PizzaSliceMesh_V7", BuildPizzaSliceMesh), pizzaCheese,
                    new Vector3(0f, 0f, 0f), Vector3.one * s, Quaternion.identity);
                PartBox(t, "RaisedGoldenCrust", pizzaCrust, new Vector3(-s * .38f, s * .17f, 0f), new Vector3(s * .16f, s * .13f, s * .72f), .20f);
                Vector3[] pizzaToppings = { new Vector3(-.18f,.19f,-.17f), new Vector3(-.16f,.19f,.13f), new Vector3(.03f,.19f,-.09f), new Vector3(.10f,.19f,.10f), new Vector3(.28f,.18f,0f) };
                for (int i = 0; i < pizzaToppings.Length; i++)
                    PartCylinder(t, "Pepperoni_" + i, pepperoni,
                        new Vector3(pizzaToppings[i].x * s, (pizzaToppings[i].y - .015f) * s, pizzaToppings[i].z * s),
                        new Vector3(s * .095f, s * .025f, s * .095f), .5f, .5f);
                PartSphere(t, "BasilLeafA", basil, new Vector3(-s * .01f, s * .178f, s * .20f), new Vector3(s * .07f, s * .018f, s * .035f), true);
                PartSphere(t, "BasilLeafB", basil, new Vector3(s * .18f, s * .178f, -s * .19f), new Vector3(s * .07f, s * .018f, s * .035f), true);
                h = s * .34f; fx = s * 1.02f; fz = s * .80f;
                break;

            case "Fries":
                // Flared paper carton with a front label and a crown of individually modelled fries.
                Material friesGold = ColorMat(mats, "Food_FryGold", new Color(1.00f, .69f, .16f));
                Material friesLight = ColorMat(mats, "Food_FryHighlight", new Color(1.00f, .82f, .34f));
                Material friesRed = ColorMat(mats, "Food_FryCartonRed", new Color(.91f, .08f, .18f));
                Material friesWhite = ColorMat(mats, "Food_FryLogoWhite", new Color(1f, .94f, .87f));
                AddModelPart(t, "FlaredFriesCarton", ModelMesh("FriesCartonMesh_V7", BuildFriesCartonMesh), friesRed,
                    new Vector3(0f, 0f, 0f), Vector3.one * s, Quaternion.identity);
                PartBox(t, "CartonLogo", friesWhite, new Vector3(0f, s * .20f, s * .292f), new Vector3(s * .27f, s * .10f, s * .018f), .15f);
                PartBox(t, "LogoStripe", friesGold, new Vector3(0f, s * .20f, s * .306f), new Vector3(s * .12f, s * .045f, s * .012f), .12f);
                Vector3[] fryPositions = {
                    new Vector3(-.25f,.49f,-.10f), new Vector3(-.16f,.53f,.02f), new Vector3(-.07f,.48f,-.12f),
                    new Vector3(.02f,.56f,.08f), new Vector3(.11f,.51f,-.08f), new Vector3(.19f,.49f,.08f), new Vector3(.27f,.46f,-.02f)
                };
                for (int i = 0; i < fryPositions.Length; i++)
                {
                    float fryWidth = s * (i % 3 == 0 ? .075f : .064f);
                    PartBox(t, "FrenchFry_" + i, i % 2 == 0 ? friesGold : friesLight,
                        new Vector3(fryPositions[i].x * s, fryPositions[i].y * s, fryPositions[i].z * s),
                        new Vector3(fryWidth, s * (.43f + (i % 3) * .035f), fryWidth * .82f), .20f,
                        Quaternion.Euler((i % 2 == 0) ? 5f : -7f, 0f, (i - 3) * 3.2f));
                }
                h = s * .83f; fx = s * .95f; fz = s * .72f;
                break;

            case "PlantPot":
                // Terracotta flower pot, dark soil, sculpted leaf meshes, stems and bright blossoms.
                Material terracotta = ColorMat(mats, "Garden_PotTerracotta", new Color(.78f, .27f, .12f));
                Material soil = ColorMat(mats, "Garden_PotSoil", new Color(.19f, .10f, .055f));
                Material stem = ColorMat(mats, "Garden_Stem", new Color(.12f, .45f, .16f));
                Material leafGreen = ColorMat(mats, "Garden_LeafGreen", new Color(.13f, .67f, .23f));
                Material leafLightGreen = ColorMat(mats, "Garden_LeafLight", new Color(.38f, .86f, .22f));
                Material blossom = ColorMat(mats, "Garden_Blossom", new Color(1f, .21f, .47f));
                AddModelPart(t, "TaperedFlowerPot", ModelMesh("PlantPotBody_V7", delegate { return BuildLatheProfile(new Vector2[] { new Vector2(.015f,.20f), new Vector2(.045f,.25f), new Vector2(.29f,.33f), new Vector2(.34f,.34f), new Vector2(.35f,.28f) }, 28); }), terracotta,
                    new Vector3(0f, 0f, 0f), Vector3.one * s, Quaternion.identity);
                PartCylinder(t, "PotSoil", soil, new Vector3(0f, s * .335f, 0f), new Vector3(s * .27f, s * .035f, s * .27f), .48f, .48f);
                for (int i = 0; i < 4; i++)
                {
                    float a = i * Mathf.PI * .5f + .3f;
                    float x = Mathf.Cos(a) * .13f, z = Mathf.Sin(a) * .13f;
                    PartCylinder(t, "PlantStem_" + i, stem, new Vector3(x * s, s * (.50f + (i % 2) * .07f), z * s), new Vector3(s * .028f, s * (.40f + (i % 2) * .12f), s * .028f), .42f, .26f,
                        Quaternion.Euler(Mathf.Sin(a) * 20f, 0f, Mathf.Cos(a) * -20f));
                    AddModelPart(t, "PlantLeaf_" + i, ModelMesh("PlantLeafMesh_V7", BuildLeafMesh), i % 2 == 0 ? leafGreen : leafLightGreen,
                        new Vector3(x * s * 1.9f, s * (.53f + (i % 2) * .06f), z * s * 1.9f),
                        new Vector3(s * .30f, s * .27f, s * .14f), Quaternion.Euler(0f, a * Mathf.Rad2Deg, (i % 2 == 0 ? -35f : 35f)));
                }
                PartSphere(t, "FlowerCentre", ColorMat(mats, "Garden_FlowerCentre", new Color(1f, .79f, .13f)), new Vector3(s * .06f, s * .83f, 0f), Vector3.one * s * .09f, true);
                for (int i = 0; i < 5; i++)
                {
                    float a = i * Mathf.PI * 2f / 5f;
                    PartSphere(t, "FlowerPetal_" + i, blossom, new Vector3(s * (.06f + Mathf.Cos(a) * .09f), s * .83f, Mathf.Sin(a) * s * .09f), new Vector3(s * .10f, s * .045f, s * .075f), true);
                }
                h = s * 1.08f; fx = s * .95f; fz = s * .95f;
                break;

            case "TrafficLight":
                // Three-aspect traffic signal with visor hoods, front lenses, pole and weighted base.
                Material signalMetal = ColorMat(mats, "Street_SignalMetal", new Color(.13f, .16f, .22f));
                Material signalRed = ColorMat(mats, "Street_SignalRed", new Color(1f, .08f, .12f));
                Material signalAmber = ColorMat(mats, "Street_SignalAmber", new Color(1f, .67f, .06f));
                Material signalGreen = ColorMat(mats, "Street_SignalGreen", new Color(.10f, .92f, .35f));
                PartCylinder(t, "SignalPole", signalMetal, new Vector3(0f, s * .67f, 0f), new Vector3(s * .075f, s * 1.34f, s * .075f), .5f, .4f);
                PartCylinder(t, "SignalBase", signalMetal, new Vector3(0f, s * .06f, 0f), new Vector3(s * .34f, s * .12f, s * .34f), .5f, .5f);
                PartBox(t, "SignalHousing", signalMetal, new Vector3(0f, s * 1.22f, s * .09f), new Vector3(s * .35f, s * .80f, s * .25f), .10f);
                Material[] lenses = { signalRed, signalAmber, signalGreen };
                for (int i = 0; i < 3; i++)
                {
                    float y = s * (1.47f - i * .25f);
                    PartSphere(t, "SignalLens_" + i, lenses[i], new Vector3(0f, y, s * .225f), new Vector3(s * .19f, s * .18f, s * .045f), true);
                    PartBox(t, "LensVisor_" + i, signalMetal, new Vector3(0f, y + s * .08f, s * .24f), new Vector3(s * .23f, s * .035f, s * .08f), .10f);
                }
                h = s * 1.68f; fx = s * .52f; fz = s * .52f;
                break;

            case "TimeBoost":
                // Bright stopwatch pickup; the beveled case, bezel, face and hands make it readable at a glance.
                Material boostGreen = ColorMat(mats, "Boost_Green", new Color(0.22f, 0.95f, 0.48f));
                Material boostFace = ColorMat(mats, "Boost_Face", new Color(0.96f, 1.00f, 0.88f));
                Material boostGold = ColorMat(mats, "Boost_Gold", new Color(1.00f, 0.72f, 0.12f));
                PartBox(t, "StopwatchTopButton", boostGold, new Vector3(0f, s * 0.93f, 0f), new Vector3(s * 0.18f, s * 0.16f, s * 0.16f), .16f);
                PartSphere(t, "StopwatchBody", boostGreen, new Vector3(0f, s * 0.48f, 0f), new Vector3(s * .90f, s * .90f, s * .30f), true);
                Mesh bezelMesh = ModelMesh("StopwatchBezelMesh", delegate { return BuildTorus(.34f, .045f, 32, 10); });
                AddModelPart(t, "StopwatchBezel", bezelMesh, boostGold, new Vector3(0f, s * .48f, s * .18f), new Vector3(s * .92f, s * .92f, s * .92f), Quaternion.Euler(90f, 0f, 0f));
                PartSphere(t, "StopwatchFace", boostFace, new Vector3(0f, s * .48f, s * .145f), new Vector3(s * .69f, s * .69f, s * .12f));
                PartBox(t, "ClockHandMinute", dark, new Vector3(s * .02f, s * .57f, s * .211f), new Vector3(s * .045f, s * .24f, s * .02f), .35f, Quaternion.Euler(0f, 0f, -18f));
                PartBox(t, "ClockHandHour", dark, new Vector3(s * .09f, s * .47f, s * .215f), new Vector3(s * .18f, s * .045f, s * .02f), .35f, Quaternion.Euler(0f, 0f, -24f));
                PartSphere(t, "ClockPin", boostGold, new Vector3(0f, s * .49f, s * .225f), Vector3.one * s * .09f);
                h = s * 1.08f; fx = s; fz = s * .42f;
                break;

            case "Car":
                // Recognisable toy car: sculpted rounded body, raised cabin, glass, separate bumpers,
                // side windows, rubber wheels, metallic hubs, front grille and lights.
                Material bumper = ColorMat(mats, "CarBumper", new Color(0.18f, 0.20f, 0.24f));
                Material chrome = ColorMat(mats, "CarChrome", new Color(0.68f, 0.78f, 0.85f));
                Material lamp = ColorMat(mats, "CarHeadlamp", new Color(1.0f, 0.92f, 0.62f));
                Material tail = ColorMat(mats, "CarTailLamp", new Color(0.98f, 0.035f, 0.06f));
                bool bus = sp.key == "Bus";
                float length = bus ? s * 1.08f : s * 0.98f;
                float width = s * 0.64f;
                PartBox(t, "Chassis", bumper, new Vector3(0f, s * 0.13f, 0f), new Vector3(length * 0.95f, s * 0.16f, width * 0.93f), 0.12f);
                // V7: silhouette meshes give cars a sloped hood, rounded shoulders and a narrower roofline.
                Mesh bodyMesh = ModelMesh(bus ? "BusBody_Profile_V7" : "ToyCarBody_Profile_V7", bus ? new Func<Mesh>(BuildBusBodyMesh) : new Func<Mesh>(BuildToyCarBodyMesh));
                AddModelPart(t, "PaintedBody", bodyMesh, body, new Vector3(0f, s * (bus ? .32f : .25f), 0f), new Vector3(length, s, s), Quaternion.identity);
                PartBox(t, "FrontBumper", chrome, new Vector3(length * 0.49f, s * 0.23f, 0f), new Vector3(s * 0.07f, s * 0.10f, width * 0.83f), 0.2f);
                PartBox(t, "RearBumper", bumper, new Vector3(-length * 0.49f, s * 0.23f, 0f), new Vector3(s * 0.07f, s * 0.10f, width * 0.83f), 0.2f);
                if (bus)
                {
                    PartBox(t, "BusRoof", ColorMat(mats, "BusRoof", Color.Lerp(sp.color, Color.white, .12f)), new Vector3(-s * 0.015f, s * .64f, 0f), new Vector3(length * .79f, s * .42f, width * .80f), 0.17f);
                    for (int side = -1; side <= 1; side += 2)
                    {
                        for (int i = -2; i <= 2; i++)
                        {
                            PartBox(t, "BusWindow_" + side + "_" + i, glass, new Vector3(i * length * .145f, s * .66f, side * width * .405f), new Vector3(length * .115f, s * .22f, s * .018f), .12f);
                            PartBox(t, "BusWindowGlint_" + side + "_" + i, paleGlass, new Vector3(i * length * .145f + length * .012f, s * .705f, side * width * .418f), new Vector3(length * .018f, s * .12f, s * .009f), .04f);
                        }
                        PartBox(t, "BusPassengerDoor_" + side, ColorMat(mats, "BusDoorFrame", Color.Lerp(sp.color, Color.black, .18f)), new Vector3(-length * .30f, s * .36f, side * width * .414f), new Vector3(length * .105f, s * .42f, s * .025f), .07f);
                        PartBox(t, "BusDoorGlass_" + side, paleGlass, new Vector3(-length * .30f, s * .51f, side * width * .432f), new Vector3(length * .075f, s * .12f, s * .014f), .04f);
                    }
                    PartBox(t, "BusWindshield", paleGlass, new Vector3(length * .405f, s * .65f, 0f), new Vector3(s * .018f, s * .22f, width * .55f), .12f);
                    PartBox(t, "BusWindshieldBar", body, new Vector3(length * .417f, s * .65f, 0f), new Vector3(s * .016f, s * .23f, s * .022f), .08f);
                    PartBox(t, "BusDestinationSign", dark, new Vector3(length * .417f, s * .81f, 0f), new Vector3(s * .02f, s * .055f, width * .48f), .04f);
                    PartBox(t, "BusRouteColourStripe", ColorMat(mats, "BusRouteStripe", new Color(.95f, .25f, .10f)), new Vector3(0f, s * .48f, width * .414f), new Vector3(length * .78f, s * .045f, s * .015f), .05f);
                }
                else
                {
                    Mesh cabinMesh = ModelMesh("ToyCarCabin_Profile_V7", BuildToyCarCabinMesh);
                    AddModelPart(t, "CabinShell", cabinMesh, body, new Vector3(0f, s * .53f, 0f), new Vector3(length, s, s), Quaternion.identity);
                    PartBox(t, "Windshield", paleGlass, new Vector3(length * .185f, s * .595f, 0f), new Vector3(s * .025f, s * .19f, width * .54f), .09f, Quaternion.Euler(0f, 0f, 18f));
                    PartBox(t, "RearWindow", glass, new Vector3(-length * .245f, s * .59f, 0f), new Vector3(s * .022f, s * .16f, width * .51f), .08f, Quaternion.Euler(0f, 0f, -18f));
                    for (int side = -1; side <= 1; side += 2)
                    {
                        PartBox(t, "FrontSideWindow_" + side, glass, new Vector3(length * .055f, s * .61f, side * width * .323f), new Vector3(length * .145f, s * .135f, s * .018f), .10f);
                        PartBox(t, "RearSideWindow_" + side, paleGlass, new Vector3(-length * .135f, s * .60f, side * width * .323f), new Vector3(length * .12f, s * .12f, s * .018f), .10f);
                        PartBox(t, "DoorSeam_" + side, ColorMat(mats, "CarDoorSeam", Color.Lerp(sp.color, Color.black, .26f)), new Vector3(-length * .015f, s * .235f, side * width * .498f), new Vector3(s * .009f, s * .14f, s * .012f), .03f);
                        PartBox(t, "DoorHandle_" + side, chrome, new Vector3(-length * .015f, s * .315f, side * width * .504f), new Vector3(s * .07f, s * .025f, s * .016f), .25f);
                        PartBox(t, "MirrorArm_" + side, bumper, new Vector3(length * .19f, s * .48f, side * width * .385f), new Vector3(s * .09f, s * .035f, s * .075f), .08f, Quaternion.Euler(0f, 0f, side * 10f));
                        PartBox(t, "SideMirror_" + side, body, new Vector3(length * .19f, s * .50f, side * width * .445f), new Vector3(s * .09f, s * .075f, s * .055f), .16f);
                    }
                    PartBox(t, "FrontPlate", white, new Vector3(length * .506f, s * .245f, 0f), new Vector3(s * .016f, s * .045f, s * .15f), .04f);
                }
                for (int side = -1; side <= 1; side += 2)
                {
                    for (int axle = -1; axle <= 1; axle += 2)
                    {
                        Vector3 wheelPos = new Vector3(axle * length * .30f, s * .16f, side * width * .48f);
                        PartCylinder(t, "RubberWheel_" + axle + "_" + side, dark, wheelPos, new Vector3(s * .22f, s * .12f, s * .22f), .5f, .5f, Quaternion.Euler(90f, 0f, 0f));
                        Vector3 outer = wheelPos + new Vector3(0f, 0f, side * s * .063f);
                        PartCylinder(t, "WheelHub_" + axle + "_" + side, chrome, outer, new Vector3(s * .115f, s * .126f, s * .115f), .5f, .5f, Quaternion.Euler(90f, 0f, 0f));
                        Mesh rimRing = ModelMesh("AlloyWheelRimRing_V7", delegate { return BuildTorus(.32f, .055f, 20, 6); });
                        AddModelPart(t, "AlloyRimRing_" + axle + "_" + side, rimRing, metal, outer + new Vector3(0f, 0f, side * s * .064f), Vector3.one * s * .25f, Quaternion.Euler(90f, 0f, 0f));
                        for (int spoke = 0; spoke < 5; spoke++)
                        {
                            float a = spoke * Mathf.PI * 2f / 5f;
                            PartBox(t, "RimSpoke_" + axle + "_" + side + "_" + spoke, white,
                                outer + new Vector3(Mathf.Cos(a) * s * .055f, Mathf.Sin(a) * s * .055f, side * s * .067f),
                                new Vector3(s * .018f, s * .075f, s * .012f), .18f, Quaternion.Euler(0f, 0f, -a * Mathf.Rad2Deg));
                        }
                    }
                    PartBox(t, "Headlight_" + side, lamp, new Vector3(length * .495f, s * .34f, side * width * .27f), new Vector3(s * .025f, s * .10f, s * .14f), .18f);
                    PartBox(t, "Taillight_" + side, tail, new Vector3(-length * .495f, s * .32f, side * width * .27f), new Vector3(s * .025f, s * .095f, s * .13f), .18f);
                }
                PartBox(t, "FrontGrille", dark, new Vector3(length * .501f, s * .26f, 0f), new Vector3(s * .02f, s * .07f, s * .22f), .1f);
                fx = length; fz = width * 1.14f; h = bus ? s * .88f : s * .78f;
                break;

            case "Tree":
                // Layered low-poly ornamental tree with tapered trunk, branch shoulders and faceted canopy.
                Material bark = ColorMat(mats, "TreeBark", new Color(0.37f, 0.18f, 0.08f));
                PartCylinder(t, "TaperedTrunk", bark, new Vector3(0f, s * .42f, 0f), new Vector3(s * .27f, s * .84f, s * .27f), .22f, .46f);
                PartCylinder(t, "BranchLeft", bark, new Vector3(-s * .18f, s * .67f, 0f), new Vector3(s * .13f, s * .36f, s * .13f), .20f, .32f, Quaternion.Euler(0f, 0f, -28f));
                PartCylinder(t, "BranchRight", bark, new Vector3(s * .18f, s * .70f, 0f), new Vector3(s * .13f, s * .34f, s * .13f), .20f, .32f, Quaternion.Euler(0f, 0f, 28f));
                // Layered faceted canopy: a clearer tree silhouette than a stack of round primitives.
                PartCylinder(t, "CanopyLowerTier", leafDark, new Vector3(0f, s * .78f, 0f), new Vector3(s * 1.18f, s * .78f, s * 1.18f), .50f, .03f);
                PartCylinder(t, "CanopyMiddleTier", body, new Vector3(0f, s * 1.08f, 0f), new Vector3(s * .90f, s * .78f, s * .90f), .50f, .025f);
                PartCylinder(t, "CanopyUpperTier", leafLight, new Vector3(0f, s * 1.38f, 0f), new Vector3(s * .61f, s * .75f, s * .61f), .50f, .015f);
                Material fruitA = ColorMat(mats, "TreeFruitWarm", new Color(.98f, .31f, .12f));
                for (int i = 0; i < 5; i++)
                {
                    float a = i * Mathf.PI * 2f / 5f;
                    float y = s * (.84f + (i % 3) * .19f);
                    float r = s * (.43f - (i % 3) * .075f);
                    PartSphere(t, "FruitDot_" + i, fruitA, new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r), Vector3.one * s * .075f, true);
                }
                h = s * 1.80f;
                break;

            case "Building":
                Material walls = body;
                Material roofMat = ColorMat(mats, "HouseRoof_" + sp.key, Color.Lerp(sp.color, new Color(.20f, .09f, .23f), .45f));
                Material windowMat = ColorMat(mats, "HouseWindow_" + sp.key, new Color(.18f, .76f, .95f));
                Material trim = ColorMat(mats, "HouseTrim_" + sp.key, Color.Lerp(sp.color, Color.white, .45f));
                bool tower = sp.key == "BuildL";
                if (tower)
                {
                    PartBox(t, "TowerBody", walls, new Vector3(0f, s * .90f, 0f), new Vector3(s, s * 1.80f, s), .06f);
                    PartBox(t, "TowerCrown", roofMat, new Vector3(0f, s * 1.84f, 0f), new Vector3(s * 1.04f, s * .12f, s * 1.04f), .06f);
                    for (int floor = 0; floor < 5; floor++)
                        for (int winCol = -1; winCol <= 1; winCol++)
                            PartBox(t, "TowerWindowF_" + floor + "_" + winCol, windowMat,
                                new Vector3(winCol * s * .27f, s * (.23f + floor * .32f), s * .506f), new Vector3(s * .15f, s * .18f, s * .02f), .05f);
                    PartCylinder(t, "RoofAntenna", metal, new Vector3(0f, s * 2.04f, 0f), new Vector3(s * .045f, s * .32f, s * .045f), .5f, .20f);
                    h = s * 2.2f;
                }
                else
                {
                    PartBox(t, "HouseBody", walls, new Vector3(0f, s * .48f, 0f), new Vector3(s, s * .96f, s), .07f);
                    AddModelPart(t, "PitchedRoof", ModelMesh("GableRoof", BuildGableRoof), roofMat,
                        new Vector3(0f, s * 1.02f, 0f), new Vector3(s * 1.12f, s * .48f, s * 1.12f), Quaternion.identity);
                    PartBox(t, "FrontDoor", wood, new Vector3(s * .17f, s * .25f, s * .508f), new Vector3(s * .22f, s * .50f, s * .025f), .04f);
                    PartBox(t, "DoorKnob", metal, new Vector3(s * .25f, s * .27f, s * .53f), new Vector3(s * .035f, s * .035f, s * .02f), .2f);
                    for (int side = -1; side <= 1; side += 2)
                    {
                        PartBox(t, "WindowFrame_" + side, trim, new Vector3(-s * .22f, s * .62f, s * .51f), new Vector3(s * .26f, s * .25f, s * .035f), .04f);
                        PartBox(t, "WindowGlass_" + side, windowMat, new Vector3(-s * .22f, s * .62f, s * .533f), new Vector3(s * .20f, s * .19f, s * .015f), .04f);
                        PartBox(t, "WindowCrossV_" + side, trim, new Vector3(-s * .22f, s * .62f, s * .544f), new Vector3(s * .025f, s * .21f, s * .012f), .02f);
                        PartBox(t, "WindowCrossH_" + side, trim, new Vector3(-s * .22f, s * .62f, s * .544f), new Vector3(s * .21f, s * .025f, s * .012f), .02f);
                    }
                    // Chimney, roof ridge, step and flower box create a readable toy house silhouette.
                    PartBox(t, "Chimney", ColorMat(mats, "HouseChimney", new Color(.72f, .28f, .22f)),
                        new Vector3(-s * .29f, s * 1.19f, -s * .15f), new Vector3(s * .15f, s * .47f, s * .15f), .06f);
                    PartBox(t, "RoofRidgeCap", trim, new Vector3(0f, s * 1.255f, 0f), new Vector3(s * .055f, s * .055f, s * 1.06f), .15f);
                    PartBox(t, "FrontStep", trim, new Vector3(s * .17f, s * .035f, s * .57f), new Vector3(s * .34f, s * .07f, s * .16f), .06f);
                    PartBox(t, "FlowerBox", wood, new Vector3(-s * .22f, s * .465f, s * .56f), new Vector3(s * .30f, s * .08f, s * .09f), .05f);
                    Material flowerRed = ColorMat(mats, "HouseFlowerRed", new Color(.98f, .19f, .26f));
                    Material flowerYellow = ColorMat(mats, "HouseFlowerYellow", new Color(1f, .76f, .14f));
                    for (int flower = 0; flower < 3; flower++)
                        PartSphere(t, "WindowFlower_" + flower, flower % 2 == 0 ? flowerRed : flowerYellow,
                            new Vector3((-0.31f + flower * .09f) * s, s * .55f, s * .57f), Vector3.one * s * .065f, true);
                    h = s * 1.30f;
                }
                break;

            case "Popsicle":
                // A distinct rounded frozen-pop silhouette, wooden stick, and glossy contrasting stripe.
                Material stick = ColorMat(mats, "Food_PopsicleStick", new Color(0.70f, 0.43f, 0.21f));
                Material stripe = ColorMat(mats, "Food_PopsicleStripe", Color.Lerp(sp.color, Color.white, 0.40f));
                AddModelPart(t, "RoundedIcePop", ModelMesh("PopsicleCapsule", BuildPopsicle), body,
                    new Vector3(0f, s * 0.34f, 0f), new Vector3(s * 0.66f, s * 0.66f, s * 0.28f), Quaternion.identity);
                PartBox(t, "WoodenStick", stick, new Vector3(0f, s * 0.115f, 0f), new Vector3(s * 0.17f, s * 0.28f, s * 0.065f), 0.17f);
                PartBox(t, "PopsicleStripe", stripe, new Vector3(0f, s * 0.45f, s * 0.144f), new Vector3(s * 0.13f, s * 0.38f, s * 0.018f), 0.15f, Quaternion.Euler(0f, 0f, -13f));
                h = s * 0.72f; fz = s * 0.34f;
                break;

            case "MilkCarton":
                // Gable-top paper carton with a folded roof, screw cap, brand panel, and milk-drop badge.
                Material cartonWhite = ColorMat(mats, "Food_MilkCartonWhite", new Color(0.96f, 0.97f, 0.93f));
                Material cartonBlue = ColorMat(mats, "Food_MilkCartonBlue", new Color(0.12f, 0.57f, 0.92f));
                Material cartonRed = ColorMat(mats, "Food_MilkCartonRed", new Color(0.98f, 0.24f, 0.31f));
                PartBox(t, "CartonBody", cartonWhite, new Vector3(0f, s * 0.34f, 0f), new Vector3(s * 0.70f, s * 0.66f, s * 0.58f), 0.07f);
                AddModelPart(t, "FoldedCartonTop", ModelMesh("MilkCartonGable", BuildGableRoof), cartonWhite,
                    new Vector3(0f, s * 0.70f, 0f), new Vector3(s * 0.72f, s * 0.22f, s * 0.60f), Quaternion.identity);
                PartCylinder(t, "ScrewCap", cartonBlue, new Vector3(0f, s * 0.85f, 0f), new Vector3(s * 0.18f, s * 0.09f, s * 0.18f), 0.42f, 0.42f);
                PartBox(t, "BlueBrandBand", cartonBlue, new Vector3(0f, s * 0.37f, s * 0.294f), new Vector3(s * 0.60f, s * 0.22f, s * 0.025f), 0.04f);
                PartSphere(t, "MilkDropBadge", white, new Vector3(0f, s * 0.43f, s * 0.316f), new Vector3(s * 0.16f, s * 0.18f, s * 0.035f));
                PartBox(t, "RedCartonTab", cartonRed, new Vector3(s * 0.18f, s * 0.70f, s * 0.20f), new Vector3(s * 0.17f, s * 0.07f, s * 0.025f), 0.04f);
                h = s * 0.91f; fx = s * 0.74f; fz = s * 0.62f;
                break;

            case "Cheese":
                // Custom triangular wedge mesh, golden rind, and visible cheese holes.
                Material rind = ColorMat(mats, "Food_CheeseRind", new Color(0.93f, 0.48f, 0.055f));
                Material hole = ColorMat(mats, "Food_CheeseHole", new Color(0.60f, 0.29f, 0.035f));
                AddModelPart(t, "CheeseWedge", ModelMesh("CheeseWedgeMesh", BuildCheeseWedge), body,
                    new Vector3(0f, s * 0.465f, 0f), new Vector3(s * 0.94f, s * 0.93f, s * 0.90f), Quaternion.identity);
                PartBox(t, "CheeseRindFront", rind, new Vector3(0f, s * 0.045f, s * 0.451f), new Vector3(s * 0.87f, s * 0.035f, s * 0.018f), 0.04f);
                Vector3[] holes = { new Vector3(-.22f,.19f,.30f), new Vector3(.12f,.13f,.31f), new Vector3(-.04f,.29f,.24f), new Vector3(.25f,.065f,.26f) };
                for (int i = 0; i < holes.Length; i++)
                    PartSphere(t, "CheeseHole_" + i, hole, new Vector3(holes[i].x * s, holes[i].y * s, holes[i].z * s), new Vector3(s * .075f, s * .065f, s * .025f), true);
                h = s * 0.52f; fx = s * .94f; fz = s * .90f;
                break;

            case "Duck":
                // Cute rubber bath duck with breast, neck/head, orange beak, wings and glossy eyes.
                Material duckYellow = ColorMat(mats, "Toy_DuckYellow_" + sp.key, sp.color);
                Material duckOrange = ColorMat(mats, "Toy_DuckOrange", new Color(1f, .31f, .035f));
                Material duckWhite = ColorMat(mats, "Toy_DuckEyeWhite", Color.white);
                PartSphere(t, "DuckBody", duckYellow, new Vector3(-s * .03f, s * .29f, 0f), new Vector3(s * .90f, s * .52f, s * .66f));
                PartSphere(t, "DuckBreast", ColorMat(mats, "Toy_DuckBreast", new Color(1f, .88f, .20f)), new Vector3(s * .17f, s * .31f, s * .18f), new Vector3(s * .49f, s * .40f, s * .43f));
                PartCylinder(t, "DuckNeck", duckYellow, new Vector3(s * .20f, s * .48f, 0f), new Vector3(s * .31f, s * .38f, s * .31f), .36f, .48f, Quaternion.Euler(0f, 0f, -16f));
                PartSphere(t, "DuckHead", duckYellow, new Vector3(s * .22f, s * .67f, 0f), new Vector3(s * .43f, s * .40f, s * .42f));
                PartBox(t, "DuckBeak", duckOrange, new Vector3(s * .25f, s * .61f, s * .23f), new Vector3(s * .23f, s * .09f, s * .21f), .16f, Quaternion.Euler(7f, 0f, -7f));
                for (int side = -1; side <= 1; side += 2)
                {
                    PartSphere(t, "DuckEyeWhite_" + side, duckWhite, new Vector3(s * .29f, s * .72f, side * s * .155f), new Vector3(s * .105f, s * .12f, s * .055f), true);
                    PartSphere(t, "DuckEyePupil_" + side, dark, new Vector3(s * .32f, s * .72f, side * s * .195f), new Vector3(s * .047f, s * .065f, s * .022f), true);
                    PartSphere(t, "DuckWing_" + side, ColorMat(mats, "Toy_DuckWing_" + side, new Color(1f, .84f, .14f)), new Vector3(-s * .12f, s * .31f, side * s * .245f), new Vector3(s * .34f, s * .28f, s * .15f));
                }
                h = s * .88f; fx = s * .95f; fz = s * .82f;
                break;

            case "Pan":
                // Top-visible non-stick frying pan and long handle with a hanging hole.
                Material panBody = ColorMat(mats, "Kitchen_PanBody", new Color(.13f, .17f, .24f));
                Material panInterior = ColorMat(mats, "Kitchen_PanInterior", new Color(.34f, .39f, .46f));
                PartCylinder(t, "PanSkirt", panBody, new Vector3(0f, s * .16f, 0f), new Vector3(s * .84f, s * .22f, s * .84f), .48f, .50f);
                PartCylinder(t, "NonStickInterior", panInterior, new Vector3(0f, s * .285f, 0f), new Vector3(s * .73f, s * .035f, s * .73f), .49f, .49f);
                PartCylinder(t, "PanCenter", ColorMat(mats, "Kitchen_PanCenter", new Color(.20f, .25f, .32f)), new Vector3(0f, s * .307f, 0f), new Vector3(s * .42f, s * .012f, s * .42f), .5f, .5f);
                PartBox(t, "HandleNeck", panBody, new Vector3(s * .51f, s * .18f, 0f), new Vector3(s * .43f, s * .11f, s * .17f), .10f);
                PartBox(t, "HandleGrip", dark, new Vector3(s * .78f, s * .18f, 0f), new Vector3(s * .30f, s * .14f, s * .22f), .10f);
                PartSphere(t, "HandleHole", metal, new Vector3(s * .88f, s * .255f, 0f), new Vector3(s * .055f, s * .025f, s * .055f), true);
                h = s * .36f; fx = s * 1.14f; fz = s * .90f;
                break;

            case "Piano":
                // Toy upright keyboard with separate ivory/black keys and music stand.
                Material pianoBody = ColorMat(mats, "Toy_PianoBody", new Color(.12f, .075f, .23f));
                Material ivory = ColorMat(mats, "Toy_PianoIvory", new Color(1f, .97f, .86f));
                Material keyBlack = ColorMat(mats, "Toy_PianoBlack", new Color(.035f, .04f, .07f));
                PartBox(t, "PianoCabinet", pianoBody, new Vector3(0f, s * .31f, 0f), new Vector3(s * 1.05f, s * .56f, s * .74f), .10f);
                PartBox(t, "KeyboardBase", dark, new Vector3(0f, s * .61f, s * .25f), new Vector3(s * 1.00f, s * .08f, s * .32f), .04f);
                const int whiteKeys = 8;
                for (int i = 0; i < whiteKeys; i++)
                {
                    float x = (i - (whiteKeys - 1) * .5f) * s * .118f;
                    PartBox(t, "WhiteKey_" + i, ivory, new Vector3(x, s * .666f, s * .31f), new Vector3(s * .105f, s * .035f, s * .24f), .018f);
                }
                int[] blackAfter = { 0, 1, 3, 4, 5 };
                for (int i = 0; i < blackAfter.Length; i++)
                {
                    float x = (blackAfter[i] - (whiteKeys - 1) * .5f + .5f) * s * .118f;
                    PartBox(t, "BlackKey_" + i, keyBlack, new Vector3(x, s * .691f, s * .245f), new Vector3(s * .057f, s * .025f, s * .13f), .016f);
                }
                PartBox(t, "MusicStand", pianoBody, new Vector3(0f, s * .64f, -s * .26f), new Vector3(s * .72f, s * .23f, s * .09f), .05f, Quaternion.Euler(-8f, 0f, 0f));
                PartBox(t, "PianoLogo", ivory, new Vector3(0f, s * .44f, s * .378f), new Vector3(s * .20f, s * .07f, s * .018f), .02f);
                h = s * .78f; fx = s * 1.08f; fz = s * .80f;
                break;

            case "Scissors":
                // Metallic scissors lying flat, with two ring handles, crossing blades, screw and tips.
                Material blade = ColorMat(mats, "Kitchen_ScissorBlade", new Color(.70f, .79f, .88f));
                Material handle = ColorMat(mats, "Kitchen_ScissorHandle", new Color(.13f, .54f, .92f));
                Mesh handleRing = ModelMesh("ScissorHandleRing", delegate { return BuildTorus(.34f, .10f, 28, 8); });
                AddModelPart(t, "HandleLeft", handleRing, handle, new Vector3(-s * .20f, s * .09f, -s * .20f), new Vector3(s * .37f, s * .14f, s * .37f), Quaternion.identity);
                AddModelPart(t, "HandleRight", handleRing, handle, new Vector3(s * .20f, s * .09f, -s * .20f), new Vector3(s * .37f, s * .14f, s * .37f), Quaternion.identity);
                PartBox(t, "BladeLeft", blade, new Vector3(-s * .055f, s * .075f, s * .30f), new Vector3(s * .095f, s * .045f, s * .64f), .06f, Quaternion.Euler(0f, -11f, 0f));
                PartBox(t, "BladeRight", blade, new Vector3(s * .055f, s * .078f, s * .30f), new Vector3(s * .095f, s * .045f, s * .64f), .06f, Quaternion.Euler(0f, 11f, 0f));
                PartSphere(t, "ScissorPivot", metal, new Vector3(0f, s * .12f, s * .02f), Vector3.one * s * .10f, true);
                h = s * .20f; fx = s * .88f; fz = s * 1.0f;
                break;

            case "Fridge":
                // Tall rounded refrigerator with split freezer/door, handles, water dispenser and magnets.
                Material fridgeWhite = ColorMat(mats, "Kitchen_FridgeWhite", new Color(.92f, .97f, .98f));
                Material fridgeDoor = ColorMat(mats, "Kitchen_FridgeDoor", new Color(.72f, .89f, .94f));
                Material fridgeDark = ColorMat(mats, "Kitchen_FridgeShadow", new Color(.24f, .42f, .50f));
                PartBox(t, "FridgeCabinet", fridgeWhite, new Vector3(0f, s * .78f, 0f), new Vector3(s * .74f, s * 1.52f, s * .62f), .08f);
                PartBox(t, "UpperFreezer", fridgeDoor, new Vector3(0f, s * 1.24f, s * .319f), new Vector3(s * .68f, s * .52f, s * .035f), .05f);
                PartBox(t, "LowerDoor", fridgeDoor, new Vector3(0f, s * .66f, s * .319f), new Vector3(s * .68f, s * .62f, s * .035f), .05f);
                PartBox(t, "FreezerHandle", metal, new Vector3(s * .24f, s * 1.24f, s * .35f), new Vector3(s * .045f, s * .32f, s * .06f), .2f);
                PartBox(t, "DoorHandle", metal, new Vector3(s * .24f, s * .66f, s * .35f), new Vector3(s * .045f, s * .34f, s * .06f), .2f);
                PartBox(t, "WaterDispenser", fridgeDark, new Vector3(-s * .17f, s * .74f, s * .348f), new Vector3(s * .22f, s * .22f, s * .035f), .05f);
                PartBox(t, "DispenserCup", white, new Vector3(-s * .17f, s * .67f, s * .37f), new Vector3(s * .09f, s * .06f, s * .035f), .02f);
                PartSphere(t, "FruitMagnetOrange", ColorMat(mats, "Kitchen_FridgeMagnetOrange", new Color(1f, .43f, .06f)), new Vector3(-s * .17f, s * 1.12f, s * .35f), Vector3.one * s * .085f, true);
                PartSphere(t, "FruitMagnetGreen", ColorMat(mats, "Kitchen_FridgeMagnetGreen", new Color(.15f, .69f, .25f)), new Vector3(-s * .05f, s * 1.13f, s * .35f), Vector3.one * s * .07f, true);
                h = s * 1.58f; fx = s * .80f; fz = s * .68f;
                break;

            case "TrafficCone":
                Material coneOrange = ColorMat(mats, "Street_ConeOrange", new Color(1f, .28f, .035f));
                Material coneWhite = ColorMat(mats, "Street_ConeWhite", Color.white);
                PartBox(t, "RubberBase", dark, new Vector3(0f, s * .04f, 0f), new Vector3(s * .82f, s * .08f, s * .82f), .12f);
                PartCylinder(t, "ConeBody", coneOrange, new Vector3(0f, s * .38f, 0f), new Vector3(s * .68f, s * .68f, s * .68f), .05f, .48f);
                PartCylinder(t, "ReflectiveBand", coneWhite, new Vector3(0f, s * .38f, 0f), new Vector3(s * .48f, s * .09f, s * .48f), .25f, .34f);
                h = s * .76f;
                break;

            case "StreetLamp":
                Material lampMetal = ColorMat(mats, "Street_LampMetal", new Color(.22f, .27f, .34f));
                Material lampGlass = ColorMat(mats, "Street_LampGlow", new Color(1f, .86f, .46f));
                PartCylinder(t, "LampPole", lampMetal, new Vector3(0f, s * .86f, 0f), new Vector3(s * .11f, s * 1.72f, s * .11f), .50f, .34f);
                PartBox(t, "LampArm", lampMetal, new Vector3(s * .23f, s * 1.66f, 0f), new Vector3(s * .55f, s * .09f, s * .09f), .08f);
                PartSphere(t, "LampHead", lampMetal, new Vector3(s * .46f, s * 1.58f, 0f), new Vector3(s * .25f, s * .17f, s * .22f));
                PartSphere(t, "LampBulb", lampGlass, new Vector3(s * .48f, s * 1.50f, 0f), new Vector3(s * .14f, s * .09f, s * .14f));
                h = s * 1.78f; fx = s * .9f; fz = s * .5f;
                break;

            case "Bench":
                Material seat = ColorMat(mats, "Street_BenchWood", new Color(.62f, .31f, .11f));
                Material iron = ColorMat(mats, "Street_BenchIron", new Color(.12f, .17f, .24f));
                PartBox(t, "SeatSlat1", seat, new Vector3(0f, s * .40f, 0f), new Vector3(s, s * .08f, s * .20f), .08f);
                PartBox(t, "SeatSlat2", seat, new Vector3(0f, s * .40f, s * .21f), new Vector3(s, s * .08f, s * .20f), .08f);
                PartBox(t, "BackSlat1", seat, new Vector3(0f, s * .66f, -s * .18f), new Vector3(s, s * .10f, s * .08f), .08f);
                PartBox(t, "BackSlat2", seat, new Vector3(0f, s * .82f, -s * .18f), new Vector3(s, s * .10f, s * .08f), .08f);
                for (int side = -1; side <= 1; side += 2)
                {
                    PartBox(t, "Leg_" + side, iron, new Vector3(side * s * .38f, s * .21f, 0f), new Vector3(s * .09f, s * .40f, s * .46f), .10f);
                    PartBox(t, "BackLeg_" + side, iron, new Vector3(side * s * .38f, s * .58f, -s * .18f), new Vector3(s * .09f, s * .65f, s * .08f), .08f);
                }
                h = s * .95f; fx = s * 1.10f; fz = s * .65f;
                break;

            case "Hydrant":
                Material hydrantRed = ColorMat(mats, "Street_HydrantRed", new Color(.88f, .08f, .075f));
                PartCylinder(t, "HydrantBase", hydrantRed, new Vector3(0f, s * .13f, 0f), new Vector3(s * .58f, s * .26f, s * .58f), .50f, .38f);
                PartCylinder(t, "HydrantBody", hydrantRed, new Vector3(0f, s * .48f, 0f), new Vector3(s * .42f, s * .52f, s * .42f), .36f, .42f);
                PartCylinder(t, "HydrantCap", metal, new Vector3(0f, s * .79f, 0f), new Vector3(s * .52f, s * .15f, s * .52f), .48f, .42f);
                for (int side = -1; side <= 1; side += 2)
                    PartCylinder(t, "SideOutlet_" + side, hydrantRed, new Vector3(side * s * .25f, s * .48f, 0f), new Vector3(s * .19f, s * .20f, s * .19f), .42f, .42f, Quaternion.Euler(0f, 0f, 90f));
                h = s * .88f;
                break;

            case "Pillar":
                Material bollard = ColorMat(mats, "Street_Bollard", new Color(.15f, .20f, .28f));
                Material bollardBand = ColorMat(mats, "Street_BollardBand", new Color(1f, .78f, .12f));
                PartCylinder(t, "BollardBase", bollard, new Vector3(0f, s * .06f, 0f), new Vector3(s * .78f, s * .12f, s * .78f), .50f, .50f);
                PartCylinder(t, "BollardPost", bollard, new Vector3(0f, s * .48f, 0f), new Vector3(s * .40f, s * .78f, s * .40f), .42f, .36f);
                PartCylinder(t, "BollardReflector", bollardBand, new Vector3(0f, s * .56f, 0f), new Vector3(s * .405f, s * .09f, s * .405f), .48f, .45f);
                h = s * .90f;
                break;

            default: // Wall, but with a bevelled, striped road-barrier silhouette.
                Material barrier = ColorMat(mats, "Street_Barrier", new Color(.95f, .31f, .07f));
                Material barrierStripe = ColorMat(mats, "Street_BarrierStripe", new Color(.95f, .95f, .89f));
                PartBox(t, "BarrierBody", barrier, new Vector3(0f, s * .36f, 0f), new Vector3(s, s * .72f, s * .42f), .09f);
                for (int side = -1; side <= 1; side += 2)
                    PartBox(t, "BarrierStripe_" + side, barrierStripe, new Vector3(side * s * .20f, s * .38f, s * .216f), new Vector3(s * .10f, s * .68f, s * .018f), .04f, Quaternion.Euler(0f, 0f, 18f));
                h = s * .75f; fz = s * .48f;
                break;
        }

        // Every visible part is merged into one mesh per material, preserving colour blocks while
        // limiting the renderer count for mobile builds.
        CombineModelParts(root.transform, sp.key);

        BoxCollider col = root.AddComponent<BoxCollider>();
        col.size = new Vector3(Mathf.Max(0.1f, fx), Mathf.Max(0.1f, h), Mathf.Max(0.1f, fz));
        col.center = new Vector3(0f, h * 0.5f, 0f);
        Rigidbody rb = root.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.mass = Mathf.Max(0.5f, s);
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        EdibleObject eo = root.AddComponent<EdibleObject>();
        SetRef(eo, "data", data);

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabDir + "/Edible_" + sp.key + ".prefab");
        UnityEngine.Object.DestroyImmediate(root);
        return saved;
    }

    private static void CombineModelParts(Transform root, string modelKey)
    {
        Dictionary<Material, List<CombineInstance>> batches = new Dictionary<Material, List<CombineInstance>>();
        MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
        for (int i = 0; i < filters.Length; i++)
        {
            MeshFilter mf = filters[i];
            if (mf == null || mf.sharedMesh == null || mf.transform == root) continue;
            MeshRenderer mr = mf.GetComponent<MeshRenderer>();
            if (mr == null || mr.sharedMaterial == null) continue;
            List<CombineInstance> list;
            if (!batches.TryGetValue(mr.sharedMaterial, out list))
            {
                list = new List<CombineInstance>(); batches.Add(mr.sharedMaterial, list);
            }
            CombineInstance ci = new CombineInstance();
            ci.mesh = mf.sharedMesh;
            ci.transform = root.worldToLocalMatrix * mf.transform.localToWorldMatrix;
            list.Add(ci);
        }

        List<KeyValuePair<Material, List<CombineInstance>>> ordered = new List<KeyValuePair<Material, List<CombineInstance>>>(batches);
        ordered.Sort(delegate(KeyValuePair<Material, List<CombineInstance>> a, KeyValuePair<Material, List<CombineInstance>> b)
        { return string.CompareOrdinal(a.Key.name, b.Key.name); });
        for (int i = 0; i < filters.Length; i++)
            if (filters[i] != null && filters[i].transform != root) UnityEngine.Object.DestroyImmediate(filters[i].gameObject);

        for (int i = 0; i < ordered.Count; i++)
        {
            KeyValuePair<Material, List<CombineInstance>> batch = ordered[i];
            Mesh merged = new Mesh();
            merged.name = "Combined_" + modelKey + "_" + batch.Key.name;
            merged.indexFormat = IndexFormat.UInt32;
            merged.CombineMeshes(batch.Value.ToArray(), true, true, false);
            merged.RecalculateBounds();
            string combinedKey = SafeAssetName(merged.name);
            Mesh asset = SaveAsset(merged, MeshDir + "/" + combinedKey + ".asset");
            modelMeshCache[combinedKey] = asset;
            AddModelPart(root, "ModelMesh_" + i, asset, batch.Key, Vector3.zero, Vector3.one, Quaternion.identity);
        }
    }

    /// <summary>Builds smooth, lofted body panels along the X-axis. Inputs are normalized height/half-widths.</summary>
    private static Mesh BuildProfileBody(float[] xs, float[] halfWidths, float[] bottoms, float[] tops, float centerY, int sides)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        for (int station = 0; station < xs.Length; station++)
        {
            float mid = (bottoms[station] + tops[station]) * 0.5f;
            float ry = Mathf.Max(0.001f, (tops[station] - bottoms[station]) * 0.5f);
            for (int j = 0; j < sides; j++)
            {
                float a = j * Mathf.PI * 2f / sides;
                float cy = Mathf.Sin(a);
                float cz = Mathf.Cos(a);
                float roundedZ = Mathf.Sign(cz) * Mathf.Pow(Mathf.Abs(cz), .82f);
                float roundedY = Mathf.Sign(cy) * Mathf.Pow(Mathf.Abs(cy), .82f);
                vertices.Add(new Vector3(xs[station], mid + ry * roundedY - centerY, halfWidths[station] * roundedZ));
            }
        }
        for (int station = 0; station < xs.Length - 1; station++)
        {
            int row = station * sides, nextRow = (station + 1) * sides;
            for (int j = 0; j < sides; j++)
            {
                int n = (j + 1) % sides;
                int a = row + j, b = nextRow + j, c = row + n, d = nextRow + n;
                triangles.Add(a); triangles.Add(b); triangles.Add(c);
                triangles.Add(c); triangles.Add(b); triangles.Add(d);
            }
        }
        int leftCenter = vertices.Count;
        vertices.Add(new Vector3(xs[0], (bottoms[0] + tops[0]) * .5f - centerY, 0f));
        int rightCenter = vertices.Count;
        vertices.Add(new Vector3(xs[xs.Length - 1], (bottoms[bottoms.Length - 1] + tops[tops.Length - 1]) * .5f - centerY, 0f));
        for (int j = 0; j < sides; j++)
        {
            int n = (j + 1) % sides;
            triangles.Add(leftCenter); triangles.Add(j); triangles.Add(n);
            int row = (xs.Length - 1) * sides;
            triangles.Add(rightCenter); triangles.Add(row + n); triangles.Add(row + j);
        }
        Mesh mesh = new Mesh(); mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
    }

    private static Mesh BuildToyCarBodyMesh()
    {
        float[] x = { -.50f, -.47f, -.39f, -.27f, -.10f, .12f, .30f, .42f, .48f, .50f };
        float[] w = { .20f, .27f, .31f, .32f, .32f, .32f, .31f, .29f, .25f, .20f };
        float[] b = { .15f, .115f, .105f, .105f, .105f, .105f, .105f, .11f, .13f, .16f };
        float[] t = { .22f, .30f, .34f, .38f, .40f, .39f, .35f, .30f, .25f, .22f };
        return BuildProfileBody(x, w, b, t, .25f, 18);
    }

    private static Mesh BuildToyCarCabinMesh()
    {
        float[] x = { -.31f, -.27f, -.21f, -.14f, .02f, .14f, .22f, .255f };
        float[] w = { .115f, .15f, .18f, .195f, .195f, .175f, .13f, .075f };
        float[] b = { .38f, .38f, .39f, .40f, .40f, .385f, .36f, .34f };
        float[] t = { .40f, .48f, .60f, .675f, .69f, .625f, .48f, .385f };
        return BuildProfileBody(x, w, b, t, .53f, 18);
    }

    private static Mesh BuildBusBodyMesh()
    {
        float[] x = { -.50f, -.49f, -.46f, -.40f, .36f, .44f, .49f, .50f };
        float[] w = { .22f, .29f, .315f, .32f, .32f, .30f, .26f, .20f };
        float[] b = { .18f, .12f, .09f, .085f, .085f, .09f, .12f, .16f };
        float[] t = { .42f, .48f, .52f, .54f, .54f, .52f, .48f, .43f };
        return BuildProfileBody(x, w, b, t, .32f, 18);
    }

    /// <summary>Organic banana tube with an arcing centreline and tapered ends.</summary>
    private static Mesh BuildBananaMesh()
    {
        const int rings = 25, sides = 12;
        List<Vector3> verts = new List<Vector3>(); List<int> tris = new List<int>();
        for (int i = 0; i <= rings; i++)
        {
            float u = (float)i / rings;
            float x = Mathf.Lerp(-.47f, .47f, u);
            float y = .19f + .43f * 4f * u * (1f - u);
            float dydx = (1.72f * (1f - 2f * u)) / .94f;
            Vector3 tangent = new Vector3(1f, dydx, 0f).normalized;
            Vector3 normal = new Vector3(-tangent.y, tangent.x, 0f).normalized;
            float taper = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Mathf.PI * u)), .55f);   // sin(pi)가 -8.7e-8 이라 음수^0.55 = NaN 이 되던 문제 방지
            float radius = .018f + .095f * taper;
            for (int j = 0; j < sides; j++)
            {
                float a = j * Mathf.PI * 2f / sides;
                Vector3 offset = normal * (Mathf.Cos(a) * radius) + Vector3.forward * (Mathf.Sin(a) * radius * .80f);
                verts.Add(new Vector3(x, y, 0f) + offset);
            }
        }
        for (int i = 0; i < rings; i++)
            for (int j = 0; j < sides; j++)
            {
                int a = i * sides + j, b = i * sides + (j + 1) % sides;
                int c = (i + 1) * sides + j, d = (i + 1) * sides + (j + 1) % sides;
                tris.Add(a); tris.Add(c); tris.Add(b); tris.Add(b); tris.Add(c); tris.Add(d);
            }
        Mesh m = new Mesh(); m.SetVertices(verts); m.SetTriangles(tris, 0); m.RecalculateNormals(); m.RecalculateBounds(); return m;
    }

    private static Mesh BuildLatheProfile(Vector2[] profile, int sides = 32)
    {
        List<Vector3> v = new List<Vector3>(); List<int> tri = new List<int>();
        for (int ring = 0; ring < profile.Length; ring++)
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2f / sides;
                v.Add(new Vector3(Mathf.Cos(a) * profile[ring].y, profile[ring].x, Mathf.Sin(a) * profile[ring].y));
            }
        for (int ring = 0; ring < profile.Length - 1; ring++)
            for (int i = 0; i < sides; i++)
            {
                int n = (i + 1) % sides;
                int a = ring * sides + i, b = ring * sides + n, c = (ring + 1) * sides + i, d = (ring + 1) * sides + n;
                tri.Add(a); tri.Add(c); tri.Add(b); tri.Add(b); tri.Add(c); tri.Add(d);
            }
        int lower = v.Count; v.Add(new Vector3(0f, profile[0].x, 0f));
        int upper = v.Count; v.Add(new Vector3(0f, profile[profile.Length - 1].x, 0f));
        for (int i = 0; i < sides; i++)
        {
            int n = (i + 1) % sides;
            tri.Add(lower); tri.Add(i); tri.Add(n);
            int st = (profile.Length - 1) * sides; tri.Add(upper); tri.Add(st + n); tri.Add(st + i);
        }
        Mesh m = new Mesh(); m.SetVertices(v); m.SetTriangles(tri, 0); m.RecalculateNormals(); m.RecalculateBounds(); return m;
    }

    private static Mesh BuildBurgerLowerBun()
    {
        return BuildLatheProfile(new Vector2[] { new Vector2(.015f,.26f), new Vector2(.025f,.37f), new Vector2(.05f,.43f), new Vector2(.11f,.45f), new Vector2(.15f,.40f), new Vector2(.16f,.32f) });
    }

    private static Mesh BuildBurgerUpperBun()
    {
        return BuildLatheProfile(new Vector2[] { new Vector2(.39f,.35f), new Vector2(.405f,.42f), new Vector2(.45f,.435f), new Vector2(.51f,.39f), new Vector2(.565f,.30f), new Vector2(.60f,.19f), new Vector2(.615f,.07f), new Vector2(.616f,.01f) });
    }

    /// <summary>Low-poly triangular pizza wedge with a shallow cheese surface and solid sides.</summary>
    private static Mesh BuildPizzaSliceMesh()
    {
        Vector3[] v = {
            new Vector3(-.46f,.055f,-.38f), new Vector3(-.46f,.055f,.38f), new Vector3(.49f,.055f,0f),
            new Vector3(-.46f,.16f,-.38f), new Vector3(-.46f,.16f,.38f), new Vector3(.49f,.16f,0f)
        };
        int[] tri = {
            0,2,1, 3,4,5,
            0,1,4, 0,4,3,
            1,2,5, 1,5,4,
            2,0,3, 2,3,5
        };
        Mesh m = new Mesh(); m.vertices = v; m.triangles = tri; m.RecalculateNormals(); m.RecalculateBounds(); return m;
    }

    /// <summary>Open-top, outward-flared paper carton, authored as a tapered polygon mesh.</summary>
    private static Mesh BuildFriesCartonMesh()
    {
        Vector3[] v = {
            new Vector3(-.25f,.02f,-.20f), new Vector3(.25f,.02f,-.20f), new Vector3(.25f,.02f,.20f), new Vector3(-.25f,.02f,.20f),
            new Vector3(-.39f,.43f,-.29f), new Vector3(.39f,.43f,-.29f), new Vector3(.39f,.43f,.29f), new Vector3(-.39f,.43f,.29f),
            new Vector3(0f,.02f,0f)
        };
        int[] tri = {
            // Bottom fan faces downward; the four tapered side panels face outward.
            8,0,1, 8,1,2, 8,2,3, 8,3,0,
            0,4,5, 0,5,1,
            1,5,6, 1,6,2,
            2,6,7, 2,7,3,
            3,7,4, 3,4,0
        };
        Mesh m = new Mesh(); m.vertices = v; m.triangles = tri; m.RecalculateNormals(); m.RecalculateBounds(); return m;
    }

    /// <summary>Curved pointed leaf surface made from a tapered mesh, not a scaled sphere.</summary>
    private static Mesh BuildLeafMesh()
    {
        const int segments = 7;
        List<Vector3> v = new List<Vector3>(); List<int> tri = new List<int>();
        for (int i = 0; i <= segments; i++)
        {
            float u = i / (float)segments;
            float y = u;
            float width = .22f * Mathf.Sin(Mathf.PI * u);
            float camber = .035f * Mathf.Sin(Mathf.PI * u);
            v.Add(new Vector3(-width, y, 0f));
            v.Add(new Vector3(0f, y, camber));
            v.Add(new Vector3(width, y, 0f));
        }
        for (int i = 0; i < segments; i++)
        {
            int a = i * 3, b = a + 1, c = a + 2;
            int d = a + 3, e = a + 4, f = a + 5;
            tri.Add(a); tri.Add(d); tri.Add(b); tri.Add(b); tri.Add(d); tri.Add(e);
            tri.Add(b); tri.Add(e); tri.Add(c); tri.Add(c); tri.Add(e); tri.Add(f);
        }
        // Duplicate the thin surface for a back face with independent normals, so leaves remain
        // visible from every camera angle while keeping the mesh truly custom and low-poly.
        int frontVertexCount = v.Count;
        for (int i = 0; i < frontVertexCount; i++)
        {
            Vector3 q = v[i];
            if (i % 3 == 1) q.z = -q.z;
            v.Add(q);
        }
        int frontTriangleCount = tri.Count;
        for (int i = 0; i < frontTriangleCount; i += 3)
        {
            tri.Add(tri[i + 2] + frontVertexCount);
            tri.Add(tri[i + 1] + frontVertexCount);
            tri.Add(tri[i] + frontVertexCount);
        }
        Mesh m = new Mesh(); m.SetVertices(v); m.SetTriangles(tri, 0); m.RecalculateNormals(); m.RecalculateBounds(); return m;
    }

    /// <summary>Rounded frozen-pop silhouette with a narrow stick end and domed top.</summary>
    private static Mesh BuildPopsicle()
    {
        float[] ys = { 0.015f, 0.055f, 0.12f, 0.24f, 0.52f, 0.76f, 0.88f, 0.955f, 0.99f };
        float[] rs = { 0.16f, 0.31f, 0.42f, 0.47f, 0.48f, 0.45f, 0.38f, 0.23f, 0.04f };
        const int sides = 24;
        List<Vector3> v = new List<Vector3>(); List<int> tri = new List<int>();
        for (int ring = 0; ring < ys.Length; ring++)
        {
            for (int i = 0; i < sides; i++)
            {
                float a = 2f * Mathf.PI * i / sides;
                float c = Mathf.Cos(a), sn = Mathf.Sin(a);
                // A superellipse gives the ice pop a soft rectangular profile rather than a sphere.
                float ex = Mathf.Sign(c) * Mathf.Pow(Mathf.Abs(c), 0.72f);
                float ez = Mathf.Sign(sn) * Mathf.Pow(Mathf.Abs(sn), 0.72f);
                v.Add(new Vector3(ex * rs[ring], ys[ring] - 0.5f, ez * rs[ring] * 0.64f));
            }
        }
        for (int ring = 0; ring < ys.Length - 1; ring++)
            for (int i = 0; i < sides; i++)
            {
                int a = ring * sides + i, b = ring * sides + (i + 1) % sides;
                int c = (ring + 1) * sides + i, d = (ring + 1) * sides + (i + 1) % sides;
                tri.Add(a); tri.Add(c); tri.Add(b); tri.Add(b); tri.Add(c); tri.Add(d);
            }
        int bottom = v.Count; v.Add(new Vector3(0f, ys[0] - 0.5f, 0f));
        int top = v.Count; v.Add(new Vector3(0f, ys[ys.Length - 1] - 0.5f, 0f));
        for (int i = 0; i < sides; i++)
        {
            tri.Add(bottom); tri.Add((i + 1) % sides); tri.Add(i);
            int start = (ys.Length - 1) * sides;
            tri.Add(top); tri.Add(start + i); tri.Add(start + (i + 1) % sides);
        }
        Mesh m = new Mesh(); m.SetVertices(v); m.SetTriangles(tri, 0); m.RecalculateNormals(); m.RecalculateBounds(); return m;
    }

    /// <summary>Solid triangular-prism cheese wedge with a sloped upper face.</summary>
    private static Mesh BuildCheeseWedge()
    {
        Vector3[] v = {
            new Vector3(-.5f,-.5f,-.5f), new Vector3(.5f,-.5f,-.5f),
            new Vector3(-.5f,-.5f,.5f), new Vector3(.5f,-.5f,.5f),
            new Vector3(-.5f,.5f,-.5f), new Vector3(-.5f,.5f,.5f)
        };
        int[] tri = {
            0,2,1, 1,2,3,       // bottom
            0,1,4,              // back triangular face
            2,5,3,              // front triangular face
            0,4,5, 0,5,2,        // tall left face
            1,3,5, 1,5,4         // sloped top
        };
        Mesh m = new Mesh(); m.vertices = v; m.triangles = tri; m.RecalculateNormals(); m.RecalculateBounds(); return m;
    }

    private static Mesh BuildRoundedBox(float bevel, int segments)
    {
        bevel = Mathf.Clamp(bevel, 0.01f, 0.22f);
        List<Vector3> v = new List<Vector3>(); List<int> tri = new List<int>();
        Vector3[] normals = { Vector3.forward, Vector3.back, Vector3.right, Vector3.left, Vector3.up, Vector3.down };
        Vector3[] us = { Vector3.right, Vector3.left, Vector3.back, Vector3.forward, Vector3.right, Vector3.right };
        Vector3[] vs = { Vector3.up, Vector3.up, Vector3.up, Vector3.up, Vector3.back, Vector3.forward };
        int seg = Mathf.Max(1, segments);
        for (int face = 0; face < 6; face++)
        {
            int start = v.Count;
            for (int y = 0; y <= seg; y++)
                for (int x = 0; x <= seg; x++)
                {
                    float a = Mathf.Lerp(-0.5f, 0.5f, (float)x / seg);
                    float b = Mathf.Lerp(-0.5f, 0.5f, (float)y / seg);
                    Vector3 p = normals[face] * 0.5f + us[face] * a + vs[face] * b;
                    Vector3 core = new Vector3(Mathf.Clamp(p.x, -0.5f + bevel, 0.5f - bevel), Mathf.Clamp(p.y, -0.5f + bevel, 0.5f - bevel), Mathf.Clamp(p.z, -0.5f + bevel, 0.5f - bevel));
                    Vector3 d = p - core;
                    if (d.sqrMagnitude > 0.000001f) p = core + d.normalized * bevel;
                    v.Add(p);
                }
            for (int y = 0; y < seg; y++)
                for (int x = 0; x < seg; x++)
                {
                    int a = start + y * (seg + 1) + x;
                    int b = a + 1, c = a + seg + 1, d = c + 1;
                    // Face orientation corrected against its requested normal.
                    Vector3 cr = Vector3.Cross(v[b] - v[a], v[c] - v[a]);
                    if (Vector3.Dot(cr, normals[face]) >= 0f) { tri.Add(a); tri.Add(b); tri.Add(c); tri.Add(b); tri.Add(d); tri.Add(c); }
                    else { tri.Add(a); tri.Add(c); tri.Add(b); tri.Add(b); tri.Add(c); tri.Add(d); }
                }
        }
        Mesh mesh = new Mesh(); mesh.SetVertices(v); mesh.SetTriangles(tri, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
    }

    private static Mesh BuildUvSphere(int segments, int rings)
    {
        List<Vector3> v = new List<Vector3>(); List<int> tri = new List<int>();
        for (int y = 0; y <= rings; y++)
        {
            float lat = Mathf.PI * (-0.5f + (float)y / rings);
            float cy = Mathf.Sin(lat); float r = Mathf.Cos(lat);
            for (int x = 0; x <= segments; x++)
            {
                float lon = 2f * Mathf.PI * x / segments;
                v.Add(new Vector3(r * Mathf.Cos(lon) * .5f, cy * .5f, r * Mathf.Sin(lon) * .5f));
            }
        }
        for (int y = 0; y < rings; y++)
            for (int x = 0; x < segments; x++)
            {
                int a = y * (segments + 1) + x; int b = a + segments + 1;
                tri.Add(a); tri.Add(b); tri.Add(a + 1); tri.Add(a + 1); tri.Add(b); tri.Add(b + 1);
            }
        Mesh m = new Mesh(); m.SetVertices(v); m.SetTriangles(tri, 0); m.RecalculateNormals(); m.RecalculateBounds(); return m;
    }

    private static Mesh BuildIcoSphere(int subdivisions)
    {
        float t = (1f + Mathf.Sqrt(5f)) / 2f;
        List<Vector3> verts = new List<Vector3> {
            new Vector3(-1,t,0),new Vector3(1,t,0),new Vector3(-1,-t,0),new Vector3(1,-t,0),
            new Vector3(0,-1,t),new Vector3(0,1,t),new Vector3(0,-1,-t),new Vector3(0,1,-t),
            new Vector3(t,0,-1),new Vector3(t,0,1),new Vector3(-t,0,-1),new Vector3(-t,0,1)
        };
        for (int i = 0; i < verts.Count; i++) verts[i] = verts[i].normalized * .5f;
        List<int> faces = new List<int> {
            0,11,5,0,5,1,0,1,7,0,7,10,0,10,11,1,5,9,5,11,4,11,10,2,10,7,6,7,1,8,
            3,9,4,3,4,2,3,2,6,3,6,8,3,8,9,4,9,5,2,4,11,6,2,10,8,6,7,9,8,1
        };
        for (int sub = 0; sub < subdivisions; sub++)
        {
            Dictionary<long, int> cache = new Dictionary<long, int>(); List<int> next = new List<int>();
            Func<int,int,int> mid = delegate(int a, int b)
            {
                int mn = Mathf.Min(a, b), mx = Mathf.Max(a, b); long key = ((long)mn << 32) | (uint)mx; int idx;
                if (cache.TryGetValue(key, out idx)) return idx;
                Vector3 p = ((verts[a] + verts[b]) * .5f).normalized * .5f; idx = verts.Count; verts.Add(p); cache[key] = idx; return idx;
            };
            for (int i = 0; i < faces.Count; i += 3)
            {
                int a = faces[i], b = faces[i + 1], c = faces[i + 2]; int ab = mid(a,b), bc = mid(b,c), ca = mid(c,a);
                next.AddRange(new int[] { a,ab,ca, b,bc,ab, c,ca,bc, ab,bc,ca });
            }
            faces = next;
        }
        Mesh m = new Mesh(); m.SetVertices(verts); m.SetTriangles(faces, 0); m.RecalculateNormals(); m.RecalculateBounds(); return m;
    }

    private static Mesh BuildCylinder(float bottomRadius, float topRadius, int sides)
    {
        List<Vector3> v = new List<Vector3>(); List<int> tri = new List<int>();
        for (int i = 0; i <= sides; i++)
        {
            float a = i * Mathf.PI * 2f / sides; float x = Mathf.Cos(a), z = Mathf.Sin(a);
            v.Add(new Vector3(x * bottomRadius, -0.5f, z * bottomRadius));
            v.Add(new Vector3(x * topRadius, 0.5f, z * topRadius));
        }
        for (int i = 0; i < sides; i++)
        {
            int a = i * 2;
            tri.Add(a); tri.Add(a + 1); tri.Add(a + 2);
            tri.Add(a + 2); tri.Add(a + 1); tri.Add(a + 3);
        }
        int bottomCenter = v.Count; v.Add(new Vector3(0f, -.5f, 0f)); int topCenter = v.Count; v.Add(new Vector3(0f, .5f, 0f));
        for (int i = 0; i < sides; i++)
        {
            int a = i * 2, b = (i + 1) * 2;
            tri.Add(bottomCenter); tri.Add(a); tri.Add(b);
            tri.Add(topCenter); tri.Add(b + 1); tri.Add(a + 1);
        }
        Mesh m = new Mesh(); m.SetVertices(v); m.SetTriangles(tri, 0); m.RecalculateNormals(); m.RecalculateBounds(); return m;
    }

    private static Mesh BuildTorus(float majorRadius, float tubeRadius, int majorSegments, int tubeSegments)
    {
        List<Vector3> v = new List<Vector3>(); List<int> tri = new List<int>();
        for (int i = 0; i <= majorSegments; i++)
        {
            float u = i * Mathf.PI * 2f / majorSegments;
            for (int j = 0; j <= tubeSegments; j++)
            {
                float a = j * Mathf.PI * 2f / tubeSegments;
                float r = majorRadius + tubeRadius * Mathf.Cos(a);
                v.Add(new Vector3(r * Mathf.Cos(u), tubeRadius * Mathf.Sin(a), r * Mathf.Sin(u)));
            }
        }
        int stride = tubeSegments + 1;
        for (int i = 0; i < majorSegments; i++)
            for (int j = 0; j < tubeSegments; j++)
            {
                int a = i * stride + j, b = a + stride;
                // Winding produces outside-facing normals after recalculation.
                tri.Add(a); tri.Add(a + 1); tri.Add(b); tri.Add(a + 1); tri.Add(b + 1); tri.Add(b);
            }
        Mesh m = new Mesh(); m.SetVertices(v); m.SetTriangles(tri, 0); m.RecalculateNormals(); m.RecalculateBounds(); return m;
    }

    private static Mesh BuildGableRoof()
    {
        Vector3[] v = {
            new Vector3(-.5f,-.5f,.5f), new Vector3(.5f,-.5f,.5f), new Vector3(0f,.5f,.5f),
            new Vector3(-.5f,-.5f,-.5f), new Vector3(.5f,-.5f,-.5f), new Vector3(0f,.5f,-.5f)
        };
        int[] tri = {
            0,1,2, 3,5,4, // gable ends
            0,5,3, 0,2,5, // left sloped roof
            1,5,2, 1,4,5, // right sloped roof
            0,4,1, 0,3,4  // bottom cap
        };
        Mesh m = new Mesh(); m.vertices = v; m.triangles = tri; m.RecalculateNormals(); m.RecalculateBounds(); return m;
    }
}
#endif
