using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace MBasic
{
    class Var : Runnable
    {
        private string name;
        public Var(string name)
        {
            this.name = name;
        }
        public Object Run(Environment e)
        {
            return e.get(name);
        }
    }
    /*
     * Another Code type: variable assignment. This code closure will assign the result of running of its contents
     * to the given referential variable.
     */
    class AssignVar : Runnable
    {
        private string variableName;
        private Runnable code;
        public AssignVar(string name, Runnable code)
        {
            variableName = name; this.code = code;
        }
        public Object Run(Environment env)
        {
            Object oo = env.getVariable(variableName);
            Object ret = null;
            if (oo is VarStore)
            {
                ret = code.Run(env);
                ((VarStore)oo).o = ret;
            }
            else throw new Exception();
            return ret;
        }
    }
    /*
     * An infamous goto support: labels in code blocks. It is not actually runnable, it is used to indicate the
     * control passing target.
     */
    class Label : Runnable
    {
        public string name;
        public Label(string n) { name = n; }
        public Object Run(Environment e) { return null; }
    }
    /*
     * Infamous GoTo itself: it does not actually jump anywhere, it passes a special return value to the code block
     * evaluator.
     */
    class GoTo : Runnable
    {
        private string name;
        public GoTo(string n) { name = n; }
        public Object Run(Environment e)
        {
            return new Label(name);
        }
    }
    /*
     * Most complicated thing here: code block evaluator with labels and jumps support.
     * Labels are resolved in compilation time.
     */
    class Block : Runnable
    {
        private ArrayList tmp = new ArrayList();
        private Runnable[] codes;
        private Hashtable labels = null;
        public void add(Runnable c)
        {
            tmp.Add(c);
        }
        public void compile()
        {
            codes = (Runnable[])tmp.ToArray(typeof(Runnable));
            for (int i = 0; i < codes.Length; i++)
            {
                if (codes[i] is Label)
                {
                    if (labels == null) labels = new Hashtable();
                    labels[((Label)codes[i]).name] = i;
                }
            }
            tmp = null;
        }
        public Object Run(Environment e)
        {
            Object ret = null;
            int i = 0;
            for (; i < codes.Length; )
            {
                ret = codes[i].Run(e);
                if (ret is Label)
                {
                    if (labels == null) return ret;
                    Object ii = labels[((Label)ret).name];
                    if (ii == null) return ret; // Propagate goto upwards.
                    i = (int)ii;
                }
                else i++;
            }
            return ret;
        }
    }
    /*
     * Calling a function or a functional value. It is a bit more complicated than necessary, and it is
     * potentially capable of calling lambda values as well, if we decide to extend the language to such a level
     * of power.
     */
    class Call : Runnable
    {
        private Runnable fun;
        private Runnable[] args;
        private ArrayList tmp;
        public Call(Runnable fn)
        {
            tmp = new ArrayList();
            fun = fn;
        }
        public void addarg(Runnable c)
        {
            tmp.Add(c);
        }
        public void compile()
        {
            args = (Runnable[])tmp.ToArray(typeof(Runnable));
            tmp = null;
        }
        public Object Run(Environment e)
        {
            Object fn = fun.Run(e);
            if (fn is DelegateCode)
            {
                Object[] operators = new Object[args.Length];
                for (int i = 0; i < args.Length; i++)
                {
                    operators[i] = args[i].Run(e);
                }
                return ((DelegateCode)fn)(operators);
            }
            if (fn is CompiledCode)
            {
                CompiledCode compiledFunc = ((CompiledCode)fn);
                string[] hs = compiledFunc.args;
                Environment nenv = e.clone();
                if (hs.Length != args.Length) throw new Exception();
                for (int i = 0; i < hs.Length; i++)
                {
                    nenv.add(hs[i], args[i].Run(e));
                }
                return compiledFunc.run.Run(nenv);
            }
            throw new Exception();
        }
    }

    /*
     * Another ideomatic one: closure storing a constant.
     */
    class Const : Runnable
    {
        private Object val;
        public Const(Object value) { val = value; }
        public Object Run(Environment e) { return val; }
    }
    /*
     * Reference variable creation device.
     */
    class DefineVar : Runnable
    {
        private string nm;
        public DefineVar(string n) { nm = n; }
        public Object Run(Environment e)
        {
            e.add(nm, new VarStore());
            return null;
        }
    }
    /*
     * If despatcher: now the language is complete.
     */
    class If : Runnable
    {
        Runnable i, t, f;
        public If(Runnable a, Runnable b)
        {
            i = a; t = b; f = null;
        }
        public If(Runnable a, Runnable b, Runnable c)
        {
            i = a; t = b; f = c;
        }
        public Object Run(Environment e)
        {
            Object oi = i.Run(e);
            if (oi == null || !((oi is Boolean) && (((Boolean)oi))))
            {
                if (f != null)
                {
                    return f.Run(e);
                }
                return null;
            }
            return t.Run(e);
        }
    }
}
