using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace JackCompiler
{
    struct Symbol
    {
        string name;
        string type;
        string kind;
        int index;

        public Symbol(string _name, string _type, string _kind, int _index)
        {
            name = _name;
            type = _type;
            kind = _kind;
            index = _index;
        }

        public string Name { get => name; }
        public string Type { get => type; }
        public string Kind { get => kind; }
        public int Index { get => index;}
    }

    class SymbolTable
    {
        int staticIndex, fieldIndex, intIndex, argIndex, varIndex = 0;

        Hashtable table;

        public SymbolTable()
        {
            StartSubroutine();
        }

        public void StartSubroutine()
        {
            staticIndex = 0;
            fieldIndex = 0;
            argIndex = 0;
            varIndex = 0;

            table = new Hashtable();
        }

        public void Define(string name, string type, string kind)
        {
            Symbol newSymbol;

            switch (kind)
            {
                case "static": // class
                    {
                        newSymbol = new Symbol(name, type, kind, staticIndex);

                        staticIndex++;
                        break;
                    }
                case "field": // class
                    {
                        newSymbol = new Symbol(name, type, kind, fieldIndex);

                        fieldIndex++;
                        break;
                    }
                case "arg": // subroutine
                    {
                        newSymbol = new Symbol(name, type, kind, argIndex);

                        argIndex++;
                        break;
                    }
                case "var": // subroutine
                    {
                        newSymbol = new Symbol(name, type, kind, varIndex);

                        varIndex++;
                        break;
                    }
                default:
                    {
                        return;
                    }
            }

            table.Add(name, newSymbol);
        }

        // gets the current index for the specified Kind
        public int VarCount(string kind)
        {
            switch (kind)
            {
                case "static":
                    {
                        return staticIndex;
                    }
                case "field":
                    {
                        return fieldIndex;
                    }
                case "arg":
                    {
                        return argIndex;
                    }
                case "var":
                    {
                        return varIndex;
                    }
                default:
                    {
                        return 0;
                    }
            }
        }

        //Gets the Symbols Kind - static, field, arg, or var
        public string KindOf(string name)
        {
            if(table.ContainsKey(name))
            {
                Symbol symbol = (Symbol)table[name];

                return symbol.Kind;
            }
            else
            {
                //Console.WriteLine("Not in table: " + name);

                return null;
            }
        }

        //Gets the Symbols Type - int, string, etc..
        public string TypeOf(string name)
        {
            if (table.ContainsKey(name))
            {
                Symbol symbol = (Symbol)table[name];

                return symbol.Type;
            }
            else
            {
                //Console.WriteLine("Not in table: " + name);

                return null;
            }
        }

        //Gets the Symbols index - static 4, etc..
        public int IndexOf(string name)
        {
            if (table.ContainsKey(name))
            {
                Symbol symbol = (Symbol)table[name];

                return symbol.Index;
            }
            else
            {
                //Console.WriteLine("Not in table: " + name);

                return -1;
            }
        }
    }
}
