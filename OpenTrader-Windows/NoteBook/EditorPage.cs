using System;
#if __MACOS__
using AppKit;
using Foundation;
using CoreGraphics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
#endif
#if __WINDOWS__
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows;
#endif
using System.Drawing;
using System.Collections;
using System.IO;
using Microsoft.CodeAnalysis;

using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using Microsoft.CSharp;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using OpenCompiler;
using OpenTrader.Controls;

namespace OpenTrader
{
#if __MACOS__
    public class EditorPage : NSSplitView, ITraderPage
#endif
#if __WINDOWS__
	public class EditorPage : TraderPage
#endif
    {
#if __MACOS__
        NSViewController mViewController;
        NSButton mCompileButton;
        NSTextView mTextEditor;
        NSTextView mCompileTextView;
#endif
#if __WINDOWS__
        Controls.EditorControl editorControl;
        string? path;
#endif

        Language language;
        public new Language Language { get => language; set { language = value; } }



#if __MACOS__
        public string Script
        {
            get {  return mTextEditor.String; } 
        }

        public NSViewController ViewController
        {
            get { return mViewController;  }
        }
#endif

        public PageType PageType
        {
            get { return OpenTrader.PageType.Editor; }
        }

#if __MACOS__
        public NSTextView CompileTextView
        {
            get
            {
                return mCompileTextView;
            }
        }
#endif

#if __MACOS__
        public void WriteLine(string text)
        {
            mCompileTextView.TextStorage.Append(new NSAttributedString(text + "\r\n"));
            NSRange range = new NSRange(mCompileTextView.String.Length, 0);
            mCompileTextView.ScrollRangeToVisible(range);
        }

        public void Write(string text)
        {
            mCompileTextView.TextStorage.Append(new NSAttributedString(text));
            NSRange range = new NSRange(mCompileTextView.String.Length, 0);
            mCompileTextView.ScrollRangeToVisible(range);
        }
#endif

#if __WINDOWS__
        public void WriteLine(string text)
        {
        }

        public void Write(string text)
        {
        }

        public string Text
        {
            get => editorControl.AvalonEditor.Text;
        }

        private void Save(object sender, EventArgs e)
        {
            File.WriteAllTextAsync(path, editorControl.AvalonEditor.Text);
        }

        private void SaveAs(object sender, EventArgs e)
        {
            var sfd = new System.Windows.Forms.SaveFileDialog()
            {
                Title = "Save Strategy",
                CheckFileExists = false,
                InitialDirectory = "D:\\Dropbox\\Trading\\TradingScripts"
            };

            sfd.ShowDialog();

            if (sfd.FileName != "")
            {
                path = sfd.FileName;
                File.WriteAllTextAsync(path, editorControl.AvalonEditor.Text);
            }
        }
#endif

        public TraderBook TraderBook
        {
            get; set;
        }



#if __APPLEOS__
        public EditorPage(Language language, TraderBook parent, NSTextStorage document, CGRect frameRect) : base(frameRect)
        {
            TraderBook = parent;
            this.language = language;
            CGSize parentSize = new CGSize (parent.Bounds.Width,parent.Bounds.Height);
            // mVBox = new VBox(false, 0);

            // Set up  the  scroll bar

            // Set up the editor


            // string text = document.Text;
            // Mono.TextEditor.TextEditor texteditor = new Mono.TextEditor.TextEditor(document);
            // texteditor.Document.MimeType = "text/x-csharp";

            CGRect texteditorRect = new CoreGraphics.CGRect(0, 0, frameRect.Width, frameRect.Height - 250);
            NSViewController svController = new NSViewController();
            NSScrollView sv = new NSScrollView(texteditorRect);
            sv.HasHorizontalScroller  = true;
            sv.HasVerticalScroller = true;

            // svController.View = sv;


            CGSize texteditorSize = sv.ContentSize;

            mTextEditor = new NSTextView(texteditorRect);
            mTextEditor.TextStorage.SetString(document);
            if (language == Language.CSharp)
            {
                mTextEditor.TextStorage.Delegate = new SyntaxHighlighter();
            }
            mTextEditor.Font = NSFont.FromFontName("Menlo", 12);
            // mTextEditor.TextStorage.AddAttribute(NSStringAttributeKey.Font,NSFont.FromFontName("Menlo",20),new NSRange(0,document.Length));
            mTextEditor.Editable = true;
            mTextEditor.Selectable = true;
            mTextEditor.VerticallyResizable = true;
            mTextEditor.HorizontallyResizable = true;


            //sv.AddSubview(texteditor);
            sv.DocumentView = mTextEditor;
            sv.VerticalRulerView = new LineNumberRulerView(sv);
            sv.RulersVisible = true;



            CGRect compileRect = new CoreGraphics.CGRect(0, parentSize.Height - 250, parentSize.Width, 250);
            NSSplitView compileView = new NSSplitView(compileRect);
            NSSplitViewController compileViewController = new NSSplitViewController();
            compileViewController.View = compileView;

            mCompileTextView = new NSTextView(new CGRect(0, 0, parentSize.Width, 200));
            mCompileTextView.Editable = false;
            mCompileTextView.Selectable = true;
            mCompileTextView.VerticallyResizable = true;
            mCompileTextView.HorizontallyResizable = true;


            mCompileButton = new NSButton(new CGRect(0, 200, 120, 50));
            mCompileButton.Title = "Compile";
            mCompileButton.Activated += CompileButton_Clicked;
            compileView.AddSubview(mCompileTextView);
            compileView.AddSubview(mCompileButton);


            // mCompileTextView.HeightRequest = 100;;


            // mVPaned.AddSubview(mCompileTextView);

            /*

            // Need ActionButtons
            //mExecuteLabel = new Label("Execute");
            mExecuteButton = new NSButton();
            mExecuteButton.Title = "Execute";
            mCompileLabel = new Label("Compile");
            */


            // mVPaned.AddSubview(mCompileButton);
            /*
            mCompileButton.Clicked += CompileButton_Clicked;
            mButtonsHBox = new HBox();
            mButtonsHBox.PackStart(mCompileButton, false, false, 5);
            mButtonsHBox.PackStart(mExecuteButton, false, false, 5);

            mVBox.PackStart(mButtonsHBox, false, false, 0);
            */
            this.AddSubview(sv); // sv
            this.AddSubview(compileView);


            mViewController = new NSSplitViewController();
            mViewController.View = this;
        }
#endif

#if __WINDOWS__

       public string Source { get => editorControl.AvalonEditor.Text;  }

       public EditorPage(TraderBook parent, Data.ScriptFile scriptFile) : base(parent, PageType.Editor)
        {
            TraderBook = parent;
            pageType = PageType.Editor;

            language = scriptFile.Language;

            editorControl = new Controls.EditorControl();
            editorControl.CompileButton.Click += CompileButton_Clicked;
            editorControl.SaveButton.Click += Save;
            editorControl.SaveAsButton.Click += SaveAs;

            editorControl.AvalonEditor.Text = scriptFile.Code;
            this.Children.Add(editorControl);   // 
        }

        public EditorPage(TraderBook parent, string path)
            : base(parent, PageType.Editor)
		{
            TraderBook = parent;
			pageType = PageType.Editor;
            this.path = path;

            language = Path.GetExtension(path) switch
            {
                "cs" => Language.CSharp,
                "fs" => Language.FSharp,
                _ => Language.OpenScript
            };

            editorControl = new Controls.EditorControl();
            editorControl.CompileButton.Click += CompileButton_Clicked;
            editorControl.SaveButton.Click += Save;
            editorControl.SaveAsButton.Click += SaveAs;

            if ( path != null)
                editorControl.AvalonEditor.Text = System.IO.File.ReadAllText(path);
			this.Children.Add( editorControl );	// 
		}
#endif

        void CompileButton_Clicked(object sender, EventArgs e)
        {
            switch (language)
            {
                case Language.OpenScript:
                    CompileOpenScript();
                    break;
                case Language.FSharp:
                    CompileFSharp();
                    break;
                case Language.CSharp:
                    CompileCSharp();
                    break;
            }
        }

        void CompileFSharp()
        {
            // FSharp.Compiler.SourceCodeServices.FSharpChecker.Create();

        }

        void CompileOpenScript()
        {

            editorControl.CompileTextBox.Clear();
            try
            {
                var lex = new OpenCompiler.Lexer(Source);
                lex.Tokenise();
                editorControl.CompileTextBox.Text = lex.GetOutput();

                var compiler = new OpenCompiler.Compiler();
                editorControl.CompileTextBox.Text = compiler.GetOutput();

                compiler.Compile(lex.tokens);
                TraderBook.CreateScript(compiler.Symbols,compiler.Instructions);
                editorControl.CompileTextBox.Text = compiler.GetOutput();
            }
            catch (CompilerException cex)
            {
                editorControl.CompileTextBox.Text = cex.Message + " at line:" + cex.LineNumber + " column:" + cex.ColumnNumber;
            }
            catch (Exception ex)
            {
                editorControl.CompileTextBox.Text = ex.Message;
            }
        }


        void CompileCSharp()
        {
#if __APPLEOS__
            mCompileTextView.TextStorage.SetString(new NSMutableAttributedString(""));
#endif
#if __WINDOWS__
            editorControl.CompileTextBox.Clear();
#endif

            System.CodeDom.Compiler.CompilerParameters parameters = new System.CodeDom.Compiler.CompilerParameters()
                {
                    GenerateExecutable = false,
                    GenerateInMemory = true
                };


#if __APPLEOS__
            mCompileTextView.TextStorage.SetString(new NSMutableAttributedString(""));
#endif
#if __WINDOWS__
            editorControl.CompileTextBox.Clear();
#endif

#if __APPLEOS__
            string source = mTextEditor.String;
#endif
#if __WINDOWS__
            string source = editorControl.AvalonEditor.Text;
#endif

#if __APPLEOS__
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);

            string assemblyName = Path.GetRandomFileName()+".dll";
            var refPaths = new[] {
                typeof(System.Object).GetTypeInfo().Assembly.Location,
                typeof(Console).GetTypeInfo().Assembly.Location,
               typeof(OpenTrader.TraderScript).GetTypeInfo().Assembly.Location,
                Path.Combine(Path.GetDirectoryName(typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly.Location), "System.Runtime.dll"),
                 Path.Combine(Path.GetDirectoryName(typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly.Location), "System.Drawing.Primitives.dll"),
                 Path.Combine(Path.GetDirectoryName(typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly.Location), "System.Drawing.Common.dll")
            };
            MetadataReference[] references = refPaths.Select(r => MetadataReference.CreateFromFile(r)).ToArray();


            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName,
                syntaxTrees: new[] { syntaxTree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            
            using (var ms = new MemoryStream())
            {
                EmitResult result = compilation.Emit(ms);
                string errors = "";
                if (!result.Success)
                {
                    IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                        diagnostic.IsWarningAsError ||
                        diagnostic.Severity == DiagnosticSeverity.Error);

                    foreach (Diagnostic diagnostic in failures)
                    {
                        errors += "Line: "+ (diagnostic.Location.GetMappedLineSpan().StartLinePosition.Line+1) + "\tId" +  diagnostic.Id+"\t: "+diagnostic.GetMessage()+"\r\n";
                    }

                    mCompileTextView.TextStorage.SetString(new NSMutableAttributedString(errors));
                }
                else
                {
                    mCompileTextView.TextStorage.SetString(new NSMutableAttributedString("Compiled OK!"));
                    Assembly assembly = null;
                    try
                    {
                        // assembly = AssemblyLoadContext.Default.LoadFromStream(ms);
                        ms.Seek(0, 0);
                        byte[] data = new byte[ms.Length];
                        ms.Read(data, 0, data.Length);
                        assembly = Assembly.Load(data);
                    }
                    catch (Exception exception)
                    {
                        mCompileTextView.TextStorage.SetString(new NSMutableAttributedString("Error trying to load assembly "+ms.Length+": "+exception.Message));

                    }

                    if (assembly != null)
                    {
                        Type[] types = assembly.GetTypes();
                        Type foundType = null;
                        foreach (Type type in types)
                        {
                            if (type.BaseType.Name == "TraderScript")
                            {
                                TraderBook.SetTypeName(assembly, type.FullName);
                                foundType = type;
                                break;
                            }
                        }
                    }
                }
            }
#endif
#if __WINDOWS__
            parameters.ReferencedAssemblies.Add("System.dll");
			parameters.ReferencedAssemblies.Add("System.Drawing.dll");
            parameters.ReferencedAssemblies.Add("System.Drawing.Common.dll");
            parameters.ReferencedAssemblies.Add("OpenTrader.exe");

            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);

