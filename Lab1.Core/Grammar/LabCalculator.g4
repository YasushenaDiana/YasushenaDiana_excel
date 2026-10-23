grammar LabCalculator;

compileUnit : expression EOF ;

expression
    : LPAREN expression RPAREN                                                   #ParenthesizedExpr
    | <assoc=right> expression EXPONENT expression                               #ExponentialExpr
    | operatorToken=(ADD | SUBTRACT) expression                                  #UnaryExpr
    | expression operatorToken=(MULTIPLY | DIVIDE | MOD | DIV) expression        #MultiplicativeExpr
    | expression operatorToken=(ADD | SUBTRACT) expression                       #AdditiveExpr
    | operatorToken=(MOD | DIV) LPAREN expression COMMA expression RPAREN        #ModDivFunctionExpr
    | NUMBER                                                                     #NumberExpr
    | CELL                                                                       #CellExpr
    ;

NUMBER : [0-9]+ ;

MOD : [mM][oO][dD] ;
DIV : [dD][iI][vV] ;

CELL : [a-zA-Z]+ [0-9]+ ;

EXPONENT : '^' ;
MULTIPLY : '*' ;
DIVIDE : '/' ;
ADD : '+' ;
SUBTRACT : '-' ;
LPAREN : '(' ;
RPAREN : ')' ;
COMMA : ',' ;

WS : [ \t\r\n]+ -> skip ;
