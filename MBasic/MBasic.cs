using System;
using System.Collections;
using System.Xml;
using System.IO;

namespace MBasic
{
    /*
     * Generic dereferenced value.
     */
    class VarStore
    {
        public Object o;
    }
    /*
     * Environment: the common thing to all the interpretative evaluators.
     * In order to make the compiler simpler, we're resolving variable names in runtine.
     * Please note that it is possible to get rid of hash table lookups here and use, say, arrays
     * for the current environment storage. This approach is quite common within some more complicated
     * closure-based interpreters.
     */
    class Environment
    {
        private Hashtable context = new Hashtable();
        public void add(string name, Object val)
        {
            context[name] = val;
        }
        public Object getVariable(string name)
        {
            return context[name];
        }
        public Object get(string name)
        {
            Object o = context[name];
            if (o is VarStore) return ((VarStore)o).o;
            return o;
        }
        /*
         * This one is a major performance bottleneck.
         * In case of smarter environment handling (yielding a more compilcated compilation stage) this
         * won't be a big deal, really. 
         * 
         * For our educational purposes this solution is pretty much sufficient.
         */
        public Environment clone()
        {
            Environment x = new Environment();
            foreach (string k in context.Keys)
            {
                x.context[k] = context[k];
            }
            return x;
        }
    }

    /*
     * Our "compiler" will produce a runnable Code from the parse tree.
     */
    interface Runnable
    {
        Object Run(Environment e);
    }
    /*
     * Some pre-defined functions are delegates.
     */
    public delegate Object DelegateCode(Object[] args);
    /*
     * To call a "compiled" function we will need an information about its arguments frame.
     * In a more complicated setting arguments + local closure environment passing is defined here.
     */
    class CompiledCode
    {
        public string[] args;
        public Runnable run;
    }

    /*
     * An intermediate parse tree: it can come from any source - a true parser or from the serialized XML, 
     * or whatever else.
     */
    class ParseTree
    {
        public string node;
        public Hashtable attrs;
        public ParseTree[] subnodes;
        public string get(string name)
        {
            if (attrs == null) return null;
            Object o = attrs[name];
            if (o == null) return null;
            return (string)o;
        }
    }

    /*
     * A very short living storage to tell our toplevel evaluator to define a function.
     */
    class Procedure : Runnable
    {
        public string name;
        public CompiledCode value;
        public Object Run(Environment e) { return null; }
    }

    /*
     * XML serialized representation reader, intermediate ParseTree compiler and toplevel evaluator combined.
     */
    class Compiler
    {
        private string[] commas = new string[] { "," };
        private Environment globenv = new Environment();
        private ParseTree last = null;
        public Compiler()
        {
            Init(); // Pre-define some functions.
        }
        private void Pop(Stack nodes, Stack subs)
        {
            ParseTree tr = (ParseTree)(nodes.Pop());
            ArrayList ar = (ArrayList)(subs.Pop());
            if (ar.Count > 0)
                tr.subnodes = (ParseTree[])ar.ToArray(typeof(ParseTree));
            if (subs.Count > 0)
            {
                ArrayList a2 = (ArrayList)(subs.Peek());
                a2.Add(tr);
            }
            last = tr;
        }
        /*
         * Quick and dirty way of reading the whole XML stream into our ParseTree.
         * 
         * We're not using any of the fancy System.Xml.* stuff here, just a raw token reader, in
         * order to demonstrate some useful techniques, which could be applied within a real handcrafted
         * parser as well.
         */
        private ParseTree Read(System.IO.TextReader input)
        {
            XmlTextReader rdr = new XmlTextReader(input);
            ArrayList cde = new ArrayList();
            Stack tnodes = new Stack();
            Stack tsubs = new Stack();

            for (; ; )
            {
                if (!rdr.Read()) break;
                if (rdr.NodeType == XmlNodeType.Element)
                {
                    ParseTree tr = new ParseTree();
                    string nm = rdr.Name;
                    tr.node = nm;
                    bool emp = rdr.IsEmptyElement;
                    tnodes.Push(tr);
                    tsubs.Push(new ArrayList());
                    if (rdr.HasAttributes)
                    {
                        tr.attrs = new Hashtable();
                        int an = rdr.AttributeCount;
                        for (int i = 0; i < an; i++)
                        {
                            rdr.MoveToAttribute(i);
                            tr.attrs[rdr.Name] = rdr.Value;
                        }
                    }
                    if (emp) Pop(tnodes, tsubs);
                }
                else if (rdr.NodeType == XmlNodeType.EndElement)
                {
                    Pop(tnodes, tsubs);
                }
            }
            return last;
        }

