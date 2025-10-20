using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace OpenCompiler
{
    internal class Lexer
    {
        int pos;
        string source;
        string[] keywords = { "script", "var", "int", "real", "string", "series","true", "false", "proc", "return", "enum", "pane","candle","pattern"};
        char[] punctuators = { '{', '}', ';', ':', ',','[',']','(',')','\n','\r','.' }; // TODO: add dot
        int lineNumber = 1;
        int columnNumber = 1;

        internal List<Token> tokens = new List<Token>();

        internal List<Token> Tokens
        {
            get { return tokens; }
        }

        internal Lexer(string source)
        {
            this.source = source;
        }

        internal char look
        {
            get
            {
                if (pos>=source.Length)
                {
                    return '\0';
                }
                else
                {
                    return source[pos];
                }
            }
        }

        private void Next()
        {
            pos++;
            columnNumber++;
        }

        private void Rewind(int n=1)
        {
            pos -= n;
            columnNumber -= n;
        }

        internal List<Token> Tokenise()
        {
            tokens.Clear();
            pos = 0;
            lineNumber = 1;
            columnNumber = 1;

            while (pos < source.Length) {
                SkipWhite();
                Token? token;
                if (IsAlpha(look))
                    token = GetKeyWordOrName();
                else if (look == '"')
                    token = GetStringLiteral();
                else if (IsDigit(look))
                    token = GetNumberLiteral();
                else if (IsPunctuator(look))
                    token = GetPunctuator();
                else
                {
                    token = GetOperator();
                }

                if (token != null)
                {
                    tokens.Add(token);
                }
                else
                {
                    throw new CompilerException("Unexpected char",lineNumber,columnNumber-1);
                }
            }

            return tokens;
        }

        private Token? GetOperator()
        {
            Token token = new Token(lineNumber,columnNumber)
            {
                Group = TokenGroup.operation
            };

            switch(look)
            {
                case '+':
                case '-':
                case '*':
                case '/':
                    token.Source = look.ToString();
                    Next();
                    break;
                case '|':
                case '&':
                case '^':
                    token.Source = look.ToString();
                    Next();
                    break;
                case '>':
                case '<':
                case '!':
                case '=':
                    token.Source = look.ToString();
                    Next();
                    if (look == '=')
                    {
                        token.Source += look;
                        Next();
                    }
                    break;
                default:
                    return null;
            }
            return token;
        }

        private Token GetNumberLiteral()
        {
            string number = look.ToString();
            string exponent = "";
            string fraction = "";

            Next();
            if (number == "0" && (look == 'x' || look == 'X'))
            {
                Next();
                while (IsHex(look))
                {
                    number += look.ToString().ToLower();
                    Next();
                }
                Token hexToken = new Token(lineNumber, columnNumber)
                {
                    Source = "0x"+number,
                    Group = TokenGroup.literal
                };
                return hexToken;
            }

            while (IsDigit(look))
            {
                number += look;
                Next();
            }

            if (look == '.')
            {
                Next();
                if (IsDigit(look))
                {
                    while (IsDigit(look))
                    {
                        fraction += look;
                        Next();
                    }
                }
                else
                {
                    Rewind();
                }
            }

            // Get the exponent part
            if (look == 'e' || look == 'E')
            {
                Next();

                exponent = "+";
                if (look=='-')
                {
                    exponent = "-";
                    Next(); 
                }
                else if(look=='+')
                {
                    Next();
                }
                while (IsDigit(look))
                {
                    exponent += look;
                    Next();
                }
            }

            if (fraction != "")
            {
                number += '.'+fraction;
            }

            if (exponent != "")
            {
                number += 'e' + exponent;
            }

            Token token = new Token(lineNumber, columnNumber);
            token.Source = number;
            token.Group = TokenGroup.literal;
            return token;
        }

        private Token GetStringLiteral()
        {
            Next();
            string text = "";
            while (look != '"')
            {
                if (look == '\\')
                {
                    Next();
                    switch (look)
                    {
                        case '\\':
                            text += "'";
                            break;
                        case '"':
                            text += "\""; 
                            break;
                        case 'a':
                            text += "\a";
                            break;
                        case 'b':
                            text += "\b";
                            break;
                        case 'f':
                            text += "\f";
                            break;
                        case 'n':
                            text += "\n";
                            break;
                        case 'r':
                            text += "\r";
                            break;
                        case 't':   
                            text += "\t";
                            break;
                        case 'v':
                            text += "\v";
                            break;
                        default:
                            text += '\\' + look;
                            break;
                    }
                }
                else
                {
                    text += look;
                }
                Next();
            }
            Next();

            Token token = new Token(lineNumber, columnNumber)
            {
                Source = text,
                Group = TokenGroup.literal
            };

            return token;
        }

        private void  SkipWhite()
        {
            while (look == ' ')
            {
                Next();
            }
        }

        private bool IsPunctuator(char c)
        {
            return punctuators.Contains(c);
        }

        private bool IsAlpha(char c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c=='_';
        }

        private bool IsDigit(char c)
        {
            return c >= '0' && c <= '9';
        }

        private bool IsHex(char c)
        {
            return (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
        }

        private Token GetKeyWordOrName()
        {
            string name = "";
            char c= source[pos];
            do
            {
                name += c;
                Next();
                if (pos >= source.Length)
                {
                    break;
                } else
                {
                    c = source[pos];
                }

            } while ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_' || (c >= '0' && c <= '9'));

            Token token = new Token(lineNumber, columnNumber)
            {
                Source = name,
                Group = (name == "true" || name == "false") ? TokenGroup.literal : keywords.Contains(name) ? TokenGroup.keyword : TokenGroup.identifier
            };

            return token;
        }

        private Token GetPunctuator()
        {
            char punctuator = look;
            if (look == '\r')
            {
                Next();
                if (look == '\n')
                {
                    punctuator = '\n';
                }
                else
                {
                    Rewind();
                }
            }

            if (punctuator == '\n') {
                lineNumber++;
                columnNumber = 1;
            }

            Next();
            var token = new Token(lineNumber, columnNumber)
            {
                Group = TokenGroup.punctuator,
                Source = punctuator.ToString()
            };

            return token;
        }


        internal string GetOutput()
        {
            string output = "";
            foreach (var token in tokens)
            {
                output += token.Source + " : " + token.Group switch
                {
                    TokenGroup.literal => "literal",
                    TokenGroup.keyword => "keyword",
                    TokenGroup.identifier => "identifier",
                    TokenGroup.operation => "operator",
                    TokenGroup.punctuator => "punctuator",
                    TokenGroup.none => "none",
                } + " @" + token.Line + "," + token.Column + "\n";
            }

            return output;
        }
    }
}
