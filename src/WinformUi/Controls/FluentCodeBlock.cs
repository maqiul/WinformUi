using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using WinformUi.Theming;

namespace WinformUi.Controls
{
    /// <summary>Languages understood by <see cref="FluentCodeBlock"/>.</summary>
    public enum SyntaxLanguage
    {
        Autodetect,
        Xaml,
        CSharp,
        Plain
    }

    /// <summary>
    /// Read-only, selectable, theme-aware code presenter with regex colorization
    /// for XAML and C# sources and clipboard export.
    /// </summary>
    [ToolboxItem(true)]
    [DefaultProperty("Code")]
    public class FluentCodeBlock : FluentRichTextBox
    {
        private static readonly string[] CSharpKeywords =
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
            "class", "const", "continue", "default", "delegate", "do", "double", "else", "enum",
            "event", "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach",
            "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock", "long",
            "namespace", "new", "null", "object", "operator", "out", "override", "params", "private",
            "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof",
            "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true", "try",
            "typeof", "uint", "ulong", "unsafe", "ushort", "using", "var", "virtual", "void", "volatile", "while"
        };

        private SyntaxLanguage _language = SyntaxLanguage.Autodetect;
        private bool _applying;

        public FluentCodeBlock()
        {
            IsTextSelectionEnabled = true;
            ReadOnly = true;
            WordWrap = false;
            DetectUrls = false;
            Size = new Size(420, 180);
            Font = CodeFont();
        }

