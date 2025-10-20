using System;
namespace OpenTrader
{
    public partial class Repository
    {
        public class Column
        {
            public string Property { get; private set; }
            public string Name { get; private set; }
            public string Type { get; private set; }
            public string Modifiers { get; private set; }

            public Column(string property, string name, string type, string modifiers)
            {
                Property = property;
                Name = name;
                Type = type;
                Modifiers = modifiers;
            }
        }
    }
}
