using System;
#if __MACOS__
using AppKit;
using System.Collections.Generic;
using Foundation;
using CoreGraphics;
#endif
#if __WINDOWS__
using System.Windows.Controls;
#endif

namespace OpenTrader
{
#if __MACOS__
    public class LogPage : NSScrollView, ITraderPage
#endif
 #if __WINDOWS__
    public class LogPage : TraderPage
#endif
    {
#if __MACOS__
        NSTextView textView;
#endif
#if __WINDOWS__
        TextBox textBox;
#endif

        public PageType PageType
        {
            get { return OpenTrader.PageType.Log; }
        }


        public TraderBook TraderBook { get; set; }

#if __MACOS__
        public void WriteLine(string text)
        {
            textView.TextStorage.Append(new NSAttributedString(text+"\r\n"));
            NSRange range = new NSRange(textView.String.Length, 0);
            textView.ScrollRangeToVisible(range);
        }
#endif
#if __WINDOWS__
        public void WriteLine(string text)
        {
            textBox.Text = textBox.Text + "\r\n" + text;
        }
#endif
#if __MACOS__
        public void Write(string text)
        {
            textView.TextStorage.Append(new NSAttributedString(text));
            NSRange range = new NSRange(0, textView.String.Length);
            textView.ScrollRangeToVisible(range);
        }
#endif
#if __WINDOWS__
        public void Write(string text)
        {
            textBox.Text = textBox.Text + text;
        }
#endif
#if __MACOS__
        public LogPage(TraderBook traderBook, CGRect frameRect) :base(frameRect)
        {
            TraderBook = traderBook;
            // mTextStorage = new NSTextStorage("");
            // mTextStorage.SetString(new NSAttributedString(""));

            CGSize bookSize = new CGSize(frameRect.Width, frameRect.Height);
            // Allow the scroll bars to be 50
            CGRect textRect = new CoreGraphics.CGRect(0, 0, bookSize.Width-50, bookSize.Height-50);
            CGSize textSize = new CGSize(textRect.Width, textRect.Height);

            // NSTextContainer textContainer = new NSTextContainer(textSize);
            // NSLayoutManager layoutManager = new NSLayoutManager();
            // layoutManager.AddTextContainer(textContainer);
            //mTextStorage.AddLayoutManager(layoutManager);
            textView = new NSTextView(textRect); // ' , textContainer);

            textView.Selectable = true;
            textView.VerticallyResizable = true;
            textView.HorizontallyResizable = true;

            this.DocumentView = textView; // do this rather than add a subview
            this.HasHorizontalScroller = true;
            this.HasVerticalScroller = true;

            WriteLine("Started logging...\r\n");
        }
#endif
#if __WINDOWS__
        public LogPage(TraderBook traderBook) : base(traderBook, PageType.Log)
        {
            textBox = new TextBox();
        }
#endif
    }
}
