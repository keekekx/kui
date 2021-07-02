using System;

namespace KUI
{
    public class UIConfig : Attribute
    {
        public string Address { get; set; }
        public int Layer { get; set; }
    }
}