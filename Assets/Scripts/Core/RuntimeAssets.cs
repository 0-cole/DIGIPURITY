using System.IO;
using UnityEngine;

namespace DigiPurity.Core
{
    /// <summary>
    /// Universal cross-platform runtime asset loader for DIGIPURITY.
    /// Resolves Assets/... or Resources/... paths via Resources.Load in both standalone builds
    /// and the Unity Editor, falling back to AssetDatabase in Editor when needed.
    /// </summary>
    public static class RuntimeAssets
    {
        public static string ToResourcePath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return string.Empty;

            string p = assetPath.Replace('\\', '/').TrimStart('/');
            if (p.StartsWith("Assets/Resources/"))
                p = p.Substring("Assets/Resources/".Length);
            else if (p.StartsWith("Assets/"))
                p = p.Substring("Assets/".Length);
            else if (p.StartsWith("Resources/"))
                p = p.Substring("Resources/".Length);

            // Strip file extension for Resources.Load
            int dot = p.LastIndexOf('.');
            int slash = p.LastIndexOf('/');
            if (dot > slash && dot >= 0)
            {
                p = p.Substring(0, dot);
            }
            return p;
        }

        public static T Load<T>(string assetPath) where T : Object
        {
            if (string.IsNullOrEmpty(assetPath)) return null;

            string resPath = ToResourcePath(assetPath);
            T loaded = Resources.Load<T>(resPath);
            if (loaded != null) return loaded;

#if UNITY_EDITOR
            string normalized = assetPath.Replace('\\', '/');
            T editorAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(normalized);
            if (editorAsset != null) return editorAsset;

            if (normalized.StartsWith("Assets/") && !normalized.StartsWith("Assets/Resources/"))
            {
                string inRes = "Assets/Resources/" + normalized.Substring("Assets/".Length);
                editorAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(inRes);
                if (editorAsset != null) return editorAsset;
            }
#endif
            return null;
        }

        public static Sprite[] LoadAllSprites(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return null;
            string resPath = ToResourcePath(assetPath);
            return Resources.LoadAll<Sprite>(resPath);
        }
    }
}