        /*
         * Intermediate compiler and toplevel evaluator combined.
         */
        public Object Run(TextReader input)
        {
            ParseTree tree = Read(input);
            Object ret = null;
            foreach (ParseTree p in tree.subnodes)
            {
                Runnable code = Compile(p);
                if (code is Procedure)
                {
                    globenv.add(((Procedure)code).name, ((Procedure)code).value);
                }
                else ret = code.Run(globenv);
            }
            return ret;
        }

        /*
         * Helper function to ease the delegate definition.
         */
        private void AddMethod(string s, DelegateCode c)
        {
            globenv.add(s, c);
        }

        /*
         * Pre-defined delegates.
         */
        private void Init()
        {
            AddMethod("print", delegate(Object[] o)
            {
                Console.WriteLine(o[0]);
                return null;
            });
            AddMethod("eq", delegate(Object[] o)
            {
                return o[0].Equals(o[1]);
            });
            AddMethod("plus", delegate(Object[] o)
            {
                return (Double)(((Double)o[0]) + ((Double)o[1]));
            });
            AddMethod("mul", delegate(Object[] o)
            {
                return (Double)(((Double)o[0]) * ((Double)o[1]));
            });
            /*
             * Do-it-yourself: define some more functions.
             */
        }

        /*
         * Here comes another one ideomatic entity: compiler converting our
         * simple ParseTree structure into a nested Code closure. This approach is
         * very common in the world of functional programming, but, behold, here we're doing
         * the same in an object-oriented setting.
         */
        private Runnable Compile(ParseTree tree)
        {
            switch (tree.node)
            {
                case "begin":
                    {
                        Block b = new Block();
                        foreach (ParseTree pp in tree.subnodes)
                        {
                            b.add(Compile(pp));
                        }
                        b.compile();
                        return b;
                    }
                case "var": return new Var(tree.get("name"));
                case "def": return new DefineVar(tree.get("name"));
                case "label": return new Label(tree.get("name"));
                case "goto": return new GoTo(tree.get("label"));
                case "set": return new AssignVar(tree.get("name"), Compile(tree.subnodes[0]));
                case "number": return new Const(Double.Parse(tree.get("value")));
                case "string": return new Const(tree.get("value"));
                case "if":
                    {
                        int length = tree.subnodes.Length;
                        if (length == 2)
                        {
                            return new If(Compile(tree.subnodes[0]), Compile(tree.subnodes[1]));
                        }
                        else if (length == 3)
                        {
                            return new If(Compile(tree.subnodes[0]), Compile(tree.subnodes[1]), Compile(tree.subnodes[2]));
                        }
                        else throw new Exception();
                    }
                case "function":
                    {
                        CompiledCode c = new CompiledCode();
                        c.args = ((string)(tree.get("args"))).Split(commas, StringSplitOptions.RemoveEmptyEntries);
                        c.run = Compile(tree.subnodes[0]);
                        Procedure p = new Procedure();
                        p.name = tree.get("name");
                        p.value = c;
                        return p;
                    }
                case "call":
                    {
                        if (tree.get("fun") != null)
                        {
                            Call callie = new Call(new Const(globenv.get(tree.get("fun"))));
                            if (tree.subnodes != null)
                                foreach (ParseTree callParameters in tree.subnodes) callie.addarg(Compile(callParameters));
                            callie.compile();
                            return callie;
                        }
                        else
                        {
                            Call cl = new Call(Compile(tree.subnodes[0]));
                            int l = tree.subnodes.Length;
                            for (int i = 1; i < l; i++) cl.addarg(Compile(tree.subnodes[i]));
                            cl.compile();
                            return cl;
                        }
                    }
            }
            return new Const(null); // unknown tag;
        }
    }
}