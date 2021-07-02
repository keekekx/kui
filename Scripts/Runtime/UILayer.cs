using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KUI
{
    public class UILayer : MonoBehaviour
    {
        public enum ShowMode
        {
            None,
            Stack,
            Queue,
        }

        public ShowMode m_ShowMode;

        private List<UIContext> _operators = new List<UIContext>();
    
        private void Awake()
        {
            ApplySafeArea();
        }

        private void ApplySafeArea()
        {
            var r = Screen.safeArea;
            var rt = transform as RectTransform;
            var anchorMin = r.position;
            var anchorMax = r.position + r.size;
        
            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
        }

        public void OperatorOpen(UIContext ctx)
        {
            if (ctx.State == State.Init)
            {
                var temp = UIManager.UILoadFunction.Invoke(ctx.Prefab);
                var go = Instantiate(temp, transform);
                ctx.UI = go.GetComponent<UIBase>();
                ctx.UI.PreAwake(ctx.Params);
                ctx.UI.Context = ctx;
            }
        
            switch (m_ShowMode)
            {
                case ShowMode.None:
                    ctx.UI.Show();
                    ctx.UI.transform.SetAsLastSibling();
                    if (!_operators.Contains(ctx))
                    {
                        _operators.Add(ctx);
                    }
                    break;
                case ShowMode.Stack:
                    if (_operators.Count > 0)
                    {
                        var old = _operators[_operators.Count - 1];
                        old.State = State.Hiding;
                        old.UI.Hide();
                    }
                    ctx.UI.Show();
                    _operators.Add(ctx);
                    break;
                case ShowMode.Queue:
                {
                    if (_operators.Count == 0)
                    {
                        ctx.UI.Show();
                        _operators.Insert(0, ctx);
                    }
                    else
                    {
                        ctx.UI.Hide();
                        _operators.Add(ctx);
                    }
                }
                    return;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        
        }

        public bool OperatorClose(UIContext ctx)
        {
            if (!_operators.Contains(ctx))
            {
                throw new Exception("貌似这层没有这个UI呢");
            }
            switch (m_ShowMode)
            {
                case ShowMode.None:
                    _operators.Remove(ctx);
                    break;
                case ShowMode.Stack:
                {
                    if (_operators[_operators.Count - 1] != ctx)
                    {
                        throw new Exception("最后一个打开的窗口不是该窗口");
                    }

                    ctx.UI.Hide();
                    _operators.RemoveAt(_operators.Count - 1);
                    if (_operators.Count > 0)
                    {
                        var last = _operators[_operators.Count - 1];
                        last.UI.Show();
                    }

                    break;
                }
                case ShowMode.Queue:
                {
                    if (_operators[0] != ctx)
                    {
                        throw new Exception("第一个打开的窗口不是该窗口");
                    }

                    ctx.UI.Hide();
                    _operators.RemoveAt(0);
                    if (_operators.Count > 0)
                    {
                        var head = _operators[0];
                        head.UI.Show();
                    }

                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return !_operators.Contains(ctx);
        }

        public bool Back(UIContext ctx)
        {
            if (_operators.Count == 0)
            {
                throw new Exception("没有界面可以关闭");
            }

            switch (m_ShowMode)
            {
                case ShowMode.None:
                    throw new Exception("普通模式不支持返回操作");
                case ShowMode.Stack:
                {
                    if (_operators[_operators.Count - 1] != ctx)
                    {
                        throw new Exception("最后一个打开的窗口不是该窗口");
                    }
                    ctx.UI.Hide();
                    _operators.RemoveAt(_operators.Count - 1);
                    if (_operators.Count > 0)
                    {
                        var last = _operators[_operators.Count - 1];
                        last.UI.Show();
                    }
                    break;
                }
                case ShowMode.Queue:
                {
                    if (_operators[0] != ctx)
                    {
                        throw new Exception("第一个打开的窗口不是该窗口");
                    }

                    ctx.UI.Hide();
                    _operators.RemoveAt(0);
                    if (_operators.Count > 0)
                    {
                        var head = _operators[0];
                        head.UI.Show();
                    }
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return !_operators.Contains(ctx);
        }
    }
}