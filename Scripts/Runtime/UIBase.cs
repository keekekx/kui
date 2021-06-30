using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIBase : MonoBehaviour
{
    public UIContext Context;
    /// <summary>
    /// UIManager Open时，第一次创建会调用该接口
    /// </summary>
    public virtual void PreAwake(object[] param)
    {
        
    }

    public virtual void Show()
    {
        PreShow();
        gameObject.SetActive(true);
        OnShow();
    }

    /// <summary>
    /// 窗口显示前调用
    /// </summary>
    public virtual void PreShow()
    {
        
    }

    /// <summary>
    /// 窗口显示时调用
    /// </summary>
    public virtual void OnShow()
    {
        Context.State = State.Showing;
    }

    public virtual void Hide()
    {
        PreHide();
        gameObject.SetActive(false);
        OnHide();
    }

    public virtual void PreHide()
    {
        
    }

    public virtual void OnHide()
    {
        Context.State = State.Hiding;
    }

    public virtual void Close()
    {
        PreClose();
        OnClose();
        UIManager.Instance.Close(Context.Prefab);
    }

    public virtual void Back()
    {
        PreClose();
        OnClose();
        UIManager.Instance.Back(Context.Prefab);
    }

    /// <summary>
    /// 窗口关闭前调用
    /// </summary>
    public virtual void PreClose()
    {
        
    }

    /// <summary>
    /// 窗口关闭时调用
    /// </summary>
    public virtual void OnClose()
    {
        
    }
}