        /// <summary>The source to display. Assigning re-runs the colorizer.</summary>
        [Browsable(true)]
        [Localizable(true)]
        [DefaultValue("")]
        public string Code
        {
            get { return base.Text; }
            set { base.Text = value ?? string.Empty; }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public override string Text
        {
            get { return base.Text; }
            set { base.Text = value; }
        }

        [Category("Behavior")]
        [DefaultValue(SyntaxLanguage.Autodetect)]
        public SyntaxLanguage Language
        {
            get { return _language; }
            set
            {
                if (_language != value)
                {
                    _language = value;
                    Highlight();
                }
            }
        }

        /// <summary>Copies the source text to the clipboard, like the reference copy button.</summary>
        public void CopySourceToClipboard()
        {
            if (!string.IsNullOrEmpty(base.Text))
            {
                Clipboard.SetText(base.Text);
            }
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            Highlight();
        }

        protected internal override void ApplyTheme()
        {
            base.ApplyTheme();
            Highlight();
        }

        private SyntaxLanguage EffectiveLanguage()
        {
            if (_language != SyntaxLanguage.Autodetect)
            {
                return _language;
            }

            string text = base.Text;
            if (text == null)
            {
                return SyntaxLanguage.Xaml;
            }

            string trimmed = text.TrimStart();
            return trimmed.StartsWith("<", StringComparison.Ordinal) ? SyntaxLanguage.Xaml : SyntaxLanguage.CSharp;
        }

        private void Highlight()
        {
            if (_applying || !IsHandleCreated)
            {
                return;
            }

            _applying = true;
            try
            {
                RichTextBox inner = InnerRichTextBox;
                string text = inner.Text;
                inner.Select(0, inner.TextLength);
                inner.SelectionColor = DefaultTextColor();
                inner.SelectionFont = Font;

                if (_language != SyntaxLanguage.Plain && text.Length > 0)
                {
                    foreach (Token token in Tokenize(text, EffectiveLanguage()))
                    {
                        inner.Select(token.Start, token.Length);
                        inner.SelectionColor = token.Color;
                    }
                }

                inner.Select(0, 0);
            }
            finally
            {
                _applying = false;
            }
        }

        private Color DefaultTextColor()
        {
            bool dark = ApplicationThemeManager.EffectiveTheme == ApplicationTheme.Dark;
            return dark ? Color.FromArgb(0x9C, 0xDC, 0xFE) /* Aqua */ : Color.FromArgb(0x64, 0x95, 0xED) /* CornflowerBlue */;
        }

        private struct Token
        {
            public Token(int start, int length, Color color)
            {
                Start = start;
                Length = length;
                Color = color;
            }

            public readonly int Start;
            public readonly int Length;
            public readonly Color Color;
        }

        private static Color CommentColor(bool dark)
        {
            return dark ? Color.FromArgb(0x6A, 0x99, 0x59) : Color.FromArgb(0x00, 0x80, 0x00);
        }

        private static Color StringColor(bool dark)
        {
            return dark ? Color.FromArgb(0xD2, 0x6F, 0x6F) : Color.FromArgb(0xA3, 0x15, 0x15);
        }

        private static Color KeywordColor(bool dark)
        {
            return dark ? Color.FromArgb(0x56, 0x9C, 0xD5) : Color.FromArgb(0x00, 0x00, 0xFF);
        }

        private static Color TagColor(bool dark)
        {
            return dark ? Color.FromArgb(0x64, 0x95, 0xED) : Color.FromArgb(0x00, 0x8B, 0x8B);
        }

        private static Color AttributeColor(bool dark)
        {
            return dark ? Color.FromArgb(0x9C, 0xDC, 0xFE) : Color.FromArgb(0xFF, 0x8C, 0x00);
        }

        private static Color NumberColor(bool dark)
        {
            return dark ? Color.FromArgb(0xB5, 0xCE, 0xA8) : Color.FromArgb(0x09, 0x86, 0x1F);
        }

        private static IEnumerable<Token> Tokenize(string text, SyntaxLanguage language)
        {
            bool dark = ApplicationThemeManager.EffectiveTheme == ApplicationTheme.Dark;
            var tokens = new List<Token>();
            if (language == SyntaxLanguage.Xaml)
            {
                AddMatches(tokens, text, @"<!--[\s\S]*?-->|/\*[\s\S]*?\*/", CommentColor(dark));
                AddMatches(tokens, text, @"</?[A-Za-z_][\w.\-]*", TagColor(dark));
                AddMatches(tokens, text, @"""[^""\n]*""|'[^'\n]*'", StringColor(dark));
                AddMatches(tokens, text, @"\b[A-Za-z_][\w\-]*(?==)", AttributeColor(dark));
                AddMatches(tokens, text, @"&[A-Za-z]+;|&#\d+;", KeywordColor(dark));
            }
            else if (language == SyntaxLanguage.CSharp)
            {
                AddMatches(tokens, text, @"//.*|/\*[\s\S]*?\*/", CommentColor(dark));
                AddMatches(tokens, text, @"""(?:\\.|[^""\\])*""|@\\w*""(?:[^""\\]|"""")*""|'(?:\\.|[^'\\])*'", StringColor(dark));
                AddMatches(tokens, text, @"\b\d+(?:\.\d+)?[fFdDmMuUlL]?\b", NumberColor(dark));
                foreach (string keyword in CSharpKeywords)
                {
                    AddMatches(tokens, text, @"\b" + keyword + @"\b", KeywordColor(dark));
                }
            }

            return tokens;
        }

        private static void AddMatches(List<Token> tokens, string text, string pattern, Color color)
        {
            foreach (Match match in Regex.Matches(text, pattern))
            {
                tokens.Add(new Token(match.Index, match.Length, color));
            }
        }

        private static string _codeFamily;

        private static FontFamily ResolveCodeFamily()
        {
            if (_codeFamily != null)
            {
                return _codeFamily == "__default__" ? null : GetFont(_codeFamily);
            }

            using (var installed = new InstalledFontCollection())
            {
                string preferred = null;
                foreach (FontFamily family in installed.Families)
                {
                    string name = family.Name;
                    if (string.Equals(name, "Fira Code", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(name, "Cascadia Code", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(name, "JetBrains Mono", StringComparison.OrdinalIgnoreCase))
                    {
                        preferred = name;
                        break;
                    }

                    if (preferred == null && string.Equals(name, "Consolas", StringComparison.OrdinalIgnoreCase))
                    {
                        preferred = name;
                    }
                }

                _codeFamily = preferred ?? "Courier New";
            }

            return GetFont(_codeFamily);
        }

        private static FontFamily GetFont(string name)
        {
            try
            {
                return new FontFamily(name);
            }
            catch (ArgumentException)
            {
                return FontFamily.GenericMonospace;
            }
        }

        private static Font CodeFont()
        {
            FontFamily family = ResolveCodeFamily() ?? FontFamily.GenericMonospace;
            return new Font(family, 10f, FontStyle.Regular, GraphicsUnit.Point);
        }
    }
}
