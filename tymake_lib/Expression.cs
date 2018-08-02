﻿/* Copyright (C) 2016-2017 by John Cronin
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:

 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.

 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace tymake_lib
{
    public class Expression : LocationBase
    {
        internal Expression a, b;
        internal Tokens op;

        public virtual EvalResult Evaluate(MakeState s)
        {
            EvalResult ea, eb;

            switch (op)
            {
                case Tokens.NOT:
                    ea = a.Evaluate(s);
                    check_null(ea);
                    if (ea.AsInt == 0)
                        return new EvalResult(1);
                    else
                        return new EvalResult(0);

                case Tokens.LAND:
                    ea = a.Evaluate(s);
                    if (ea.AsInt == 0)
                        return new EvalResult(0);
                    eb = b.Evaluate(s);
                    if (eb.AsInt == 0)
                        return new EvalResult(0);
                    return new EvalResult(1);

                case Tokens.LOR:
                    ea = a.Evaluate(s);
                    if (ea.AsInt != 0)
                        return new EvalResult(1);
                    eb = b.Evaluate(s);
                    if (eb.AsInt != 0)
                        return new EvalResult(1);
                    return new EvalResult(0);

                case Tokens.PLUS:
                    ea = a.Evaluate(s);
                    eb = b.Evaluate(s);

                    check_null(ea);
                    check_null(eb);

                    if (ea.Type == EvalResult.ResultType.Int && eb.Type == EvalResult.ResultType.Int)
                        return new EvalResult(ea.intval + eb.intval);
                    else if (ea.Type == EvalResult.ResultType.String && eb.Type == EvalResult.ResultType.String)
                        return new EvalResult(ea.strval + eb.strval);
                    else if (ea.Type == EvalResult.ResultType.Int && eb.Type == EvalResult.ResultType.String)
                        return new EvalResult(ea.intval.ToString() + eb.strval);
                    else if (ea.Type == EvalResult.ResultType.String && eb.Type == EvalResult.ResultType.Int)
                        return new EvalResult(ea.strval + eb.intval.ToString());
                    else if (ea.Type == EvalResult.ResultType.Array && eb.Type == EvalResult.ResultType.Array)
                    {
                        var nl = new List<EvalResult>(ea.arrval);
                        nl.AddRange(eb.arrval);
                        return new EvalResult(nl);
                    }
                    else if ((ea.Type == EvalResult.ResultType.Void ||
                        ea.Type == EvalResult.ResultType.Undefined) &&
                        (eb.Type == EvalResult.ResultType.Void ||
                        eb.Type == EvalResult.ResultType.Undefined))
                        return new EvalResult();
                    else
                        throw new Statement.SyntaxException("Mismatched arguments to PLUS: " + ea.Type.ToString() + " and " + eb.Type.ToString(), this);

                case Tokens.MUL:
                    ea = a.Evaluate(s);
                    eb = b.Evaluate(s);

                    check_null(ea);
                    check_null(eb);

                    return new EvalResult(ea.AsInt * eb.AsInt);

                case Tokens.MINUS:
                    ea = a.Evaluate(s);

                    check_null(ea);

                    if (b == null)
                    {
                        // unary minus
                        if (ea.Type == EvalResult.ResultType.Void || ea.Type == EvalResult.ResultType.Undefined)
                            return ea;
                        if (ea.Type == EvalResult.ResultType.String)
                            throw new Statement.SyntaxException("Cannot apply unary minus to type string", this);
                        return new EvalResult(0 - ea.intval);
                    }
                    else
                    {
                        eb = b.Evaluate(s);

                        check_null(eb);

                        if (ea.Type == EvalResult.ResultType.String && (eb.Type == EvalResult.ResultType.Int || eb.Type == EvalResult.ResultType.Void || eb.Type == EvalResult.ResultType.Undefined))
                        {
                            long rem_amount = eb.AsInt;
                            if (rem_amount > ea.strval.Length)
                                rem_amount = ea.strval.Length;
                            return new EvalResult(ea.strval.Substring(0, ea.strval.Length - (int)rem_amount));
                        }
                        else if (ea.Type == EvalResult.ResultType.String && eb.Type == EvalResult.ResultType.String)
                        {
                            if (ea.strval.EndsWith(eb.strval))
                                return new EvalResult(ea.strval.Substring(0, ea.strval.Length - eb.strval.Length));
                            else
                                throw new Statement.SyntaxException(ea.strval + " does not end with " + eb.strval, this);
                        }
                        else if ((ea.Type == EvalResult.ResultType.Void ||
                        ea.Type == EvalResult.ResultType.Undefined) &&
                        (eb.Type == EvalResult.ResultType.Void ||
                        eb.Type == EvalResult.ResultType.Undefined))
                        {
                            return new EvalResult();
                        }
                        else
                        {
                            return new EvalResult(ea.AsInt - eb.AsInt);
                        }
                    }

                case Tokens.EQUALS:
                case Tokens.NOTEQUAL:
                    {
                        int _true = 1;
                        int _false = 0;

                        if (op == Tokens.NOTEQUAL)
                        {
                            _true = 0;
                            _false = 1;
                        }

                        ea = a.Evaluate(s);
                        eb = b.Evaluate(s);

                        if (ea.Type == EvalResult.ResultType.String && eb.Type == EvalResult.ResultType.String)
                        {
                            if (ea.strval == null)
                            {
                                if (eb.strval == null)
                                    return new EvalResult(_true);
                                else
                                    return new EvalResult(_false);
                            }
                            if (ea.strval.Equals(eb.strval))
                                return new EvalResult(_true);
                            else
                                return new EvalResult(_false);
                        }
                        else if (ea.Type == EvalResult.ResultType.Null && eb.Type != EvalResult.ResultType.Null)
                            return new EvalResult(_false);
                        else if(ea.Type != EvalResult.ResultType.Null && eb.Type == EvalResult.ResultType.Null)
                            return new EvalResult(_false);
                        else if(ea.Type == EvalResult.ResultType.Null && eb.Type == EvalResult.ResultType.Null)
                            return new EvalResult(_true);
                        {
                            if (ea.AsInt == eb.AsInt)
                                return new EvalResult(_true);
                            else
                                return new EvalResult(_false);
                        }
                    }

                case Tokens.LT:
                    ea = a.Evaluate(s);
                    eb = b.Evaluate(s);

                    check_null(ea);
                    check_null(eb);

                    if (ea.AsInt < eb.AsInt)
                        return new EvalResult(1);
                    else
                        return new EvalResult(0);

                case Tokens.GT:
                    ea = a.Evaluate(s);
                    eb = b.Evaluate(s);

                    check_null(ea);
                    check_null(eb);

                    if (ea.AsInt > eb.AsInt)
                        return new EvalResult(1);
                    else
                        return new EvalResult(0);

                case Tokens.LSHIFT:
                    ea = a.Evaluate(s);
                    eb = b.Evaluate(s);

                    check_null(ea);
                    check_null(eb);

                    return new EvalResult(ea.AsInt << (int)eb.AsInt);

                case Tokens.RSHIFT:
                    ea = a.Evaluate(s);
                    eb = b.Evaluate(s);

                    check_null(ea);
                    check_null(eb);

                    return new EvalResult(ea.AsInt >> (int)eb.AsInt);

                case Tokens.OR:
                    ea = a.Evaluate(s);
                    eb = b.Evaluate(s);

                    check_null(ea);
                    check_null(eb);

                    return new EvalResult(ea.AsInt | eb.AsInt);

                case Tokens.AND:
                    ea = a.Evaluate(s);
                    eb = b.Evaluate(s);

                    check_null(ea);
                    check_null(eb);

                    return new EvalResult(ea.AsInt & eb.AsInt);


            }
                    
            throw new NotImplementedException(op.ToString());
        }

        private void check_null(EvalResult ea)
        {
            if (ea.Type == EvalResult.ResultType.Null)
                throw new Exception("null not allowed in expression");
        }

        public class EvalResult : IEquatable<EvalResult>
        {
            public enum ResultType { Int, String, Void, Function, MakeRule, Object, Array, Any, Null, Undefined };

            public ResultType Type;

            public string strval;
            public long intval;
            public Dictionary<string, EvalResult> objval;
            public FunctionStatement funcval;
            public List<EvalResult> arrval;

            public Expression orig_expr;

            public EvalResult()
            {
                Type = ResultType.Void;
            }
            public EvalResult(long i)
            {
                Type = ResultType.Int;
                intval = i;
            }
            public EvalResult(string s)
            {
                Type = ResultType.String;
                strval = s;
            }
            public EvalResult(FunctionStatement f)
            {
                Type = ResultType.Function;
                funcval = f;
            }
            public EvalResult(Dictionary<string, EvalResult> o)
            {
                Type = ResultType.Object;
                objval = o;
            }
            public EvalResult(IEnumerable<string> sarr)
            {
                Type = ResultType.Array;
                arrval = new List<EvalResult>();
                foreach (string src in sarr)
                    arrval.Add(new EvalResult(src));
            }
            public EvalResult(IEnumerable<int> iarr)
            {
                Type = ResultType.Array;
                arrval = new List<EvalResult>();
                foreach (int src in iarr)
                    arrval.Add(new EvalResult(src));
            }
            public EvalResult(IEnumerable<EvalResult> earr)
            {
                Type = ResultType.Array;
                arrval = new List<EvalResult>(earr);
            }

            public long AsInt
            {
                get
                {
                    switch (Type)
                    {
                        case ResultType.Int:
                            return intval;
                        case ResultType.String:
                            if (strval == null || strval == "")
                                return 0;
                            return 1;
                        case ResultType.Void:
                        case ResultType.Undefined:
                            return 0;
                        case ResultType.Null:
                            return 0;
                        default:
                            throw new NotSupportedException();
                    }
                }
            }

            public static implicit operator Expression(EvalResult er)
            {
                return new ResultExpression { e = er };
            }

            public override string ToString()
            {
                switch (Type)
                {
                    case ResultType.Int:
                        return intval.ToString();
                    case ResultType.String:
                        return "\"" + strval + "\"";
                    case ResultType.Void:
                    case ResultType.Undefined:
                        return "{void}";
                    case ResultType.Array:
                        {
                            StringBuilder sb = new StringBuilder();
                            sb.Append("[ ");
                            for(int i = 0; i < arrval.Count; i++)
                            {
                                if (i != 0)
                                    sb.Append(", ");
                                sb.Append(arrval[i].ToString());
                            }
                            sb.Append(" ]");
                            return sb.ToString();
                        }
                    case ResultType.Null:
                        return "null";
                    case ResultType.Object:
                        {
                            StringBuilder sb = new StringBuilder();
                            sb.Append("[ ");
                            int i = 0;
                            foreach(var kvp in objval)
                            {
                                if (i != 0)
                                    sb.Append(", ");
                                sb.Append(kvp.Key);
                                sb.Append(" = ");
                                sb.Append(kvp.Value.ToString());
                                i++;
                            }
                            sb.Append(" ]");
                            return sb.ToString();
                        }
                    default:
                        throw new NotSupportedException();
                }
            }

            public bool Equals(EvalResult other)
            {
                if (Type != other.Type)
                    return false;
                switch(Type)
                {
                    case ResultType.Array:
                        if (arrval.Count != other.arrval.Count)
                            return false;
                        for (int i = 0; i < arrval.Count; i++)
                        {
                            if (!arrval[i].Equals(other.arrval[i]))
                                return false;
                        }
                        return true;
                    case ResultType.Int:
                        return intval == other.intval;
                    case ResultType.String:
                        return strval == other.strval;
                }
                throw new NotImplementedException();
            }
        }
    }

    internal class ResultExpression : Expression
    {
        public EvalResult e;

        public override EvalResult Evaluate(MakeState s)
        {
            return e;
        }
    }

    public class StringExpression : Expression
    {
        public string val;

        public override EvalResult Evaluate(MakeState s)
        {
            /* First, parse variables in the shell command */
            StringBuilder sb = new StringBuilder();
            int i = 0;
            while (i < val.Length)
            {
                if (val[i] == '\\' && i < (val.Length - 1))
                {
                    sb.Append(val[i++]);
                    sb.Append(val[i++]);
                }
                else if (val[i] == '$')
                {
                    /* Now try and match the largest possible variable we can */
                    int j = i + 1;

                    string match = null;

                    while (j < (val.Length) && val[j] != '$')
                    {
                        string test = val.Substring(i + 1, j - i);

                        if (s.IsDefined(test))
                            match = test;
                        j++;
                    }

                    if (match != null)
                    {
                        sb.Append(s.GetDefine(match).strval);
                        i++;
                        i += match.Length;
                    }
                    else
                        sb.Append(val[i++]);
                }
                else
                    sb.Append(val[i++]);
            }
            return new EvalResult(sb.ToString());
        }
    }

    public class LabelExpression : Expression
    {
        public string val;

        public override EvalResult Evaluate(MakeState s)
        {
            if (s.IsDefined(val))
                return s.GetDefine(val);
            else if (s.funcs.ContainsKey(val))
                return new EvalResult
                {
                    Type = EvalResult.ResultType.Function,
                    funcval = new FunctionStatement
                    {
                        name = val
                    }
                };
            else
                return new EvalResult() { Type = EvalResult.ResultType.Undefined, strval = val, orig_expr = this };
        }

        public override string ToString()
        {
            return val;
        }
    }

    internal class LambdaFunctionExpression : Expression
    {
        public List<FunctionStatement.FunctionArg> args;
        public Statement code;

        public override EvalResult Evaluate(MakeState s)
        {
            FunctionStatement fs = new FunctionStatement();
            fs.name = null;
            fs.args = args;
            fs.code = code;
            return new EvalResult(fs);
        }
    }

    internal class FunctionRefExpression : Expression
    {
        public string name;
        public List<EvalResult.ResultType> args;

        public override EvalResult Evaluate(MakeState s)
        {
            FunctionStatement fs = new FunctionStatement();
            fs.name = name;
            fs.args = new List<FunctionStatement.FunctionArg>();
            foreach (var arg in args)
                fs.args.Add(new FunctionStatement.FunctionArg { argtype = arg });
            var mangled = fs.Mangle();

            if (s.funcs.ContainsKey(mangled))
            {
                fs.args = s.funcs[mangled].args;
                fs.code = s.funcs[mangled].code;
                return new EvalResult(fs);
            }
            else
                return new EvalResult();
        }
    }

    internal class LabelIndexedExpression : Expression
    {
        public Expression label;
        public Expression index;

        public override EvalResult Evaluate(MakeState s)
        {
            EvalResult elabel = label.Evaluate(s);
            EvalResult eindex = index.Evaluate(s);

            switch(elabel.Type)
            {
                case EvalResult.ResultType.String:
                    return new EvalResult(new string(elabel.strval[(int)eindex.AsInt], 1));
                case EvalResult.ResultType.Array:
                    {
                        var idx = eindex.AsInt;
                        if (idx < 0 || idx >= elabel.arrval.Count)
                            return new EvalResult { Type = EvalResult.ResultType.Null };
                        else
                            return elabel.arrval[(int)idx];
                    }
                    
                case EvalResult.ResultType.Object:
                    return elabel.objval[eindex.strval];
                case EvalResult.ResultType.Undefined:
                    throw new Statement.SyntaxException("attempt to access array index of undefined object " + elabel.strval, this);
                default:
                    throw new Statement.SyntaxException("indexing cannot be applied to object of type: " + elabel.Type.ToString(), this);
            }
        }
    }

    internal class LabelMemberExpression : Expression
    {
        public Expression label;
        public Expression member;

        public override EvalResult Evaluate(MakeState s)
        {
            EvalResult elabel = label.Evaluate(s);

            if (elabel.Type == EvalResult.ResultType.Undefined)
                throw new Statement.SyntaxException("attempt to access member of undefined object " + elabel.strval, this);

            if (member is LabelExpression)
            {
                string m = ((LabelExpression)member).val;

                switch (elabel.Type)
                {
                    case EvalResult.ResultType.String:
                        if (m == "length")
                            return new EvalResult(elabel.strval.Length);
                        break;
                    case EvalResult.ResultType.Object:
                        if (elabel.objval.ContainsKey(m))
                            return elabel.objval[m];
                        break;
                    case EvalResult.ResultType.Array:
                        if (m == "length")
                            return new EvalResult(elabel.arrval.Count);
                        break;
                }

                if (m == "type")
                    return new EvalResult(elabel.Type.ToString());
                throw new Statement.SyntaxException("object: " + label.ToString() + " does not contain member " + m.ToString(), this);
            }
            else if (member is FuncCall)
            {
                FuncCall f = member as FuncCall;
                FuncCall fsite = new FuncCall { target = f.target };
                fsite.args = new List<Expression>(f.args);
                fsite.args.Insert(0, label);
                string m = fsite.Mangle(s, out var ers);

                switch (elabel.Type)
                {
                    case EvalResult.ResultType.String:
                        if(m == "5splitss")
                        {
                            string[] split = elabel.strval.Split(new string[] { ers[1].strval }, StringSplitOptions.None);
                            EvalResult[] ret = new EvalResult[split.Length];
                            for (int i = 0; i < split.Length; i++)
                                ret[i] = new EvalResult(split[i]);
                            return new EvalResult(ret);
                        }
                        else if(m == "9substringsi")
                        {
                            return new EvalResult(elabel.strval.Substring((int)ers[1].AsInt));
                        }
                        else if(m == "9substringsii")
                        {
                            return new EvalResult(elabel.strval.Substring((int)ers[1].AsInt, (int)ers[2].AsInt));
                        }
                        break;

                    case EvalResult.ResultType.Array:
                        if (m == "3addai")
                        {
                            elabel.arrval.Add(new EvalResult(ers[1].intval));
                            return new EvalResult();
                        }
                        else if (m == "3addas")
                        {
                            elabel.arrval.Add(new EvalResult(ers[1].strval));
                            return new EvalResult();
                        }
                        else if (m == "3addao")
                        {
                            elabel.arrval.Add(new EvalResult(ers[1].objval));
                            return new EvalResult();
                        }
                        else if (m == "3addaa")
                        {
                            elabel.arrval.Add(new EvalResult(ers[1].arrval));
                            return new EvalResult();
                        }
                        else if (m == "3addav")
                        {
                            elabel.arrval.Add(new EvalResult());
                            return new EvalResult();
                        }
                        else if (m == "3addan")
                        {
                            elabel.arrval.Add(new EvalResult { Type = EvalResult.ResultType.Null });
                            return new EvalResult();
                        }
                        else if (m == "8addrangeaa")
                        {
                            elabel.arrval.AddRange(ers[1].arrval);
                            return new EvalResult();
                        }
                        else if (m == "6insertaii")
                        {
                            elabel.arrval.Insert((int)ers[1].intval, new EvalResult(ers[2].intval));
                            return new EvalResult();
                        }
                        else if (m == "6insertais")
                        {
                            elabel.arrval.Insert((int)ers[1].intval, new EvalResult(ers[2].strval));
                            return new EvalResult();
                        }
                        else if (m == "6insertaio")
                        {
                            elabel.arrval.Insert((int)ers[1].intval, new EvalResult(ers[2].objval));
                            return new EvalResult();
                        }
                        else if (m == "6insertaia")
                        {
                            elabel.arrval.Insert((int)ers[1].intval, new EvalResult(ers[2].arrval));
                            return new EvalResult();
                        }
                        else if (m == "6removeai")
                        {
                            elabel.arrval.RemoveAt((int)ers[1].intval);
                            return new EvalResult();
                        }
                        else if(m == "8containsai" || m == "8containsas")
                        {

                            return new EvalResult(elabel.arrval.Contains(ers[1]) ? 1 : 0);
                        }
                        break;

                    case EvalResult.ResultType.Object:
                        if(m == "11containskeyos")
                        {
                            return new EvalResult(elabel.objval.ContainsKey(ers[1].strval) ? 1 : 0);
                        }
                        else if(m == "3addoss")
                        {
                            elabel.objval[ers[1].strval] = new EvalResult(ers[2].strval);
                            return new EvalResult();
                        }
                        else if(m == "3addoso")
                        {
                            elabel.objval[ers[1].strval] = new EvalResult(ers[2].objval);
                            return new EvalResult();
                        }
                        else if(m == "3getos")
                        {
                            return elabel.objval[ers[1].strval];
                        }
                        if (elabel.objval.ContainsKey(m))
                        {
                            EvalResult feval = elabel.objval[m];
                            if (feval.Type == EvalResult.ResultType.Function)
                            {
                                return feval.funcval.Run(s, ers);                                
                            }
                        }

                        break;
                }

                throw new Statement.SyntaxException("object: " + label.ToString() + " does not contain member " + m.ToString(), this);
            }
            else
                throw new NotSupportedException();
        }
    }

    internal class IntExpression : Expression
    {
        public int val;

        public override EvalResult Evaluate(MakeState s)
        {
            return new EvalResult(val);
        }
    }

    internal class NullExpression : Expression
    {
        public override EvalResult Evaluate(MakeState s)
        {
            return new EvalResult { Type = EvalResult.ResultType.Null };
        }
    }

    internal class ObjectExpression : Expression
    {
        public Dictionary<string, Expression.EvalResult> val =
            new Dictionary<string, EvalResult>();

        public override EvalResult Evaluate(MakeState s)
        {
            return new EvalResult(val);
        }
    }

    internal class ProjectDepends : Expression
    {
        public Expression project;
    }

    internal class ShellCmdDepends : Expression
    {
        public Expression shellcmd;
    }

    public class FuncCall : Expression
    {
        public string target;
        public List<Expression> args;

        public string Mangle(MakeState s, out List<Expression.EvalResult> ers)
        {
            ers = new List<EvalResult>();
            var ert = new List<EvalResult.ResultType>();
            foreach (var arg in args)
            {
                var er = arg.Evaluate(s);
                ers.Add(er);
                ert.Add(er.Type);
            }
            return Mangle(target, ert);
        }

        public static string Mangle(string target, List<Expression.EvalResult.ResultType> args)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(target.Length.ToString());
            sb.Append(target);
            foreach (var e in args)
            {
                switch (e)
                {
                    case EvalResult.ResultType.Int:
                        sb.Append("i");
                        break;
                    case EvalResult.ResultType.String:
                        sb.Append("s");
                        break;
                    case EvalResult.ResultType.Array:
                        sb.Append("a");
                        break;
                    case EvalResult.ResultType.Object:
                        sb.Append("o");
                        break;
                    case EvalResult.ResultType.Void:
                    case EvalResult.ResultType.Undefined:
                        sb.Append("v");
                        break;
                    case EvalResult.ResultType.Null:
                        sb.Append("n");
                        break;
                    case EvalResult.ResultType.Function:
                        sb.Append("f");
                        break;
                    case EvalResult.ResultType.Any:
                        sb.Append("x");
                        break;
                }
            }
            return sb.ToString();
        }

        public override EvalResult Evaluate(MakeState s)
        {
            var mangled_name = Mangle(s, out var args_to_pass);
            if (s.funcs.ContainsKey(mangled_name))
            {
                // add these here because error messages for a function
                //  call should relate to the location the function is
                //  called rather than defined
                s.fcall = this;
                return s.funcs[mangled_name].Run(s, args_to_pass);
            }
            /*List<string> mangle_all = MangleAll(s);
            foreach (var mangled_name in mangle_all)
            {
                if (s.funcs.ContainsKey(mangled_name))
                {
                    List<EvalResult> args_to_pass = new List<EvalResult>();
                    foreach (Expression arg in args)
                        args_to_pass.Add(arg.Evaluate(s));
                    return s.funcs[mangled_name].Run(s, args_to_pass);
                }
            }*/

            throw new Statement.SyntaxException("unable to find function " + Mangle(s, out var resultTypes), this);
        }

        static Dictionary<string, List<string>> mangle_all_cache = new Dictionary<string, List<string>>();

        public static List<string> MangleAll(string target, List<Expression.EvalResult.ResultType> args, MakeState s)
        {
            var ret = new List<string>();
            for(int i = 0; i <= args.Count; i++)
            {
                MangleAllLine(target, i, ret, args);
            }

            return ret;
        }

        private static void MangleAllLine(string target, int cur_idx, List<string> ret,
            List<Expression.EvalResult.ResultType> args_in)
        {
            if (cur_idx == args_in.Count)
            {
                // we've reached the end of a chain of changes - add resultant item
                ret.Add(Mangle(target, args_in));
            }
            else
            {
                if (args_in[cur_idx] == EvalResult.ResultType.Any)
                {
                    // We need to produce new arrays for every possible option at this index
                    // Re-use array because args_in is never changed in MangleAllLine
                    var new_arr = new List<EvalResult.ResultType>(args_in);
                    new_arr[cur_idx] = EvalResult.ResultType.Void;
                    MangleAllLine(target, cur_idx + 1, ret, new_arr);

                    new_arr[cur_idx] = EvalResult.ResultType.String;
                    MangleAllLine(target, cur_idx + 1, ret, new_arr);

                    new_arr[cur_idx] = EvalResult.ResultType.Object;
                    MangleAllLine(target, cur_idx + 1, ret, new_arr);

                    new_arr[cur_idx] = EvalResult.ResultType.Int;
                    MangleAllLine(target, cur_idx + 1, ret, new_arr);

                    new_arr[cur_idx] = EvalResult.ResultType.Array;
                    MangleAllLine(target, cur_idx + 1, ret, new_arr);
                }
                else
                {
                    // pass on to next item
                    MangleAllLine(target, cur_idx + 1, ret, args_in);
                }
            }
        }
    }

    class ArrayExpression : Expression
    {
        public List<Expression> val;

        public override EvalResult Evaluate(MakeState s)
        {
            List<EvalResult> ret = new List<EvalResult>();
            foreach (Expression e in val)
                ret.Add(e.Evaluate(s));
            return new EvalResult(ret);
        }
    }

    class ObjExpression : Expression
    {
        public List<ObjDef> val;

        public override EvalResult Evaluate(MakeState s)
        {
            Dictionary<string, EvalResult> ret = new Dictionary<string, EvalResult>();
            foreach (ObjDef o in val)
                ret[o.name] = o.val.Evaluate(s);
            return new EvalResult(ret);
        }
    }

    class ObjDef
    {
        public string name;
        public Expression val;
    }
}
