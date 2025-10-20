using System;
using System.Collections.Generic;
#if __MACOS__
using AppKit;
using CoreGraphics;
using Foundation;
#endif
#if __WINDOWS__
using System.Windows.Controls;
#endif

namespace OpenTrader
{
#if __MACOS__
    [Register("ParameterBar")]

    public class ParameterBar : NSStackView
#endif
#if __WINDOWS__
    public class ParameterBar : StackPanel
#endif
    {
#if __WINDOWS__
        public List<Controls.ParameterControl> ParameterControls;
#endif
        TraderBook traderBook;
        public TraderBook TraderBook {  set { traderBook = value;  } }



#if __MACOS__
        private void Initialise()
        {
            this.Delegate = new ParameterDelegate();
            this.Orientation = NSUserInterfaceLayoutOrientation.Horizontal;
            this.Distribution = NSStackViewDistribution.FillEqually;
            this.Spacing = 5;
            this.TranslatesAutoresizingMaskIntoConstraints = true;
            this.Alignment = NSLayoutAttribute.Top;
        }

        public ParameterBar(CGRect parameterRect, TraderBook traderbook) : base(parameterRect)
        {
            this.Delegate = new ParameterDelegate();
            this.Orientation = NSUserInterfaceLayoutOrientation.Horizontal;
            this.Distribution = NSStackViewDistribution.FillEqually;
            this.Spacing = 5;
            this.TranslatesAutoresizingMaskIntoConstraints = true;
            this.Alignment = NSLayoutAttribute.Bottom;
            this.AutoresizingMask = NSViewResizingMask.WidthSizable;
            traderBook = traderbook;
        }

        public ParameterBar(IntPtr handle) : base(handle)
        {
            this.Delegate = new ParameterDelegate();
            this.Orientation = NSUserInterfaceLayoutOrientation.Horizontal;
            this.Distribution = NSStackViewDistribution.FillEqually;
            this.Spacing = 5;
            this.Alignment = NSLayoutAttribute.Bottom;
            this.AutoresizingMask = NSViewResizingMask.WidthSizable;
            this.TranslatesAutoresizingMaskIntoConstraints = true;
        }

        public void BuildParameters()
        {
            if ((NSApplication.SharedApplication.Delegate as AppDelegate).IsProfiling)
                (NSApplication.SharedApplication.Delegate as AppDelegate).ProfileStack.Push(
                "ParameterBar", "BuildParameters");
            if (traderBook.TraderScript != null)
            {
                foreach(NSView subView in this.Subviews)
                {
                    this.RemoveView(subView);
                    subView.Dispose();
                }

                foreach (StrategyParameter parameter in traderBook.TraderScript.StrategyParameters)
                {
                    ParameterItem item = new ParameterItem(parameter);
                    item.TraderBook = traderBook;
                    this.AddView(item,NSStackViewGravity.Trailing);
                    item.Display();
                }
            }
            if((NSApplication.SharedApplication.Delegate as AppDelegate).IsProfiling)
                (NSApplication.SharedApplication.Delegate as AppDelegate).ProfileStack.Pop();
        }

        override public void MouseEntered(AppKit.NSEvent theEvent)
        {
            this.Window.AcceptsMouseMovedEvents = true;
            this.Window.BecomeFirstResponder();
            this.Window.MakeFirstResponder(this);
        }

        override public void MouseExited(AppKit.NSEvent theEvent)
        {
            this.Window.AcceptsMouseMovedEvents = false;
        }

        public override bool AcceptsFirstResponder()
        {
            this.Window.MakeFirstResponder(this);
            return true;
        }

#endif

#if __WINDOWS__
        public ParameterBar( TraderBook traderBook)
        {
            this.Orientation = Orientation.Horizontal;
            this.traderBook = traderBook;
            ParameterControls = new List<Controls.ParameterControl>();
            BuildParameters();
        }

        public void BuildParameters()
        {
            ParameterControls.Clear();

            if( traderBook.TraderScript != null )
            {
                foreach( var parameter in traderBook.TraderScript.StrategyParameters )
                {
                    var control = new Controls.ParameterControl(parameter);
                    control.TraderBook = traderBook;
                    ParameterControls.Add(control);
                    base.Children.Add(control);
                }
            }
        }
#endif
    }
}

