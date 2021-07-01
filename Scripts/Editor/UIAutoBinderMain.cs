#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UI;

public static class UIAutoBinderMain
{
    internal struct GenInfo
    {
        public Type Type;
        public string Name;
        public UnityEngine.Object Object;
    }

    private static List<Type> GenTypes = new List<Type>
    {
        typeof(Button),
        typeof(Image),
        typeof(Text),
        typeof(Slider),
        typeof(ScrollRect),
        typeof(InputField),
        typeof(Toggle),
        typeof(ToggleGroup),
        typeof(RawImage),
        typeof(Scrollbar),
        typeof(Dropdown),
        typeof(TextMeshProUGUI),
    };

    
    private static UIAutoBinderConfig cfgIns;
    private static UIAutoBinderConfig _config
    {
        get
        {
            if (cfgIns != null)
            {
                return cfgIns;
            }
            cfgIns = AssetDatabase.LoadAssetAtPath<UIAutoBinderConfig>("Assets/UIAutoBinder/Config.asset");
            if (cfgIns != null) return cfgIns;
            cfgIns = ScriptableObject.CreateInstance<UIAutoBinderConfig>();
            if (!AssetDatabase.IsValidFolder("Assets/UIAutoBinder/"))
            {
                AssetDatabase.CreateFolder("Assets","UIAutoBinder");
            }
            AssetDatabase.CreateAsset(cfgIns, "Assets/UIAutoBinder/Config.asset");

            return cfgIns;
        }
    }

    private static string UIPrefabPath => _config.UIPrefabPath;
    internal static string CodeGenPath => _config.CodeGenPath;
    internal const string CodeTemplate = @"using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class %CLASSNAME%
{
%INSERT%
}
";

    internal static string GenDefine(Type t, string name)
    {
        return $"   [HideInInspector]\r\n   public {t.Name} {name};";
    }

    internal static void DeepCheckInfo(Transform root, Dictionary<string, GenInfo> info)
    {
        for (var i = 0; i < root.childCount; i++)
        {
            var c = root.GetChild(i);
            var str = c.name.Split('_');
            if (str[0] == "R")
            {
                foreach (var type in GenTypes)
                {
                    var obj = c.GetComponent(type);
                    if (obj != null)
                    {
                        var key = $"m_{type.Name}{str[1]}";
                        if (info.ContainsKey(key))
                        {
                            throw new Exception($"{root.root.name}检查到重复的节点名称{str[1]}");
                        }
                        info.Add(key, new GenInfo
                        {
                            Type = type,
                            Object = obj,
                            Name = key
                        });
                    }
                }   
            }
            DeepCheckInfo(c, info);
        }
    }

    internal static MonoBehaviour GetGenClass(MonoBehaviour[] gs)
    {
        foreach (var m in gs)
        {
            var a = m.GetType().IsSubclassOf(typeof(UIBase));
            if (a)
            {
                return m;
            }
        }

        return null;
    }

    internal static void BindUI(string path)
    {
        if (!path.EndsWith(".prefab"))
        {
            return;
        }
        try
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var gs = go.GetComponents<MonoBehaviour>();
            var gt = GetGenClass(gs)?.GetType();
            if (gt == null)
            {
                return;
            }
            EditorUtility.DisplayProgressBar("UIBinder", "UI代码生成检查...", 0f);
            var dic = new Dictionary<string, GenInfo>();
            var sb = new StringBuilder();
            DeepCheckInfo(go.transform, dic);
            foreach (var info in dic.Values)
            {
                sb.AppendLine(GenDefine(info.Type, info.Name));
            }

            var code = CodeTemplate.Replace("%INSERT%", sb.ToString());
            code = code.Replace("%CLASSNAME%", gt.Name);
            if (!Directory.Exists(CodeGenPath))
            {
                Directory.CreateDirectory(CodeGenPath);
            }

            var outPath = $"{CodeGenPath}/{gt.Name}Define.cs";
            try
            {
                if (File.ReadAllText(outPath) == code)
                {
                    EditorUtility.ClearProgressBar();
                    return;
                }
            }
            catch (Exception)
            {
                Debug.Log($"{outPath}文件有改变，需要生成。");
            }

            var txt = File.CreateText(outPath);
            txt.Write(code);
            txt.Close();
            EditorUtility.DisplayProgressBar("UIBinder", "UI代码生成检查...", 1f);
            AssetDatabase.ImportAsset(outPath);
            Debug.Log($"界面代码生成{path}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"{path}:{e.Message}");
            //EditorUtility.DisplayDialog("UIBinder", $"{path}:{e.Message}", "Ok");
        }
        EditorUtility.ClearProgressBar();
    }

    private static void BindUIElements(GameObject go)
    {
        if (go == null)
        {
            EditorUtility.ClearProgressBar();
            return;
        }
        
        var gs = go.GetComponents<MonoBehaviour>();
        var t = GetGenClass(gs);
        if (t == null)
        {
            EditorUtility.ClearProgressBar();
            return;
        }
        var dic = new Dictionary<string, GenInfo>();
        DeepCheckInfo(go.transform, dic);
        foreach (var info in dic.Values)
        {
            var tp = t.GetType();
            var f = tp.GetField(info.Name, BindingFlags.Public | BindingFlags.Instance);
            f?.SetValue(t, info.Object);
        }
        EditorUtility.SetDirty(go);
        EditorUtility.ClearProgressBar();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    [MenuItem("GameObject/UIHelper/BindUIElements", false, 10)]
    private static void BindUIElements()
    {
        EditorUtility.DisplayProgressBar("UIBinder", "UI代码自动绑定...", 0f);
        var go = Selection.activeGameObject;
        BindUIElements(go);
    }
    

    [DidReloadScripts]
    private static void Bind()
    {
        EditorUtility.DisplayProgressBar("UIBinder", "UI代码自动绑定...", 0f);
        if (!Directory.Exists(UIPrefabPath))
        {
            Debug.LogWarning($"自动绑定失效，请指定有效的UI预制体路径。");
            EditorUtility.ClearProgressBar();
            return;
        }
        
        var paths = Directory.GetFiles(UIPrefabPath, "*.prefab", SearchOption.AllDirectories);
        var dic = new Dictionary<string, GenInfo>();
        for (var i = 0; i < paths.Length; i++)
        {
            var path = paths[i];
            try
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path.Replace(Application.dataPath, "Assets"));
                var gs = go.GetComponents<MonoBehaviour>();
                var t = GetGenClass(gs);
                if (t == null)
                {
                    continue;
                }
                dic.Clear();
                DeepCheckInfo(go.transform, dic);
                foreach (var info in dic.Values)
                {
                    var tp = t.GetType();
                    var f = tp.GetField(info.Name, BindingFlags.Public | BindingFlags.Instance);
                    f?.SetValue(t, info.Object);
                }
                EditorUtility.SetDirty(go);
                EditorUtility.DisplayProgressBar("UIBinder", "UI代码自动绑定...", (float)i/paths.Length);
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("UIBinder", $"{path}:{e.Message}", "Ok");
            }
        }
        
        EditorUtility.ClearProgressBar();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
public class FileModificationWarning : UnityEditor.AssetModificationProcessor
{
    static string[] OnWillSaveAssets(string[] paths)
    {
        EditorApplication.delayCall += () =>
        {
            foreach (var path in paths)
            {
                UIAutoBinderMain.BindUI(path);
            }

            AssetDatabase.Refresh();
        };
        return paths;
    }
}

#endif