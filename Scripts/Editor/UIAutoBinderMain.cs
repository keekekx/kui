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
    private struct GenInfo
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
            cfgIns = AssetDatabase.LoadAssetAtPath<UIAutoBinderConfig>("Assets/UIAutoBinder/config.asset");
            if (cfgIns != null) return cfgIns;
            cfgIns = ScriptableObject.CreateInstance<UIAutoBinderConfig>();
            if (!AssetDatabase.IsValidFolder("Assets/UIAutoBinder/"))
            {
                AssetDatabase.CreateFolder("Assets","UIAutoBinder");
            }
            AssetDatabase.CreateAsset(cfgIns, "Assets/UIAutoBinder/config.asset");

            return cfgIns;
        }
    }

    private static string UIPrefabPath => _config.UIPrefabPath;
    private static string CodeGenPath => _config.CodeGenPath;
    private const string CodeTemplate = @"using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class %CLASSNAME% : MonoBehaviour
{
%INSERT%
}
";

    private static string GenDefine(Type t, string name)
    {
        return $"   public {t.Name} {name};";
    }

    private static void DeepCheckInfo(Transform root, Dictionary<string, GenInfo> info)
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

    private static MonoBehaviour GetGenClass(MonoBehaviour[] gs)
    {
        foreach (var m in gs)
        {
            var a = m.GetType().GetCustomAttribute(typeof(GenUIBindCode));
            if (a != null)
            {
                return m;
            }
        }

        return null;
    }

    [MenuItem("UIAutoBinder/Gen Code And Bind")]
    private static void ChangeTestValue()
    {
        var paths = Directory.GetFiles(Application.dataPath + UIPrefabPath, "*.prefab", SearchOption.AllDirectories);
        var dic = new Dictionary<string, GenInfo>();
        var sb = new StringBuilder();
        foreach (var path in paths)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path.Replace(Application.dataPath, "Assets"));
            var gs = go.GetComponents<MonoBehaviour>();
            var gt = GetGenClass(gs)?.GetType();
            if (gt == null)
            {
                continue;
            }
            dic.Clear();
            sb.Clear();
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
            var txt = File.CreateText($"{CodeGenPath}/{gt.Name}Define.cs");
            txt.Write(code);
            txt.Close();
        }

        AssetDatabase.Refresh();
    }

    [DidReloadScripts]
    private static void Bind()
    {
        var paths = Directory.GetFiles(Application.dataPath + UIPrefabPath, "*.prefab", SearchOption.AllDirectories);
        var dic = new Dictionary<string, GenInfo>();
        foreach (var path in paths)
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
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
