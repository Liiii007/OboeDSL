namespace OboeCompiler
{
    public enum TokenType
    {
        None,
        Keyword,      //关键词
        Identifier,   //标识符
        Float,        //浮点形常量
        String,       //字符串常量
        Char,         //字符常量
        Operator,     //运算符
        Semicolon,    //分号
        Comma,        //逗号
        Dot,          //点运算符
        LeftBracket,  //左括号
        RightBracket, //右括号
        LeftSquare,   //左中括号
        RightSquare,  //右中括号
        LeftBrace,    //左大括号
        RightBrace,   //右大括号
        Comment,      //注释
    }

    public enum OperatorType
    {
        None,
        Add,
        Sub,
        Mul,
        Div,
        Mod,
        Assign,
        Equal,
        NotEqual,
        Less,
        LessEqual,
        Greater,
        GreaterEqual,
        LogicalAnd,
        LogicalOr,
        LogicalNot,
        LeftShift,
        RightShift,
        BitwiseAnd,
        BitwiseOr,
        BitwiseXor,
        BitwiseNot,
        Increment,
        Decrement,
    }

    public struct Token
    {
        public TokenType Type;
        public string    Value;

        public Token(TokenType type, string value)
        {
            Type  = type;
            Value = value;
        }

        public int GetIntValue()
        {
            return int.Parse(Value);
        }

        public float GetFloatValue()
        {
            return float.Parse(Value);
        }

        public char GetCharValue()
        {
            return Value[0];
        }

        public string GetStringValue()
        {
            return Value.Substring(1, Value.Length - 2);
        }
    }
}