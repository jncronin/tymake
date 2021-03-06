%namespace tymake_lib

%visibility public
%start file
%partial

%token	EQUALS COLON MUL LPAREN RPAREN AMP PLUS MINUS DOLLARS COMMA NEWLINE FUNC ASSIGN NOT NOTEQUAL LEQUAL GEQUAL LBRACE RBRACE
%token	LBRACK RBRACK DOT LT GT LSHIFT RSHIFT SEMICOLON LOR LAND OR AND APPEND ASSIGNIF NULLCOALESCE	
%token	IF ELSE INCLUDE RULEFOR INPUTS DEPENDS ALWAYS SHELLCMD TYPROJECT SOURCES MKDIR FUNCTION RETURN EXPORT
%token	INTEGER STRING VOID ARRAY OBJECT FUNCREF ANY NULL
%token	FOR FOREACH IN WHILE DO

%left	DOT
	
%union {
		public int intval;
		public string strval;
		public Statement stmtval;
		public Expression exprval;
		public List<Expression> exprlist;
		public tymake_lib.Tokens tokval;
		public Expression.EvalResult.ResultType typeval;
		public FunctionStatement.FunctionArg argval;
		public List<FunctionStatement.FunctionArg> arglistval;
		public List<Expression.EvalResult.ResultType> typelistval;
		internal List<ObjDef> objdeflist;
		internal ObjDef objdefval;
		public bool bval;
	}
	
%token <intval>	INT
%token <strval> STRING LABEL

%type <bval> export
%type <exprval> expr expr1p5 expr2 expr3 expr4 expr5 expr6 expr7 expr7a expr8 expr9 expr10 expr11 strlabelexpr funccall labelexpr labelexpr2 funcrefexpr
%type <stmtval> stmtblock stmtlist stmt stmt2 define ifblock cmd include funcdef forblock foreachblock whileblock doblock
%type <exprlist> exprlist arrayexpr
%type <tokval> assignop
%type <argval> arg
%type <typeval> argtype
%type <arglistval> arglist
%type <typelistval> typelist
%type <objdefval> objmember
%type <objdeflist> objlist objexpr

%%

file		:									{ output = new StatementList(); }			/* empty */
			|	stmtblock						{ output = $1; }
			|	stmtlist						{ output = $1; }
			;

strlabelexpr:	STRING							{ $$ = new StringExpression { val = $1 }; }
			|	labelexpr						{ $$ = $1; }
			;

labelexpr	:	labelexpr2						{ $$ = $1; }
			|	strlabelexpr DOT labelexpr2		{ $$ = new LabelMemberExpression { label = $1, member = $3 }; }
			|	strlabelexpr LBRACK expr RBRACK { $$ = new LabelIndexedExpression { label = $1, index = $3 }; }
			;

labelexpr2	:	LABEL							{ $$ = new LabelExpression { val = $1 }; }
			|	funccall						{ $$ = $1; }
			;

funccall	:	LABEL LPAREN exprlist RPAREN	{ $$ = new FuncCall { target = $1, args = $3 }; }
			;

funcdef		:	FUNCTION LABEL LPAREN arglist RPAREN stmtblock	{ $$ = new FunctionStatement { name = $2, args = $4, code = $6 }; }
			;

stmtblock	:	LBRACE stmtlist RBRACE			{ $$ = $2; }
			|	LBRACE RBRACE					{ $$ = new StatementList(); ((StatementList)$$).list = new List<Statement>(); }
			;

stmtlist	:	stmt							{ StatementList sl = new StatementList(); sl.list = new List<Statement>(); sl.list.Add($1); $$ = sl; }
			|	stmtlist stmt					{ ((StatementList)$1).list.Add($2); $$ = $1; }
			;

stmt		:	stmt2							{ $$ = $1; }
			;

export		:	EXPORT							{ $$ = true; }
			|									{ $$ = false; }
			;


stmt2		:	define SEMICOLON				{ $$ = $1; }
			|	EXPORT define SEMICOLON			{ $$ = $2; $2.export = true; }
			|	ifblock							{ $$ = $1; }
			|	forblock						{ $$ = $1; }
			|	foreachblock					{ $$ = $1; }
			|	whileblock						{ $$ = $1; }
			|	doblock							{ $$ = $1; }
			|	export funcdef					{ $$ = $2; $2.export = $1; }
			|	cmd	SEMICOLON					{ $$ = $1; }
			|	include	SEMICOLON				{ $$ = $1; }
			;

