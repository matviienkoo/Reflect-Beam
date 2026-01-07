#if UNITY_EDITOR
using UObject = UnityEngine.Object;
using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Reflection;
using Debug = UnityEngine.Debug;

namespace ProjectMaintenance
{
    public sealed class ProjectMaintenanceTool : EditorWindow
    {
        // ──────────────── self path ────────────────
        private static string ToolRelPath;
        private static string ToolAbsPath;
        private static bool   _pathsReady;

        private static void EnsurePaths()
        {
            if (_pathsReady) return;

            string[] guids = AssetDatabase.FindAssets($"{nameof(ProjectMaintenanceTool)} t:Script");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid).Replace('\\', '/');
                if (Path.GetFileNameWithoutExtension(path) != nameof(ProjectMaintenanceTool))
                    continue;

                ToolRelPath = path;
                ToolAbsPath = Path.Combine(Application.dataPath.Replace('\\', '/'),
                                           path.Substring("Assets/".Length));
                _pathsReady = true;
                break;
            }

            if (!_pathsReady)
                Debug.LogError("[ProjectMaintenance] Не найден собственный .cs файл!");
        }

        // ───────────── exclusions ─────────────
        private readonly string[] excludePaths =
        {
            "ProjectMaintenanceTool",
            "Editor/ProjectMaintenanceTool"
        };

        // ───────────── build content state ─────────────
        private bool   _buildStatsDirty = true;          // авто-скан при открытии окна
        private string _buildStatsInfo = "Скан не выполнялся.";
        private long   _buildTotalBytes;
        private readonly Dictionary<string,long> _buildByCategory = new();
        private readonly List<BuildAssetRow>     _buildRows       = new();
        private Vector2 _buildScroll;
        private string  _buildFilter = "";

        private sealed class BuildAssetRow
        {
            public string AssetPath;
            public string Category;
            public long   Bytes;
            public string PrettySize;
            public string Ext;
        }

        // ───────────── MENU ─────────────
        [MenuItem("Tools/Project Maintenance/Open Window", priority = 1)]
        private static void OpenWindow()
            => GetWindow<ProjectMaintenanceTool>("Project Maintenance");

        [MenuItem("Tools/Project Maintenance/Disable All Scripts", priority = 10)]
        private static void MenuDisable()
            => Execute(w => w.DisableScripts());

        [MenuItem("Tools/Project Maintenance/Enable All Scripts", priority = 11)]
        private static void MenuEnable()
            => Execute(w => w.EnableScripts());

        private static void Execute(System.Action<ProjectMaintenanceTool> action)
        {
            EnsurePaths();
            var wnd = GetWindow<ProjectMaintenanceTool>("Project Maintenance", true, typeof(SceneView));
            action(wnd);
        }

        // ───────────── GUI ─────────────
        private string _searchQuery = "";
        private Vector2 _searchScroll;
        private readonly List<string> _searchResults = new();

        private void OnEnable()
        {
            // авто-скан при первом открытии
            _buildStatsDirty = true;
        }

        private void OnGUI()
        {
            GUILayout.Label("Project Maintenance", EditorStyles.boldLabel);
            GUILayout.Space(5);

            // ===== Build Content (весь список) =====
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Build Content (только то, что реально попадает в билд)", EditorStyles.miniBoldLabel);
            if (GUILayout.Button("↻", GUILayout.Width(26))) _buildStatsDirty = true;
            EditorGUILayout.EndHorizontal();

            // авто-скан тут же
            EnsureBuildStats();

            GUILayout.Label(_buildStatsInfo, EditorStyles.miniLabel);
            if (_buildTotalBytes > 0)
            {
                GUILayout.Label($"Total: {PrettySize(_buildTotalBytes)}", EditorStyles.miniLabel);
                foreach (var kv in _buildByCategory.OrderByDescending(k => k.Value))
                    GUILayout.Label($"• {kv.Key}: {PrettySize(kv.Value)}", EditorStyles.miniLabel);

                GUILayout.Space(4);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Все файлы в билде:", EditorStyles.miniBoldLabel);
                GUILayout.FlexibleSpace();
                _buildFilter = EditorGUILayout.TextField(_buildFilter, GUILayout.MaxWidth(220));
                EditorGUILayout.EndHorizontal();

                _buildScroll = EditorGUILayout.BeginScrollView(_buildScroll, GUILayout.MinHeight(240));
                foreach (var r in FilterRows(_buildRows, _buildFilter))
                {
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("◉", GUILayout.Width(22)))
                    {
                        var obj = AssetDatabase.LoadAssetAtPath<UObject>(r.AssetPath);
                        if (obj != null) { Selection.activeObject = obj; EditorGUIUtility.PingObject(obj); }
                    }
                    GUILayout.Label(r.PrettySize, GUILayout.Width(70));
                    GUILayout.Label(r.Category, GUILayout.Width(90));
                    GUILayout.Label(r.Ext, GUILayout.Width(48));
                    GUILayout.Label(r.AssetPath, GUILayout.ExpandWidth(true));
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();
            }
            EditorGUILayout.EndVertical();

            GUILayout.Space(6);

            // ===== Search in Scripts =====
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("Search in Scripts", EditorStyles.miniBoldLabel);

            EditorGUILayout.BeginHorizontal();
            _searchQuery = EditorGUILayout.TextField(_searchQuery, GUILayout.ExpandWidth(true));
            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_searchQuery)))
            {
                if (GUILayout.Button("Seacrh", GUILayout.Width(90), GUILayout.Height(20)))
                    SearchInScripts();
            }
            EditorGUILayout.EndHorizontal();

            if (_searchResults.Count > 0)
            {
                GUILayout.Space(4);
                GUILayout.Label($"Found: {_searchResults.Count}", EditorStyles.miniLabel);

                _searchScroll = EditorGUILayout.BeginScrollView(_searchScroll, GUILayout.Height(200));
                foreach (var rel in _searchResults)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label(rel, GUILayout.ExpandWidth(true));

                    string assetPath = "Assets/" + rel.Replace('\\', '/');
                    var obj = AssetDatabase.LoadAssetAtPath<UObject>(assetPath);

                    using (new EditorGUI.DisabledScope(obj == null))
                    {
                        if (GUILayout.Button("Open", GUILayout.Width(60)))
                        {
                            Selection.activeObject = obj;
                            EditorGUIUtility.PingObject(obj);
                            AssetDatabase.OpenAsset(obj);
                        }
                    }

                    if (obj == null)
                    {
                        if (GUILayout.Button("Reveal", GUILayout.Width(60)))
                        {
                            string abs = Path.Combine(Application.dataPath, rel).Replace('\\', '/');
                            if (!abs.StartsWith(Application.dataPath.Replace('\\', '/')))
                                abs = Path.Combine(Application.dataPath, rel).Replace('\\', '/');
                            EditorUtility.RevealInFinder(abs);
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();
            }
            else
            {
                GUILayout.Space(2);
                GUILayout.Label("Нет результатов", EditorStyles.miniLabel);
            }
            EditorGUILayout.EndVertical();

            GUILayout.Space(6);

            if (GUILayout.Button("Disable Scripts", GUILayout.Height(24)))
                DisableScripts();

            if (GUILayout.Button("Enable Scripts", GUILayout.Height(24)))
                EnableScripts();
        }

        // ───────────── Disable / Enable Scripts ─────────────
        private void DisableScripts() => ToggleScripts(true);
        private void EnableScripts()  => ToggleScripts(false);

        private void ToggleScripts(bool disable)
        {
            EnsurePaths();

            string dataPath = Application.dataPath.Replace('\\', '/');

            foreach (var file in Directory.GetFiles(dataPath, "*.cs*", SearchOption.AllDirectories))
            {
                string abs = file.Replace('\\', '/');

                if (abs == ToolAbsPath || abs == ToolAbsPath + ".disabled")
                    continue;

                string rel = abs.Substring(dataPath.Length + 1);
                if (IsExcluded(rel))
                    continue;

                try
                {
                    if (disable && abs.EndsWith(".cs"))
                        File.Move(abs, abs + ".disabled");
                    else if (!disable && abs.EndsWith(".cs.disabled"))
                        File.Move(abs, abs[..^9]);
                }
                catch (IOException ioEx)
                {
                    Debug.Log($"Не удалось переименовать {abs}: {ioEx.Message}");
                }
            }

            AssetDatabase.Refresh();
        }

        // ───────────── Search helper ─────────────
        private void SearchInScripts()
        {
            EnsurePaths();
            _searchResults.Clear();

            if (string.IsNullOrWhiteSpace(_searchQuery))
                return;

            string dataPath = Application.dataPath.Replace('\\', '/');

            foreach (var file in Directory.GetFiles(dataPath, "*.cs*", SearchOption.AllDirectories))
            {
                string abs = file.Replace('\\', '/');

                if (abs == ToolAbsPath || abs == ToolAbsPath + ".disabled")
                    continue;

                string rel = abs.Substring(dataPath.Length + 1);
                if (IsExcluded(rel))
                    continue;

                try
                {
                    string content = File.ReadAllText(abs);
                    if (content.IndexOf(_searchQuery, StringComparison.OrdinalIgnoreCase) >= 0)
                        _searchResults.Add(rel);
                }
                catch (IOException ioEx)
                {
                    Debug.Log($"Не удалось прочитать {abs}: {ioEx.Message}");
                }
            }
        }

        private bool IsExcluded(string relPath)
        {
            relPath = relPath.ToLowerInvariant().Replace('\\', '/');

            foreach (var excl in excludePaths)
            {
                string p = excl.Trim('/').ToLowerInvariant();
                if (relPath.StartsWith(p + "/") || relPath == p || relPath.StartsWith(p + "."))
                    return true;
            }
            return false;
        }

        // =====================================================================
        //                          BUILD CONTENT
        // =====================================================================

        private void EnsureBuildStats()
        {
            if (!_buildStatsDirty) return;

            _buildStatsDirty = false;
            _buildStatsInfo  = "Сканирую...";
            _buildRows.Clear();
            _buildByCategory.Clear();
            _buildTotalBytes = 0;

            try
            {
                var included = CollectIncludedAssets();
                var rows = new List<BuildAssetRow>();

                string dataPath = Application.dataPath.Replace('\\', '/');
                string projectDir = dataPath.Substring(0, dataPath.Length - "Assets".Length);

                var arr = included.ToArray();
                int count = arr.Length, i = 0;

                foreach (var assetPath in arr)
                {
                    i++;
                    if (i % 250 == 0)
                        EditorUtility.DisplayProgressBar("Build content scan", assetPath, (float)i / count);

                    if (!assetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase)) continue;
                    if (assetPath.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)) continue;

                    // исключаем исходники, которые точно не попадают в билд
                    var ext = Path.GetExtension(assetPath).ToLowerInvariant();
                    if (ext is ".cs" or ".asmdef" or ".asmref" or ".rsp") continue;

                    string abs = Path.Combine(projectDir, assetPath).Replace('\\', '/');
                    if (!File.Exists(abs)) continue;

                    long size = new FileInfo(abs).Length;
                    if (size <= 0) continue;

                    string cat = DetectCategory(assetPath);

                    rows.Add(new BuildAssetRow
                    {
                        AssetPath  = assetPath,
                        Category   = cat,
                        Bytes      = size,
                        PrettySize = PrettySize(size),
                        Ext        = ext
                    });

                    _buildTotalBytes += size;
                    if (!_buildByCategory.ContainsKey(cat)) _buildByCategory[cat] = 0;
                    _buildByCategory[cat] += size;
                }

                EditorUtility.ClearProgressBar();

                _buildRows.AddRange(rows.OrderByDescending(r => r.Bytes));
                _buildStatsInfo = $"Файлов в билде: {_buildRows.Count}, общий размер: {PrettySize(_buildTotalBytes)}";
            }
            catch (Exception e)
            {
                EditorUtility.ClearProgressBar();
                _buildStatsInfo = "Ошибка анализа билда: " + e.Message;
            }
        }

        private static IEnumerable<BuildAssetRow> FilterRows(IEnumerable<BuildAssetRow> src, string filter)
        {
            if (string.IsNullOrWhiteSpace(filter)) return src;
            var f = filter.Trim().ToLowerInvariant();
            return src.Where(r =>
                r.AssetPath.ToLowerInvariant().Contains(f)
                || r.Category.ToLowerInvariant().Contains(f)
                || r.Ext.ToLowerInvariant().Contains(f));
        }

        private static HashSet<string> CollectIncludedAssets()
        {
            var set = new HashSet<string>();

            // 1) Включённые сцены + зависимости
            var scenePaths = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .Where(p => !string.IsNullOrEmpty(p))
                .ToArray();

            foreach (var p in scenePaths)
                if (IsAssetsPathStatic(p)) set.Add(p);

            if (scenePaths.Length > 0)
            {
                var deps = AssetDatabase.GetDependencies(scenePaths, true);
                foreach (var d in deps)
                    if (IsAssetsPathStatic(d)) set.Add(d);
            }

            // 2) Resources/**
            string dataPath = Application.dataPath.Replace('\\', '/');
            var resDirs = Directory.GetDirectories(dataPath, "Resources", SearchOption.AllDirectories);
            foreach (var dir in resDirs)
            {
                string relFolder = "Assets" + dir.Replace(dataPath, "").Replace('\\', '/');
                var guids = AssetDatabase.FindAssets("", new[] { relFolder });
                foreach (var g in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(g);
                    if (IsAssetsPathStatic(path)) set.Add(path);
                }
            }

            // 3) StreamingAssets/**
            string saAbs = Path.Combine(dataPath, "StreamingAssets").Replace('\\', '/');
            if (Directory.Exists(saAbs))
            {
                var files = Directory.GetFiles(saAbs, "*.*", SearchOption.AllDirectories);
                foreach (var abs in files)
                {
                    if (abs.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)) continue;
                    string rel = "Assets/StreamingAssets" + abs.Replace(saAbs, "").Replace('\\', '/');
                    set.Add(rel);
                }
            }

            // 4) Addressables
            TryCollectAddressables(set);

            return set;
        }

        private static void TryCollectAddressables(HashSet<string> set)
        {
            try
            {
                var defaultObjType = Type.GetType(
                    "UnityEditor.AddressableAssets.Settings.AddressableAssetSettingsDefaultObject, Unity.Addressables.Editor");
                if (defaultObjType == null) return;

                var settingsProp = defaultObjType.GetProperty("Settings", BindingFlags.Public | BindingFlags.Static);
                var settings = settingsProp?.GetValue(null, null);
                if (settings == null) return;

                var groupsProp = settings.GetType().GetProperty("groups");
                var groups = groupsProp?.GetValue(settings, null) as System.Collections.IEnumerable;
                if (groups == null) return;

                foreach (var g in groups)
                {
                    var entriesProp = g.GetType().GetProperty("entries");
                    var entries = entriesProp?.GetValue(g, null) as System.Collections.IEnumerable;
                    if (entries == null) continue;

                    foreach (var e in entries)
                    {
                        var guidProp = e.GetType().GetProperty("guid");
                        string guid = guidProp?.GetValue(e, null) as string;
                        if (string.IsNullOrEmpty(guid)) continue;

                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        if (IsAssetsPathStatic(path)) set.Add(path);
                    }
                }
            }
            catch { /* пакет не установлен или другая версия — игнор */ }
        }

        private static string DetectCategory(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return "Other";

            string ext = Path.GetExtension(assetPath).ToLowerInvariant();
            if (ext == ".unity")  return "Scenes";
            if (ext == ".prefab") return "Prefabs";

            var importer = AssetImporter.GetAtPath(assetPath);
            if (importer != null)
            {
                string name = importer.GetType().Name;
                if (name.Contains("TextureImporter")) return "Textures";
                if (name.Contains("AudioImporter"))   return "Audio";
                if (name.Contains("ModelImporter"))   return "Models";
            }

            if (ext is ".png" or ".jpg" or ".jpeg" or ".tga" or ".psd" or ".tif" or ".exr" or ".hdr")
                return "Textures";
            if (ext is ".wav" or ".mp3" or ".ogg" or ".flac" or ".aac")
                return "Audio";
            if (ext is ".fbx" or ".obj" or ".blend")
                return "Models";
            if (ext is ".shader" or ".shadergraph")
                return "Shaders";
            if (ext is ".mp4" or ".mov" or ".webm")
                return "Video";
            if (ext is ".dll")
                return "Plugins";

            return "Other";
        }

        private static bool IsAssetsPathStatic(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            path = path.Replace('\\', '/');
            return path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase);
        }

        private static string PrettySize(long bytes)
        {
            double b = bytes;
            string[] u = { "B", "KB", "MB", "GB" };
            int i = 0;
            while (b >= 1024 && i < u.Length - 1) { b /= 1024; i++; }
            return $"{b:0.##} {u[i]}";
        }
    }
}
#endif
