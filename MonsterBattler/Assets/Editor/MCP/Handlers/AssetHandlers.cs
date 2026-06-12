using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace MonsterBattler.Editor.MCP.Handlers
{
    /// <summary>
    /// Asset-pipeline commands so generated art can become PROPER imported assets (per the project
    /// rule: chrome lives in scene/prefabs/assets, not runtime loaders).
    /// </summary>
    [InitializeOnLoad]
    public static class AssetHandlers
    {
        static AssetHandlers()
        {
            // Configure a texture as a UI Sprite, optionally with a 9-slice border, and reimport.
            // params: path (Assets/...), border [L,B,R,T] (optional), ppu (optional, default 100)
            MCPCommandRegistry.Register("asset.import_sprite", p =>
            {
                var path = (string)p["path"];
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) throw new System.InvalidOperationException($"No texture at {path}");
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                if (p["ppu"] != null) importer.spritePixelsPerUnit = (float)p["ppu"];
                if (p["border"] is JArray b && b.Count == 4)
                    importer.spriteBorder = new Vector4((float)b[0], (float)b[1], (float)b[2], (float)b[3]);
                importer.SaveAndReimport();
                return new JObject { ["path"] = path, ["border"] = p["border"] };
            });

            // Copy a file from anywhere on disk into the project and import it.
            // params: from (absolute), to (Assets/...)
            MCPCommandRegistry.Register("asset.copy_in", p =>
            {
                var from = (string)p["from"];
                var to = (string)p["to"];
                var full = Path.GetFullPath(Path.Combine(Application.dataPath, "..", to));
                Directory.CreateDirectory(Path.GetDirectoryName(full));
                File.Copy(from, full, overwrite: true);
                AssetDatabase.ImportAsset(to);
                return new JObject { ["to"] = to };
            });
        }
    }
}