cmd			:	EXPORT LABEL						{ $$ = new ExportStatement { v = $2 }; }
			|	RETURN expr							{ $$ = new ReturnStatement { v = $2 }; }
			|	RETURN								{ $$ = new ReturnStatement { v = new ResultExpression { e = new Expression.EvalResult() } }; }
			|	funccall							{ $$ = new ExpressionStatement { expr = $1 }; }
			|	strlabelexpr DOT funccall			{ $$ = new ExpressionStatement { expr = new LabelMemberExpression { label = $1, member = $3 } }; }
			;

define		:	labelexpr assignop expr				{ $$ = new DefineExprStatement { tok_name = $1, assignop = $2, val = $3 }; }
			;

assignop	:	ASSIGN							{ $$ = Tokens.ASSIGN; }
			|	ASSIGNIF						{ $$ = Tokens.ASSIGNIF; }
			|	APPEND							{ $$ = Tokens.APPEND; }
			;

ifblock		:	IF expr stmtblock					{ $$ = new IfBlockStatement { test = $2, if_block = $3, else_block = null }; }
			|	IF expr stmtblock ELSE stmtblock	{ $$ = new IfBlockStatement { test = $2, if_block = $3, else_block = $5 }; }
			|	IF expr stmtblock ELSE ifblock		{ $$ = new IfBlockStatement { test = $2, if_block = $3, else_block = $5 }; }
			;

forblock	:	FOR LPAREN define SEMICOLON expr SEMICOLON define RPAREN stmtblock	{ $$ = new ForBlockStatement { init = $3, test = $5, incr = $7, code = $9 }; }
			;

foreachblock:	FOREACH LPAREN LABEL IN expr RPAREN	stmtblock	{ $$ = new ForEachBlock { val = $3, enumeration = $5, code = $7 }; }
			;

whileblock	:	WHILE LPAREN expr RPAREN stmtblock	{ $$ = new WhileBlock { test = $3, code = $5 }; }
			;

doblock		:	DO stmtblock WHILE LPAREN expr RPAREN	{ $$ = new DoBlock { test = $5, code = $2 }; }
			;

exprlist	:	exprlist COMMA expr				{ $$ = new List<Expression>($1); $$.Add($3); }
			|	expr							{ $$ = new List<Expression> { $1 }; }
			|									{ $$ = new List<Expression>(); }
			;

include		:	INCLUDE expr					{ $$ = new IncludeStatement { include_file = $2 }; }
			;

expr		:	LPAREN expr RPAREN				{ $$ = $2; }
			|	expr1p5							{ $$ = $1; }
			;

expr1p5		:	expr2 NULLCOALESCE expr1p5		{ $$ = new Expression { a = $1, b = $3, op = Tokens.NULLCOALESCE }; }
			|	expr2							{ $$ = $1; }
			;

expr2		:	expr3 LOR expr2					{ $$ = new Expression { a = $1, b = $3, op = Tokens.LOR }; }
			|	expr3							{ $$ = $1; }
			;

expr3		:	expr4 LAND expr3				{ $$ = new Expression { a = $1, b = $3, op = Tokens.LAND }; }
			|	expr4							{ $$ = $1; }
			;

expr4		:	expr5 OR expr4					{ $$ = new Expression { a = $1, b = $3, op = Tokens.OR }; }
			|	expr5							{ $$ = $1; }
			;

expr5		:	expr6 AND expr5					{ $$ = new Expression { a = $1, b = $3, op = Tokens.AND }; }
			|	expr6							{ $$ = $1; }
			;

expr6		:	expr7 EQUALS expr6				{ $$ = new Expression { a = $1, b = $3, op = Tokens.EQUALS }; }
			|	expr7 NOTEQUAL expr6			{ $$ = new Expression { a = $1, b = $3, op = Tokens.NOTEQUAL }; }
			|	expr7							{ $$ = $1; }
			;

expr7		:	expr7a LT expr7					{ $$ = new Expression { a = $1, b = $3, op = Tokens.LT }; }
			|	expr7a GT expr7					{ $$ = new Expression { a = $1, b = $3, op = Tokens.GT }; }
			|	expr7a LEQUAL expr7				{ $$ = new Expression { a = $1, b = $3, op = Tokens.LEQUAL }; }
			|	expr7a GEQUAL expr7				{ $$ = new Expression { a = $1, b = $3, op = Tokens.GEQUAL }; }
			|	expr7a							{ $$ = $1; }
			;

