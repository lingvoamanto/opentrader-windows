using System;
#if __MACOS__
using AppKit;
#endif
#if __IOS__
using UIKit;
#endif
using System.IO;

namespace OpenTrader
{
    public static class DebugHelper
    {
        public static void Alert(Exception e)
        {
            System.Diagnostics.StackTrace stack = new System.Diagnostics.StackTrace(e, true);
            var path = stack.GetFrame(0).GetFileName();
            var fileName = Path.GetFileName(path);
            var method = stack.GetFrame(0).GetMethod().Name;
            string lineNumber = stack.GetFrame(1).GetFileLineNumber().ToString();
            string message = "(" + lineNumber + ") " + e.Message;
#if __MACOS__
            NSAlert alert = new NSAlert()
            {
                AlertStyle = NSAlertStyle.Critical,
                InformativeText = fileName+" "+method,
                MessageText = e.ToString()
            };
            // alert.RunModal();
            alert.RunSheetModal(NSApplication.SharedApplication.MainWindow);
#endif
#if __IOS__
            var alertController = UIAlertController.Create(fileName + " " + method, e.ToString(), UIAlertControllerStyle.Alert);
            alertController.AddAction(UIAlertAction.Create("OK", UIAlertActionStyle.Default, null));
            UIApplication.SharedApplication.Delegate.GetWindow().RootViewController.PresentViewController(alertController, true, null);
#endif
        }

        public static void WriteLine(Exception e)
        {
            System.Diagnostics.StackTrace stack = new System.Diagnostics.StackTrace(e, true);
            var path = stack.GetFrame(0).GetFileName();
            var fileName = Path.GetFileName(path);
            var method = stack.GetFrame(0).GetMethod().Name;
            string lineNumber = stack.GetFrame(0).GetFileLineNumber().ToString();
            System.Diagnostics.Debug.WriteLine(fileName+"("+lineNumber+")"+method+" "+e.Message);
        }

        public static void WriteLine(string message)
        {
            string path = new System.Diagnostics.StackTrace(true).GetFrame(1).GetFileName();
            var fileName = Path.GetFileName(path);
            int lineNumber = new System.Diagnostics.StackTrace(true).GetFrame(1).GetFileLineNumber();
            var method = new System.Diagnostics.StackTrace(true).GetFrame(1).GetMethod().Name;
            System.Diagnostics.Debug.WriteLine(fileName + "(" + lineNumber.ToString() + ")" + method + " " + message);
        }
    }
}
