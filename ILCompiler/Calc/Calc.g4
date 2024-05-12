grammar Calc;

prog: EMPTY* ((assign';' | ifstatement | whilestatement | forstatement) EMPTY*)*;

assign: id EMPTY* '='EMPTY* mathexpr EMPTY*
    |   id EMPTY* '+='EMPTY* id EMPTY*
    |   id EMPTY* '-='EMPTY* id EMPTY*
    |   id EMPTY* '*='EMPTY* id EMPTY*
    |   id EMPTY* '/='EMPTY* id EMPTY*
    |   id'++'
    |   id'--'
    ;

ifstatement: 'if' EMPTY* '('EMPTY* boolexpr EMPTY*')'EMPTY*'{' prog '}' elsestatement?;
    
elsestatement: EMPTY* 'else' EMPTY* '{' prog '}';

whilestatement: 'while' EMPTY* '('EMPTY* boolexpr EMPTY*')'EMPTY*'{' prog '}';

forstatement: 'for' EMPTY* '('EMPTY* (assign)? EMPTY* ';'EMPTY* boolexpr EMPTY* ';' EMPTY* assign EMPTY*')'EMPTY*'{'prog'}';
    
boolexpr: mathexpr EMPTY*('=='|'!='|'>'|'<'|'>='|'<=')EMPTY* mathexpr
    | boolexpr ('&&'|'||') boolexpr
    | '(' boolexpr ')'
    | '!' boolexpr;

mathexpr: mathfunc
    | mathexpr ('*'|'/') mathexpr
    | mathexpr ('+'|'-') mathexpr
    | value
    | '(' mathexpr ')'
    | id
    ;
    
value: INT;
id: varid | memberid;
varid: ID(EMPTY* '.' EMPTY* ID)?;
memberid : '$' ID EMPTY* '.' EMPTY* ID;
mathfunc: MATHFUNC '(' (mathexpr|MATHFUNC '(' mathexpr ')') ')';

MATHFUNC: 'sin' | 'cos' | 'tan' | 'sqrt' | 'lg2' | 'lg10' | 'ln' | 'degree' | 'radian' | 'abs' | 'pow';
ID: [a-zA-Z_][a-zA-Z_0-9]*;
INT: [0-9]+;
EMPTY: [ \t\n\r]+ -> channel(HIDDEN);