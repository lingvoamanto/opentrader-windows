using System.Collections.Generic;

class Frame
{
    List<object> memory;

    internal List<object> Memory
    {
        get { return memory; }
    }

    internal void Alloc(object? obj)
    {
        if (obj == null)
            memory.Add(new object());
        else
            memory.Add(obj);
    }

    internal Frame()
    {
        memory = new List<object>();
    }
}