            string assemblyName = Path.GetRandomFileName() + ".dll";
            var refPaths = new[] {
                typeof(System.Object).GetTypeInfo().Assembly.Location,
                typeof(Console).GetTypeInfo().Assembly.Location,
               typeof(OpenTrader.TraderScript).GetTypeInfo().Assembly.Location,
                Path.Combine(Path.GetDirectoryName(typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly.Location), "System.Runtime.dll"),
                 Path.Combine(Path.GetDirectoryName(typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly.Location), "System.Drawing.Common.dll"),
                 Path.Combine(Path.GetDirectoryName(typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly.Location), "System.Drawing.Primitives.dll"),
                 Path.Combine(Path.GetDirectoryName(typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly.Location), "System.Drawing.dll"),
                 Path.Combine(Path.GetDirectoryName(typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly.Location), "System.Collections.dll")
            };
            MetadataReference[] references = refPaths.Select(r => MetadataReference.CreateFromFile(r)).ToArray();

            CSharpCompilation compilation = CSharpCompilation.Create(
                 assemblyName,
                 syntaxTrees: new[] { syntaxTree },
                 references: references,
                 options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));


            using (var ms = new MemoryStream())
            {
                EmitResult result = compilation.Emit(ms);
                string errors = "";
                if (!result.Success)
                {
                    IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                        diagnostic.IsWarningAsError ||
                        diagnostic.Severity == DiagnosticSeverity.Error);

                    foreach (Diagnostic diagnostic in failures)
                    {
                        errors += "Line: " + (diagnostic.Location.GetMappedLineSpan().StartLinePosition.Line + 1) + "\tId" + diagnostic.Id + "\t: " + diagnostic.GetMessage() + "\r\n";
                    }

                    editorControl.CompileTextBox.Text = errors;
                }
                else
                {
                    editorControl.CompileTextBox.Text = "Compiled OK!";
                    Assembly? assembly = null;
                    try
                    {
                        // assembly = AssemblyLoadContext.Default.LoadFromStream(ms);
                        ms.Seek(0, 0);
                        byte[] data = new byte[ms.Length];
                        ms.Read(data, 0, data.Length);
                        assembly = Assembly.Load(data);
                    }
                    catch (Exception exception)
                    {
                        editorControl.CompileTextBox.Text = "Error trying to load assembly " + ms.Length + ": " + exception.Message;

                    }

                    if (assembly != null)
                    {
                        Type[] types = assembly.GetTypes();
                        foreach (Type type in types)
                        {
                            if (type != null && type.BaseType != null && type.BaseType.Name == "TraderScript" && type.FullName != null)
                            {
                                TraderBook.CreateScript(assembly, type.FullName);
                                break;
                            }
                        }
                    }
                }
            }
#endif
        }
    }
}