expr7a		:	expr8 LSHIFT expr7a				{ $$ = new Expression { a = $1, b = $3, op = Tokens.LSHIFT }; }
			|	expr8 RSHIFT expr7a				{ $$ = new Expression { a = $1, b = $3, op = Tokens.RSHIFT }; }
			|	expr8							{ $$ = $1; }
			;

expr8		:	expr9 PLUS expr8				{ $$ = new Expression { a = $1, b = $3, op = Tokens.PLUS }; }
			|	expr9 MINUS expr8				{ $$ = new Expression { a = $1, b = $3, op = Tokens.MINUS }; }
			|	expr9							{ $$ = $1; }
			;

expr9		:	expr10 MUL expr9				{ $$ = new Expression { a = $1, b = $3, op = Tokens.MUL }; }
			|	expr10							{ $$ = $1; }
			;

expr10		:	NOT expr10						{ $$ = new Expression { a = $2, b = null, op = Tokens.NOT }; }
			|	MINUS expr10					{ $$ = new Expression { a = $2, b = null, op = Tokens.MINUS }; }
			|	expr11							{ $$ = $1; }
			;

expr11		:	strlabelexpr					{ $$ = $1; }
			|	INT								{ $$ = new IntExpression { val = $1 }; }
			|	arrayexpr						{ $$ = new ArrayExpression { val = $1 }; }
			|	objexpr							{ $$ = new ObjExpression { val = $1 }; }
			|	funcrefexpr						{ $$ = $1; }
			|	NULL							{ $$ = new NullExpression(); }
			;

funcrefexpr	:	FUNCREF LABEL LPAREN typelist RPAREN	{ $$ = new FunctionRefExpression { name = $2, args = $4 }; }
			|	FUNCTION LPAREN arglist RPAREN stmtblock	{ $$ = new LambdaFunctionExpression { args = $3, code = $5 }; }
			;

typelist	:	typelist COMMA argtype			{ $$ = new List<Expression.EvalResult.ResultType>($1); $$.Add($3); }
			|	argtype							{ $$ = new List<Expression.EvalResult.ResultType>(); $$.Add($1); }
			|									{ $$ = new List<Expression.EvalResult.ResultType>(); }
			;

arrayexpr	:	LBRACK exprlist RBRACK			{ $$ = $2; }
			|	LBRACK exprlist COMMA RBRACK	{ $$ = $2; }
			;
		
arglist		:	arglist COMMA arg				{ $$ = new List<FunctionStatement.FunctionArg>($1); $$.Add($3); }
			|	arg								{ $$ = new List<FunctionStatement.FunctionArg>(); $$.Add($1); }
			|									{ $$ = new List<FunctionStatement.FunctionArg>(); }
			;

objexpr		:	LBRACK objlist RBRACK			{ $$ = $2; }
			|	LBRACK objlist COMMA RBRACK		{ $$ = $2; }
			|	LBRACK ASSIGN RBRACK				{ $$ = new List<ObjDef>(); }
			;

objlist		:	objmember						{ $$ = new List<ObjDef> { $1 }; }
			|	objlist COMMA objmember			{ $1.Add($3); $$ = $1; }
			;

objmember	:	LABEL ASSIGN expr				{ $$ = new ObjDef { name = $1, val = $3 }; }
			;


argtype		:	INTEGER							{ $$ = Expression.EvalResult.ResultType.Int; }
			|	STRING							{ $$ = Expression.EvalResult.ResultType.String; }
			|	ARRAY							{ $$ = Expression.EvalResult.ResultType.Array; }
			|	OBJECT							{ $$ = Expression.EvalResult.ResultType.Object; }
			|	VOID							{ $$ = Expression.EvalResult.ResultType.Void; }
			|	FUNCTION						{ $$ = Expression.EvalResult.ResultType.Function; }
			|	ANY								{ $$ = Expression.EvalResult.ResultType.Any; }
			;

arg			:	argtype LABEL					{ $$ = new FunctionStatement.FunctionArg { name = $2, argtype = $1 }; }
			;

%%

internal Statement output;