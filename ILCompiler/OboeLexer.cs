using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace OboeCompiler
{
    public class OboeLexer
    {
        public static List<string> operators = new List<string>()
        {
            "\\+=", "-=", "\\*=", "/=", "%=", "\\+\\+", "--",
            "==", "!=", "<=", ">=",
            "<<", ">>", "&&", "\\|\\|",
            "\\+", "-", "\\*", "/", "%", "&", "\\|", "^", "~", "!", "<", ">", "="
        };

        public static HashSet<string> operators_set = new HashSet<string>();

        public static List<string> splitter = new List<string>()
        {
            "{", "}", "(", ")", "[", "]", ";", ":",
        };

        public static List<Token> ReguarToken(string input)
        {
            string pattern = "_[a-zA-Z0-9]*|[a-zA-Z][a-zA-Z0-9]*|\".*\"|'.*'";

            //TODO:Add multiple patterns

            foreach (var op in operators)
            {
                pattern += "|" + op;
                operators_set.Add(op);
            }

            foreach (var s in splitter)
            {
                pattern += "|" + s;
            }

            Regex regex   = new Regex(pattern);
            var   matches = regex.Matches(input);

            //TODO:Catalogy

            return new List<Token>();
        }

        public static HashSet<string> Keywords = new HashSet<string>()
        {
            "if",
            "else",
            "while",
            "for",
            "do",
            "switch",
            "case",
            "default",
            "break",
        };

        public static HashSet<char> Operators = new HashSet<char>()
        {
            '+',
            '-',
            '*',
            '/',
            '%',
            '=',
            '<',
            '>',
            '!',
            '&',
            '|',
            '^',
            '~',
            '?',
            ':',
            ';',
            ',',
            '.',
            '[',
            ']',
            '{',
            '}',
        };

        public enum DFAState
        {
            None,
            New,
            Identifier,
            SingleEqual,
            SinglePlus,
            SingleMinus,
            Float,
            FloatDot,
            FloatAfterDot,
            FloatE,
            FloatESignal,
            FloatAfterE,
            FinishOperator,
            FinishSingle,
            SingleSlash,
            OneLineComment,
            MultiLineComment,
            SingleMultiply,
            FinishComment,
            PreFinishMultiComment
        }

        #region Char Checker

        public static bool IsDigit(char c)
        {
            return char.IsDigit(c);
        }

        public static bool IsLetter(char c)
        {
            return char.IsLetter(c);
        }

        public static bool IsLetterOr_(char c)
        {
            return IsLetter(c) || c == '_';
        }

        public static bool IsLetterOr_OrDigit(char c)
        {
            return IsLetterOr_(c) || IsDigit(c);
        }

        public static bool IsOperator(char c, out OperatorType type)
        {
            switch (c)
            {
                case '+':
                    type = OperatorType.Add;
                    return true;
                case '-':
                    type = OperatorType.Sub;
                    return true;
                case '*':
                    type = OperatorType.Mul;
                    return true;
                case '/':
                    type = OperatorType.Div;
                    return true;
                case '%':
                    type = OperatorType.Mod;
                    return true;
                case '=':
                    type = OperatorType.Assign;
                    return true;
                case '!':
                    type = OperatorType.LogicalNot;
                    return true;

                default:
                    type = OperatorType.None;
                    return false;
            }
        }

        #endregion

        public static List<Token> Tokenize(string input)
        {
            List<Token> tokens = new List<Token>();

            int      tokenPos      = 0;
            int      tokenStartPos = 0;
            DFAState state         = DFAState.New;

            while (tokenPos < input.Length && MoveNext(input[tokenPos],
                                                       ref state,
                                                       out var isTokenStart,
                                                       out var isTokenEnd,
                                                       out var tokenType))
            {
                if (isTokenStart)
                {
                    tokenStartPos = tokenPos;
                }

                if (isTokenStart && isTokenEnd)
                {
                    tokenPos++;
                }

                if (isTokenEnd)
                {
                    AddNewToken(input, tokenPos, tokenType, tokens, ref tokenStartPos);
                    state = DFAState.New;
                }
                else
                {
                    tokenPos++;
                }
            }

            MoveNext('\n', ref state, out var _, out var isTokenLastEnd,
                     out var lastTokenType);

            if (isTokenLastEnd)
            {
                AddNewToken(input, tokenPos, lastTokenType, tokens, ref tokenStartPos);
            }

            return tokens;
        }

        public static void AddNewToken(string  input, int tokenPos, TokenType tokenType, List<Token> tokens,
                                       ref int tokenStartPos)
        {
            var tokenString = input.Substring(tokenStartPos, tokenPos - tokenStartPos);
            if (tokenType == TokenType.Identifier && Keywords.Contains(tokenString))
            {
                tokenType = TokenType.Keyword;
            }

            var newToken = new Token(tokenType, tokenString);
            tokens.Add(newToken);
            tokenStartPos = tokenPos;
        }

        public static bool MoveNext(char          c, ref DFAState state, out bool isTokenStart,
                                    out bool      isTokenEnd,
                                    out TokenType tokenType)
        {
            isTokenStart = false;
            isTokenEnd   = false;
            tokenType    = TokenType.None;

            switch (state)
            {
                case DFAState.New:
                    return HandleSingle(c, ref state, out isTokenStart, out isTokenEnd, out tokenType) ||
                           OnStateNew(c, ref state, out isTokenStart);
                case DFAState.Identifier:
                    return OnIdentifier(c, ref state, out isTokenEnd, out tokenType);

                #region Float Handle

                case DFAState.Float:
                    return OnFloat(c, ref state, out isTokenEnd, out tokenType);
                case DFAState.FloatE:
                    return OnFloatE(c, ref state, out isTokenEnd, out tokenType);
                case DFAState.FloatDot:
                    return OnFloatDot(c, ref state, out isTokenEnd, out tokenType);
                case DFAState.FloatAfterDot:
                    return OnFloatAfterDot(c, ref state, out isTokenEnd, out tokenType);
                case DFAState.FloatESignal:
                    return OnFloatESignal(c, ref state, out isTokenEnd, out tokenType);
                case DFAState.FloatAfterE:
                    return OnFloatAfterE(c, ref state, out isTokenEnd, out tokenType);

                #endregion

                #region Single Operators

                case DFAState.SingleEqual:
                    return OnSingleEqual(c, ref state, out isTokenEnd, out tokenType);
                case DFAState.SinglePlus:
                    return OnSinglePlus(c, ref state, out isTokenEnd, out tokenType);
                case DFAState.SingleMinus:
                    return OnSingleMinus(c, ref state, out isTokenEnd, out tokenType);
                case DFAState.SingleMultiply:
                    return OnSingleMultiply(c, ref state, out isTokenEnd, out tokenType);
                case DFAState.SingleSlash:
                    return OnSingleSlash(c, ref state, out isTokenEnd, out tokenType);

                #endregion

                case DFAState.OneLineComment:
                    return OnOneLineComment(c, ref state, out isTokenEnd, out tokenType);
                case DFAState.MultiLineComment:
                    return OnMultiLineComment(c, ref state, out isTokenEnd, out tokenType);
                case DFAState.PreFinishMultiComment:
                    return OnPreMultiLineComment(c, ref state, out isTokenEnd, out tokenType);
                case DFAState.FinishComment:
                    return OnFinishComment(ref state, out isTokenEnd, out tokenType);

                case DFAState.FinishOperator:
                    return OnFinishOperator(ref state, out isTokenEnd, out tokenType);

                default:
                    throw new Exception("No case match");
            }
        }

        private static bool OnFloat(char c, ref DFAState state, out bool isTokenEnd, out TokenType tokenType)
        {
            isTokenEnd = false;
            tokenType  = TokenType.None;

            if (IsDigit(c)) { }
            else if (c == '.')
            {
                state = DFAState.FloatDot;
            }
            else if (c == 'e' || c == 'E')
            {
                state = DFAState.FloatE;
            }
            else
            {
                state      = DFAState.New;
                isTokenEnd = true;
                tokenType  = TokenType.Float;
            }

            return true;
        }

        private static bool OnFloatESignal(char c, ref DFAState state, out bool isTokenEnd, out TokenType tokenType)
        {
            isTokenEnd = false;
            tokenType  = TokenType.None;

            if (IsDigit(c))
            {
                state = DFAState.FloatAfterE;
            }
            else
            {
                return false;
            }

            return true;
        }

        private static bool OnFloatDot(char c, ref DFAState state, out bool isTokenEnd, out TokenType tokenType)
        {
            isTokenEnd = false;
            tokenType  = TokenType.None;

            if (IsDigit(c))
            {
                state = DFAState.FloatAfterDot;
                return true;
            }
            else
            {
                return false;
            }
        }

        private static bool OnFloatE(char c, ref DFAState state, out bool isTokenEnd, out TokenType tokenType)
        {
            isTokenEnd = false;
            tokenType  = TokenType.None;

            if (c == '-' || c == '+')
            {
                state = DFAState.FloatESignal;
                return true;
            }
            else if (IsDigit(c))
            {
                state = DFAState.FloatAfterE;
                return true;
            }
            else
            {
                return false;
            }
        }

        private static bool OnFloatAfterE(char c, ref DFAState state, out bool isTokenEnd, out TokenType tokenType)
        {
            if (IsDigit(c))
            {
                isTokenEnd = false;
                tokenType  = TokenType.None;
            }
            else
            {
                isTokenEnd = true;
                tokenType  = TokenType.Float;
            }

            return true;
        }

        private static bool OnFloatAfterDot(char c, ref DFAState state, out bool isTokenEnd, out TokenType tokenType)
        {
            isTokenEnd = false;
            tokenType  = TokenType.None;

            if (c == 'e' || c == 'E')
            {
                state = DFAState.FloatE;
            }
            else if (IsDigit(c)) { }
            else if (c == '.')
            {
                return false;
            }
            else
            {
                isTokenEnd = true;
                tokenType  = TokenType.Float;
            }

            return true;
        }

        private static bool OnFinishOperator(ref DFAState state, out bool isTokenEnd, out TokenType tokenType)
        {
            isTokenEnd = true;
            state      = DFAState.New;
            tokenType  = TokenType.Operator;
            return true;
        }

        private static bool OnSinglePlus(char c, ref DFAState state, out bool isTokenEnd, out TokenType tokenType)
        {
            isTokenEnd = false;
            tokenType  = TokenType.None;

            if (c == '+')
            {
                state = DFAState.FinishOperator;
            }
            else if (c == '=')
            {
                state = DFAState.FinishOperator;
            }
            else
            {
                isTokenEnd = true;
                tokenType  = TokenType.Operator;
                state      = DFAState.New;
            }

            return true;
        }

        private static bool OnSingleMinus(char c, ref DFAState state, out bool isTokenEnd, out TokenType tokenType)
        {
            isTokenEnd = false;
            tokenType  = TokenType.None;

            if (c == '-')
            {
                state = DFAState.FinishOperator;
            }
            else if (c == '=')
            {
                state = DFAState.FinishOperator;
            }
            else if (IsDigit(c))
            {
                state = DFAState.Float;
            }
            else
            {
                isTokenEnd = true;
                tokenType  = TokenType.Operator;
                state      = DFAState.New;
            }

            return true;
        }

        private static bool OnSingleSlash(char c, ref DFAState state, out bool isTokenEnd, out TokenType tokenType)
        {
            isTokenEnd = false;
            tokenType  = TokenType.None;

            if (c == '=')
            {
                state = DFAState.FinishOperator;
            }
            else if (c == '/')
            {
                state = DFAState.OneLineComment;
            }
            else if (c == '*')
            {
                state = DFAState.MultiLineComment;
            }
            else
            {
                isTokenEnd = true;
                tokenType  = TokenType.Operator;
                state      = DFAState.New;
            }

            return true;
        }

        private static bool OnOneLineComment(char c, ref DFAState state, out bool isTokenEnd, out TokenType tokenType)
        {
            if (c == '\n')
            {
                state      = DFAState.FinishComment;
                isTokenEnd = true;
                tokenType  = TokenType.Comment;
            }
            else
            {
                isTokenEnd = false;
                tokenType  = TokenType.None;
            }

            return true;
        }

        private static bool OnMultiLineComment(char          c, ref DFAState state, out bool isTokenEnd,
                                               out TokenType tokenType)
        {
            if (c == '*')
            {
                state = DFAState.PreFinishMultiComment;
            }

            isTokenEnd = false;
            tokenType  = TokenType.None;

            return true;
        }

        private static bool OnPreMultiLineComment(char          c, ref DFAState state, out bool isTokenEnd,
                                                  out TokenType tokenType)
        {
            isTokenEnd = false;

            if (c == '/')
            {
                tokenType = TokenType.Comment;
                state     = DFAState.FinishComment;
            }
            else if (c == '*')
            {
                tokenType = TokenType.None;
            }
            else
            {
                tokenType = TokenType.None;
                state     = DFAState.MultiLineComment;
            }

            return true;
        }

        private static bool OnFinishComment(ref DFAState state, out bool isTokenEnd, out TokenType tokenType)
        {
            isTokenEnd = true;
            state      = DFAState.New;
            tokenType  = TokenType.Comment;
            return true;
        }

        private static bool OnSingleMultiply(char c, ref DFAState state, out bool isTokenEnd, out TokenType tokenType)
        {
            isTokenEnd = false;
            tokenType  = TokenType.None;

            if (c == '=')
            {
                state = DFAState.FinishOperator;
            }
            else
            {
                isTokenEnd = true;
                tokenType  = TokenType.Operator;
                state      = DFAState.New;
            }

            return true;
        }

        private static bool HandleSingle(char c, ref DFAState state, out bool isTokenStart, out bool isTokenEnd,
                                         out TokenType tokenType)
        {
            isTokenStart = true;
            isTokenEnd   = true;
            tokenType    = TokenType.None;
            state        = DFAState.New;
            switch (c)
            {
                case ';':
                    tokenType = TokenType.Semicolon;
                    break;
                case ',':
                    tokenType = TokenType.Comma;
                    break;
                case '.':
                    tokenType = TokenType.Dot;
                    break;
                case '(':
                    tokenType = TokenType.LeftBracket;
                    break;
                case ')':
                    tokenType = TokenType.RightBracket;
                    break;
                case '{':
                    tokenType = TokenType.LeftBrace;
                    break;
                case '}':
                    tokenType = TokenType.RightBrace;
                    break;
                case '[':
                    tokenType = TokenType.LeftSquare;
                    break;
                case ']':
                    tokenType = TokenType.RightSquare;
                    break;
                default:
                    isTokenStart = false;
                    isTokenEnd   = false;
                    return false;
            }

            return true;
        }

        private static bool OnSingleEqual(char c, ref DFAState state, out bool isTokenEnd, out TokenType tokenType)
        {
            if (c == '=')
            {
                isTokenEnd = false;
                tokenType  = TokenType.None;
                state      = DFAState.FinishOperator;
            }
            else
            {
                isTokenEnd = true;
                tokenType  = TokenType.Operator;
            }

            return true;
        }

        private static bool OnIdentifier(char c, ref DFAState state, out bool isTokenEnd, out TokenType tokenType)
        {
            if (IsLetterOr_OrDigit(c))
            {
                tokenType  = TokenType.None;
                isTokenEnd = false;
            }
            else
            {
                tokenType  = TokenType.Identifier;
                state      = DFAState.New;
                isTokenEnd = true;
            }

            return true;
        }

        public static bool OnStateNew(char c, ref DFAState state, out bool isTokenStart)
        {
            isTokenStart = false;

            if (c == ' ' || c == '\n' || c == '\t' || c == '\r')
            {
                return true;
            }
            else if (IsLetterOr_(c))
            {
                state        = DFAState.Identifier;
                isTokenStart = true;
                return true;
            }
            else if (IsDigit(c))
            {
                state        = DFAState.Float;
                isTokenStart = true;
                return true;
            }

            switch (c)
            {
                case '=':
                    state        = DFAState.SingleEqual;
                    isTokenStart = true;
                    return true;

                case '+':
                    state        = DFAState.SinglePlus;
                    isTokenStart = true;
                    return true;

                case '-':
                    state        = DFAState.SingleMinus;
                    isTokenStart = true;
                    return true;

                case '*':
                    state        = DFAState.SingleMultiply;
                    isTokenStart = true;
                    return true;

                case '/':
                    state        = DFAState.SingleSlash;
                    isTokenStart = true;
                    return true;
            }

            return false;
        }
    }
}