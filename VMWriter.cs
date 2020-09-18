using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace JackCompiler
{
    class VMWriter
    {

        StreamWriter fileWriter;

        public VMWriter(StreamWriter writer)
        {
            fileWriter = writer;
        }

        public void Close()
        {
            fileWriter.Close();
        }

        // push segment index - push temp 4, push local 0, etc..
        public void WritePush(string segment, int index)
        {
            switch(segment)
            {
                case "var":
                    {
                        fileWriter.WriteLine("push " + "local " + index);
                        break;
                    }
                case "arg":
                    {
                        fileWriter.WriteLine("push " + "argument " + index);
                        break;
                    }
                case "field":
                    {
                        fileWriter.WriteLine("push " + "this " + index);
                        break;
                    }
                default:
                    {
                        fileWriter.WriteLine("push " + segment + " " + index);
                        break;
                    }
            }
        }

        public void WritePop(string segment, int index)
        {
            switch (segment)
            {
                case "var":
                    {
                        fileWriter.WriteLine("pop " + "local " + index);
                        break;
                    }
                case "arg":
                    {
                        fileWriter.WriteLine("pop " + "argument " + index);
                        break;
                    }
                case "field":
                    {
                        fileWriter.WriteLine("pop " + "this " + index);
                        break;
                    }
                default:
                    {
                        fileWriter.WriteLine("pop " + segment + " " + index);
                        break;
                    }
            }
        }

        public void WriteArithmetic(string command)
        {
            switch (command)
            {
                case "+":
                    {
                        fileWriter.WriteLine("add");
                        break;
                    }
                case "-":
                    {
                        fileWriter.WriteLine("sub");
                        break;
                    }
                case "*":
                    {
                        WriteCall("Math.multiply", 2);
                        break;
                    }
                case "/":
                    {
                        WriteCall("Math.divide", 2);
                        break;
                    }
                case "&":
                    {
                        fileWriter.WriteLine("and");
                        break;
                    }
                case "|":
                    {
                        fileWriter.WriteLine("or");
                        break;
                    }
                case "<":
                    {
                        fileWriter.WriteLine("lt");
                        break;
                    }
                case ">":
                    {
                        fileWriter.WriteLine("gt");
                        break;
                    }
                case "=":
                    {
                        fileWriter.WriteLine("eq");
                        break;
                    }
                case "neg":
                    {
                        fileWriter.WriteLine("neg");
                        break;
                    }
                case "not":
                    {
                        fileWriter.WriteLine("not");
                        break;
                    }
            }
        }

        public void WriteLabel(string labelName)
        {
            fileWriter.WriteLine("label " + labelName);
        }

        public void WriteGoto(string labelName)
        {
            fileWriter.WriteLine("goto " + labelName);
        }

        public void WriteIf(string labelName)
        {
            fileWriter.WriteLine("if-goto " + labelName);
        }

        public void WriteCall(string name, int numArgs)
        {
            fileWriter.WriteLine("call " + name + " " + numArgs);
        }

        public void WriteFunction(string name, int numLocals)
        {
            fileWriter.WriteLine("function " + name + " " + numLocals);
        }

        public void WriteReturn()
        {
            fileWriter.WriteLine("return");
        }
    }
}
