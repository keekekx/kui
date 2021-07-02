using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace KUI
{
    public class UIManager : MonoBehaviour
    {
        [Serializable]
        public class LayerConfig
        {
            public int Layer;
            public UILayer.ShowMode ShowMode;
        }
        public GameObject LayerTemplate;
        public List<LayerConfig> LayerConfigs = new List<LayerConfig>();

        public static Func<string, GameObject> UILoadFunction;
    
        public static UIManager Instance;

        private Dictionary<string, UIContext> _uiDic = new Dictionary<string, UIContext>();
        private Dictionary<int, UILayer> _layerRootDic = new Dictionary<int, UILayer>();

        private Dictionary<UIContext, IUIUpdate> _uiUpdatesDic = new Dictionary<UIContext, IUIUpdate>();

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private UILayer GetLayer(int layer)
        {
            if (!_layerRootDic.TryGetValue(layer, out var l))
            {
                var layerGO = Instantiate(LayerTemplate, transform, false);
                layerGO.name = $"layer_{layer}";
                layerGO.transform.SetParent(transform);
                l = layerGO.GetComponent<UILayer>();
                var cfg = LayerConfigs?.Find(c => c.Layer == layer);
                if (cfg != null)
                {
                    l.m_ShowMode = cfg.ShowMode;
                }
                _layerRootDic.Add(layer, l);
            }

            return l;
        }

        public T Open<T>(params object[] data) where T : UIBase
        {
            if (UILoadFunction == null)
            {
                throw new Exception("没有设置UI界面加载接口。");
            }
            var cfg = typeof(T).GetCustomAttribute<UIConfig>();
            if (cfg == null)
            {
                throw new Exception("UI需要配置UIConfig，具体详情查看UIConfig Attribute。");
            }

            var layer = GetLayer(cfg.Layer);
            if (!_uiDic.TryGetValue(cfg.Address, out var ctx))
            {
                ctx = new UIContext
                {
                    Prefab = cfg.Address,
                    Layer = layer,
                    Params = data,
                    State = State.Init,
                };

            }
            layer.OperatorOpen(ctx);

            if (ctx.UI is IUIUpdate updater && !_uiUpdatesDic.ContainsKey(ctx))
            {
                _uiUpdatesDic.Add(ctx, updater);
            }
            _uiDic[cfg.Address] = ctx;
            return ctx.UI as T;
        }

        public T GetUI<T>(string addr) where T : UIBase
        {
            return _uiDic.TryGetValue(addr, out var ctx) ? ctx.UI as T: default;
        }

        public void Close(string addr)
        {
            if (!_uiDic.TryGetValue(addr, out var ctx)) return;
            if (ctx.Layer.OperatorClose(ctx))
            {
                _uiUpdatesDic.Remove(ctx);
                //Addressables.ReleaseInstance(ctx.UI.gameObject);
                _uiDic.Remove(addr);
            }
        }

        public void Back(string addr)
        {
            if (!_uiDic.TryGetValue(addr, out var ctx)) return;
            if (ctx.Layer.Back(ctx))
            {
                _uiUpdatesDic.Remove(ctx);
                //Addressables.ReleaseInstance(ctx.UI.gameObject);
                _uiDic.Remove(addr);
            }
        }

        private void Update()
        {
            var delta = Time.deltaTime;
            foreach (var ups in _uiUpdatesDic)
            {
                if (ups.Key.State == State.Showing)
                {
                    try
                    {
                        ups.Value.OnUpdate(delta);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                    }
                }
            }
        }

        public void Dispatch(object key, params object[] param)
        {
        
        }
    }
}