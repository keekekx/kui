public enum State
{
    None,
    Init,
    Showing,
    Hiding,
    Destroy,
}

public class UIContext
{
    public string Prefab;
    public object[] Params;
    public State State;
    public UILayer Layer;
    public UIBase UI;
}