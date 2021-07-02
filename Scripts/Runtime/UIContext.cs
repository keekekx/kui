namespace KUI
{
    public class UIContext
    {
        public string Prefab;
        public object[] Params;
        public State State;
        public UILayer Layer;
        public UIBase UI;
    }

    public enum State
    {
        None,
        Init,
        Showing,
        Hiding,
        Animation,
        Destroy,
    }
}