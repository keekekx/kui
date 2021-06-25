using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class UIManager : MonoBehaviour
{
    public GameObject LayerTemplate;
    public static UIManager Instance;

    private Dictionary<string, GameObject> _uiDic = new Dictionary<string, GameObject>();
    private Dictionary<int, RectTransform> _layerRootDic = new Dictionary<int, RectTransform>();

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private RectTransform GetLayer(int layer)
    {
        if (!_layerRootDic.TryGetValue(layer, out var l))
        {
            var layerGO = Instantiate(LayerTemplate, transform, false);
            layerGO.name = $"layer_{layer}";
            layerGO.transform.SetParent(transform);
            l = layerGO.GetComponent<RectTransform>();
            _layerRootDic.Add(layer, l);
        }

        return l;
    }

    public T Open<T>(string key, int layer = 0)
    {
        var op = Addressables.InstantiateAsync(key, GetLayer(layer));
        var go = op.WaitForCompletion();
        _uiDic[key] = go;
        return go.GetComponent<T>();
    }

    public void Open(string key, int layer = 0)
    {
        var op = Addressables.InstantiateAsync(key, GetLayer(layer));
        var go = op.WaitForCompletion();
        _uiDic[key] = go;
    }

    public T GetUI<T>(string key)
    {
        return _uiDic.TryGetValue(key, out var go) ? go.GetComponent<T>() : default;
    }

    public void Close(string key)
    {
        if (!_uiDic.TryGetValue(key, out var go)) return;
        Addressables.ReleaseInstance(go);
        _uiDic.Remove(key);
    }
}
