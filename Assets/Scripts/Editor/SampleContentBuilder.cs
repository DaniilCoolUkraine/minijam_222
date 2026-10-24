using System.Collections.Generic;
using System.IO;
using ShapeInventory.Core;
using ShapeInventory.Game;
using ShapeInventory.UI.Common;
using UnityEditor;
using UnityEngine;

namespace ShapeInventory.Editor
{
    /// <summary>
    /// Idempotent content generator. Existing assets are loaded and modified through SerializedObject so their GUIDs
    /// (and therefore every scene/asset reference) survive; missing assets are created. Object sprites are only
    /// generated when the PNG is missing, so real art is never overwritten.
    /// </summary>
    internal static class SampleContentBuilder
    {
        internal const string ContentRoot = "Assets/Content";
        internal const string ItemsDir = "Assets/Content/Items";
        internal const string ObjectsDir = "Assets/Content/Objects";
        internal const string LevelsDir = "Assets/Content/Levels";
        internal const string SpritesDir = "Assets/Content/Sprites";

        private const int SpriteCellPixels = 16;

        internal static void Run()
        {
            ContentUtil.EnsureFolder(ContentRoot);
            ContentUtil.EnsureFolder(ItemsDir);
            ContentUtil.EnsureFolder(ObjectsDir);
            ContentUtil.EnsureFolder(LevelsDir);
            ContentUtil.EnsureFolder(SpritesDir);

            // ---- items (plain first, then enchanted copies of the same shapes)
            var items = new Dictionary<string, ItemData>();
            var plainDefs = new Dictionary<string, ItemDef>();
            int created = 0;

            foreach (ItemDef def in SampleContentData.PlainItems)
            {
                plainDefs[def.Id] = def;
                bool isNew;
                items[def.Id] = UpsertItem("Item_" + def.Id, def.Id, def.Name, ContentUtil.ParseColor(def.Hex),
                    EnchantmentType.None, def.Rows, out isNew);
                if (isNew) created++;
            }

            foreach (EnchantedItemDef def in SampleContentData.EnchantedItems)
            {
                ItemDef baseDef;
                if (!plainDefs.TryGetValue(def.BaseId, out baseDef))
                {
                    Debug.LogError("ShapeInventory: unknown base shape '" + def.BaseId + "' for enchanted item " + def.Id);
                    continue;
                }

                bool isNew;
                items[def.Id] = UpsertItem("Item_" + def.Id, def.Id,
                    EnchantmentStyle.DisplayName(def.Type) + " " + baseDef.Name,
                    EnchantmentStyle.GetColor(def.Type), def.Type, baseDef.Rows, out isNew);
                if (isNew) created++;
            }

            // ---- objects + placeholder sprites
            var objects = new Dictionary<string, ObjectData>();
            var sprites = new Dictionary<string, Sprite>();
            foreach (ObjectDef def in SampleContentData.Objects)
            {
                Color color = ContentUtil.ParseColor(def.Hex);
                string path = ObjectsDir + "/Object_" + def.Id + ".asset";
                ObjectData asset = AssetDatabase.LoadAssetAtPath<ObjectData>(path);
                if (asset == null)
                {
                    asset = ObjectData.Create(def.Id, def.Name, color, def.Rows);
                    AssetDatabase.CreateAsset(asset, path);
                    created++;
                }

                var so = new SerializedObject(asset);
                so.FindProperty("id").stringValue = def.Id;
                so.FindProperty("displayName").stringValue = def.Name;
                so.FindProperty("silhouetteColor").colorValue = color;
                ContentUtil.SetStringArray(so.FindProperty("silhouetteRows"), def.Rows);
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
                objects[def.Id] = asset;
                sprites[def.Id] = EnsureObjectSprite(def, asset);
            }

            // ---- levels
            foreach (LevelDef def in SampleContentData.Levels)
            {
                ObjectData target;
                if (!objects.TryGetValue(def.ObjectId, out target))
                {
                    Debug.LogError("ShapeInventory: unknown object '" + def.ObjectId + "' in " + def.AssetName);
                    continue;
                }

                string path = LevelsDir + "/" + def.AssetName + ".asset";
                LevelData asset = AssetDatabase.LoadAssetAtPath<LevelData>(path);
                if (asset == null)
                {
                    asset = LevelData.Create(def.AssetName, null, null);
                    AssetDatabase.CreateAsset(asset, path);
                    created++;
                }

                var so = new SerializedObject(asset);
                so.FindProperty("targetObject").objectReferenceValue = target;

                SerializedProperty arr = so.FindProperty("startingItems");
                arr.arraySize = def.ItemIds.Length;
                for (int i = 0; i < def.ItemIds.Length; i++)
                {
                    ItemData item;
                    if (!items.TryGetValue(def.ItemIds[i], out item))
                        Debug.LogError("ShapeInventory: unknown item '" + def.ItemIds[i] + "' in " + def.AssetName);
                    arr.GetArrayElementAtIndex(i).objectReferenceValue = item;
                }

                // Only fill the sprite when nothing is assigned yet: a hand-assigned picture always wins.
                SerializedProperty spriteProp = so.FindProperty("objectSprite");
                Sprite sprite;
                if (spriteProp.objectReferenceValue == null && sprites.TryGetValue(def.ObjectId, out sprite) && sprite != null)
                    spriteProp.objectReferenceValue = sprite;

                so.FindProperty("questDescription").stringValue = def.Story;

                SerializedProperty reqs = so.FindProperty("requirements");
                reqs.arraySize = def.Requirements.Length;
                for (int i = 0; i < def.Requirements.Length; i++)
                {
                    SerializedProperty element = reqs.GetArrayElementAtIndex(i);
                    element.FindPropertyRelative("type").intValue = (int)def.Requirements[i].Type;
                    element.FindPropertyRelative("tier").intValue = def.Requirements[i].Tier;
                }

                so.FindProperty("successText").stringValue = def.SuccessText;
                so.FindProperty("partialText").stringValue = def.PartialText;
                so.FindProperty("failureText").stringValue = def.FailureText;

                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("ShapeInventory: sample content up to date under " + ContentRoot + " ("
                      + (SampleContentData.PlainItems.Length + SampleContentData.EnchantedItems.Length) + " items, "
                      + SampleContentData.Objects.Length + " objects, " + SampleContentData.Levels.Length + " levels; "
                      + created + " newly created, the rest updated in place).");
        }

        // ------------------------------------------------------------------ items

        private static ItemData UpsertItem(string assetName, string id, string displayName, Color color,
            EnchantmentType enchantment, string[] rows, out bool isNew)
        {
            string path = ItemsDir + "/" + assetName + ".asset";
            ItemData asset = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            isNew = asset == null;
            if (isNew)
            {
                asset = ItemData.Create(id, displayName, color, enchantment, rows);
                AssetDatabase.CreateAsset(asset, path);
            }

            var so = new SerializedObject(asset);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("color").colorValue = color;
            so.FindProperty("enchantment").intValue = (int)enchantment;
            ContentUtil.SetStringArray(so.FindProperty("shapeRows"), rows);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        // ------------------------------------------------------------------ placeholder sprites

        /// <summary>
        /// Returns the sprite at Assets/Content/Sprites/Object_&lt;id&gt;.png. The PNG (16x16 px per silhouette cell) is generated
        /// only if the file does not exist yet; an existing file is never touched.
        /// </summary>
        private static Sprite EnsureObjectSprite(ObjectDef def, ObjectData data)
        {
            string assetPath = SpritesDir + "/Object_" + def.Id + ".png";
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string diskPath = Path.Combine(projectRoot, assetPath);

            if (!File.Exists(diskPath))
            {
                byte[] png = RenderPlaceholderPng(data, ContentUtil.ParseColor(def.Hex), def.Id);
                File.WriteAllBytes(diskPath, png);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

                var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.spritePixelsPerUnit = SpriteCellPixels;
                    importer.filterMode = FilterMode.Point;
                    importer.alphaIsTransparency = true;
                    importer.mipmapEnabled = false;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.SaveAndReimport();
                }
            }
            else if (AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath) == null)
            {
                // File exists on disk but Unity has not imported it yet (e.g. dropped in from outside): import with its own settings.
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            }

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null)
                Debug.LogWarning("ShapeInventory: " + assetPath + " is not imported as a Sprite (Single); set its Texture Type to Sprite to use it.");
            return sprite;
        }

