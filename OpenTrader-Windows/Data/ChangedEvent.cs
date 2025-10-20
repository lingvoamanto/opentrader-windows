using System;
namespace OpenTrader.Data
{
    public enum ChangedAction { Add, Remove, Replace }

    public class ChangedEventArgs : EventArgs
    {
        public ChangedAction Action;

        public ChangedEventArgs(ChangedAction Action)
        {
            this.Action = Action;
        }
    }

    public delegate void ChangedEvent(object sender, ChangedEventArgs e);
}
