tymake is an interpreter for a bespoke programming language mainly designed for
writing makefiles for the tysos project (https://github.com/jncronin/tysos)

Building instructions
---------------------

Windows:
Use the provided solution file, e.g. from a Developer Command Prompt simply run
'msbuild' in the tymake base directory.

Linux:
Requires mono.  For Debianesque distributions the following should work:
sudo apt-get install mono-mcs mono-xbuild \
	libmono-system-data-datasetextensions4.0-cil \
	libmono-system-io-compression-filesystem4.0-cil \
	libmono-system-net-http4.0-cil \
	libmono-system-xml-linq4.0-cil

Then run 'xbuild' in the tymake base directory.


Usage
-----

tymake.exe -		
	Run in interactive mode
tymake.exe [command1|file1] [command2|file2] ...
	Execute the commands listed on the command line, for example:
		tymake.exe "DEBUG=1;" "mymakefile.tmk" 
	would first set the variable DEBUG to 1 then source the contents of
	mymakefile.tmk
	
Syntax
------

This section is mostly incomplete.

All statements end in semicolons.
Strings are delimited by double quotes.
A dollar sign ('$') inside a string will replace part of the string with the
content of that variable e.g. GCC="gcc"; CFLAGS="-O2"; CMD="$GCC $CFLAGS -o
output.o output.c"; would be a valid way of describing a build command.

Operators function similar to C with the exception of '+' which can be used to
append strings and integers to strings, e.g. "test" + 1 == "test1".  This leads
to some unexpected behaviour, e.g. 1+2+"test" == "12test" rather than "3test".
If the latter is intended, assign to a temporary first, e.g. a=1+2; a+"test"==
"3test".

?= assigns to a variable if not defined (useful for defaults if not already
provided).

All environment variables are automatically assigned to variables of type
string.  For example, print("$PATH\n"); would display the contents of the
PATH variable.

THIS is a special variable containing the name of the currently executing file.


Four basic data types are recognised: strings, integers, arrays and objects.

Strings are defined as above.

Integers as simple numbers without quotes.

Arrays are untyped and defined with [], e.g.:
	empty_array = [];
	mixed_array = [1, "string", 3];
and can be indexed with postscript zero-based indexing, e.g. mixed_array[1] ==
"string".
Iteration is performed with 'foreach', e.g.
	foreach(val in mixed_array) { print(val); }
Note the requirement for braces around even single statements.
Arrays also have the built-in property 'length', e.g.
	print(mixed_array.length);
	
Objects are simple key-value hashtables and also defined with [], e.g.:
	empty_object = [=];
	nonempty_object = [ name="John", position="Developer", salary=0 ];
with members accessed with the '.' operator, e.g. print(nonempty_object.name);

Other useful commands:

include "filename";	Source the contents of the mentioned file
export define/function expression
					Export the function or variable to a higher scope i.e.
						outside the current function or file (required to set
						variables/define functions in 'included' files)
function funcname(type1 name1, type2 name2, ...)
					Define a function 'funcname' with arguments name1,
						name2 etc of types type1, type2 etc where type can
						be 'int', 'string', 'array' or 'object'.
						Functions can be overloaded based on parameter types.
shellcmd(cmd);		Execute 'cmd' (interpreted as a string) in the shell.

rulefor(target, inputs, depends, funcref);
Generate a makefile rule for target (a string), given inputs (an array),
depends (an array) and funcref (of type funcref).
It target needs to be build, then tymake will ensure that all members of
'inputs' and 'depends' are built first before executing the function referneced
by funcref.

The special character '%' can be used in 'target' and 'inputs' as a wildcard
character.

funcref is either an expression referencing a function, e.g. given
function foo(int a, int b) { return(a+b); }
it can be the expression 'funcref foo(int, int)' or alternatively a function
defined directly in the rulefor.  It is run with several special variables set:
_RULE_OUTPUT		- string containing 'target'
_RULE_INPUTS		- string containing all members of inputs delimited by
						spaces
_RULE_INPUTS		- string containing the first member of inputs
_RULE_DEPENDS		- string containing all members of depends delimited by
						spaces

As stated before, the rule requires that all members of 'inputs' and 'depends'
exist before running the build function.  Thus, the only difference between
these two arrays is which environment variable they are placed in.  Typically,
source files are in 'inputs' and the tools to build them are in 'depends'.

Example rulefor:
rulefor("%.o", [ "%.c" ], [ THIS, CC ], function() {
	shellcmd("$CC -o $_RULE_OUTPUT $CFLAGS -c $_RULE_INPUT");
});

The above is a typical rule to build C files.


build(target);
Force target (and any dependencies) to be built.

if/for/while/do function similarly to C.