        private static byte[] RenderPlaceholderPng(ObjectData data, Color baseColor, string objectId)
        {
            ItemShape sil = data.Silhouette;
            int w = sil.Width * SpriteCellPixels;
            int h = sil.Height * SpriteCellPixels;
            var pixels = new Color32[w * h];

            float baseHue, baseSat, baseVal;
            Color.RGBToHSV(baseColor, out baseHue, out baseSat, out baseVal);
            // A different stripe style per object so the placeholders do not look identical.
            int style = 0;
            for (int i = 0; i < objectId.Length; i++) style += objectId[i];
            style %= 3;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // y is measured from the TOP of the picture; Texture2D rows run bottom-up.
                    int index = (h - 1 - y) * w + x;
                    if (!InsideSilhouette(sil, x, y, w, h))
                    {
                        pixels[index] = new Color32(0, 0, 0, 0);
                        continue;
                    }

                    float u = w > 1 ? x / (float)(w - 1) : 0f;
                    float v = h > 1 ? y / (float)(h - 1) : 0f;

                    float hue = Mathf.Repeat(baseHue + 0.18f * v + 0.08f * u, 1f);
                    float sat = Mathf.Clamp01(Mathf.Max(baseSat, 0.45f) + 0.15f * u);
                    float val = Mathf.Lerp(1f, 0.72f, v);

                    bool stripe;
                    if (style == 0) stripe = ((x + y) / 6) % 2 == 0;
                    else if (style == 1) stripe = (x / 8) % 2 == 0;
                    else stripe = ((x / 8) + (y / 8)) % 2 == 0;
                    if (stripe) val *= 0.9f;

                    bool edge = !InsideSilhouette(sil, x - 2, y, w, h) || !InsideSilhouette(sil, x + 2, y, w, h)
                                || !InsideSilhouette(sil, x, y - 2, w, h) || !InsideSilhouette(sil, x, y + 2, w, h);
                    if (edge) val *= 0.55f;

                    Color c = Color.HSVToRGB(hue, sat, Mathf.Clamp01(val));
                    pixels[index] = new Color32(
                        (byte)Mathf.RoundToInt(c.r * 255f),
                        (byte)Mathf.RoundToInt(c.g * 255f),
                        (byte)Mathf.RoundToInt(c.b * 255f),
                        255);
                }
            }

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels32(pixels);
            tex.Apply();
            byte[] png = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);
            return png;
        }

        private static bool InsideSilhouette(ItemShape sil, int px, int py, int w, int h)
        {
            if (px < 0 || py < 0 || px >= w || py >= h) return false;
            return sil.Contains(new Vector2Int(px / SpriteCellPixels, py / SpriteCellPixels));
        }
    }
}
