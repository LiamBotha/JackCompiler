using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Net.Http.Headers;
using System.Reflection.Metadata.Ecma335;
using System.Text.RegularExpressions;
using System.Xml;

namespace JackCompiler
{
    class Program
    {
        struct Token
        {
            public string text;
            public string type;

            public Token(string _text, string _type)
            {
                text = _text;
                type = _type;
            }
        }

        static List<string> keywords = new List<string>()
        {
            "class", "constructor", "function", "method", "field", "static",
            "var","int", "char", "boolean", "void", "true", "false", "null",
            "this","let", "do", "if", "else", "while", "return"
        };

        static List<string> symbols = new List<string>()
        {
            "{", "}", "(", ")", "[", "]", ".",
            ",", ";", "+", "-", "*", "/", "&",
            "|", "<", ">", "=", "~"
        };

        static int tokenIndex = 0;
        static int labelIndex = 0;

        static SymbolTable classTable = new SymbolTable();
        static SymbolTable subroutineTable = new SymbolTable();

        static string className = "";

        static StreamWriter fileWriter;
        static VMWriter vmWriter;

        static int Main(string[] args)
        {
            string filepath = Console.ReadLine();
            string outpath = "";

            if(Directory.Exists(filepath))
            {
                var files = Directory.GetFiles(filepath, "*.jack");

                foreach (var file in files)
                {
                    var tokens = Tokenizer(file);

                    tokenIndex = 0;

                    CompilationEngine(tokens, file.Replace(".jack", "T.xml"));
                }
            }
            else if(File.Exists(filepath))
            {
                var tokens = Tokenizer(filepath);

                outpath = filepath;

                tokenIndex = 0;

                CompilationEngine(tokens, outpath.Replace(".jack", "T.xml"));
            }
            else
            {
                Console.WriteLine("File Not Found!");

                return 1;
            }

            return 0;
        }

        private static List<Token> Tokenizer(string filepath)
        {
            List<Token> tokens = new List<Token>(); // neither val can be used as key rather struct list maybe?

            bool isComment = false;

            using (StreamReader reader = new StreamReader(filepath))
            {
                string line = "";
                while ((line = reader.ReadLine()) != null)
                {
                    // Comment removal
                    
                    if(line.Contains("/*") && line.Contains("*/"))
                    {
                        var startIndex = line.IndexOf("/*");
                        var endIndex = line.IndexOf("*/") + 2;

                        line = line.Remove(startIndex, endIndex - startIndex);
                    }
                    else if(line.Contains("/*"))
                    {
                        isComment = true;

                        line = line.Split("/*")[0];
                    }

                    if (isComment && line.Contains("*/"))
                    {
                        line = line.Split("*/")[1];

                        isComment = false;
                    }
                    else if(isComment)
                    {
                        continue;
                    }

                    if (line.StartsWith("//") || line == "")
                        continue;

                    line = line.Split("//")[0];

                    // Parsing

                    string token = "";

                    Console.WriteLine(">>" + line);

                    for (int i = 0; i < line.Length; i++)
                    {
                        string curr = line[i].ToString();

                        // String
                        if (token.StartsWith('"'))
                        {
                            if (curr != "\"")
                            {
                                token += curr;
                            }
                            else
                            {
                                token += curr;

                                tokens.Add(new Token(token.Replace("\"", ""), "stringConstant"));

                                token = "";
                            }

                            continue;
                        }

                        token = token.Trim();

                        // String
                        if (curr == " " && (token == "" || token == " "))
                        {
                            continue;
                        }
                        else if(curr == " ") // didn't get recognised and has space after so its a variable
                        {
                            if (Int32.TryParse(token, out int val))
                            {
                                tokens.Add(new Token(token, "integerConstant"));
                            }
                            else if (keywords.Contains(token))// exists in keywords so its a keyword
                            {
                                tokens.Add(new Token(token, "keyword"));
                            }
                            else
                            {
                                tokens.Add(new Token(token, "identifier"));
                            }

                            token = "";
                        }
                        else if (symbols.Contains(curr)) // cur is a symbol 
                        {
                            if (Int32.TryParse(token, out int val))
                            {
                                tokens.Add(new Token(token, "integerConstant"));
                            }
                            else if(keywords.Contains(token)) //
                            {
                                tokens.Add(new Token(token, "keyword"));
                            }
                            else if (token != "")
                            {
                                tokens.Add(new Token(token, "identifier"));
                            }

                            tokens.Add(new Token(curr, "symbol"));

                            token = "";
                        }
                        else if (curr != " ")
                        {
                            token += curr;

                            //if (keywords.Contains(token))// exists in keywords so its a keyword
                            //{
                            //    tokens.Add(new Token(token, "keyword"));

                            //    token = "";
                            //}
                        }
                    }
                }
            }

            Console.WriteLine("----------------------------------------------------------------------");

            //foreach (var token in tokens)
            //{
            //    Console.WriteLine(token.text + ", " + token.type);
            //}

            return tokens;
        }

        private static void CompilationEngine(List<Token> tokens, string filepath)
        {
            // generate file for each base file

            XmlWriterSettings xmlWriterSettings = new XmlWriterSettings()
            {
                Indent = true,
                IndentChars = "\t",
                NewLineOnAttributes = true,
                OmitXmlDeclaration = true
            };

            fileWriter = new StreamWriter(filepath.Replace("T.xml", ".vm"));
            vmWriter = new VMWriter(fileWriter);

            using (XmlWriter writer = XmlWriter.Create(filepath, xmlWriterSettings))
            {
                writer.WriteStartDocument();

                //writer.WriteStartElement("tokens");

                if(tokens[0].text == "class")
                {
                    CompileClass(writer,tokens);
                }

                //writer.WriteEndElement();

                writer.WriteEndDocument();
            }

            fileWriter.Close();
        }

        private static void WriteElement(XmlWriter writer, List<Token> tokens, string elementName = "")
        {
            if(elementName != "")
            {
                writer.WriteStartElement(elementName);
            }
            else
            {
                writer.WriteStartElement(tokens[tokenIndex].type);
            }

            writer.WriteString(" " + tokens[tokenIndex].text + " ");
            writer.WriteEndElement();

            tokenIndex++;
        }

        private static void CompileClass(XmlWriter writer, List<Token> tokens)
        {
            // Reset symbol table for every class
            classTable.StartSubroutine();

            // Root
            writer.WriteStartElement("class");

            // class
            WriteElement(writer, tokens);

            // className
            className = tokens[tokenIndex].text;
            WriteElement(writer, tokens, "class");

            fileWriter.WriteLine("// Class "  + className);

            // {
            WriteElement(writer, tokens);

            while (tokens[tokenIndex].text == "static" || tokens[tokenIndex].text == "field")
            {
                CompileClassVarDec(writer, tokens);
            }

            while (tokens[tokenIndex].text == "constructor" || tokens[tokenIndex].text == "function" || tokens[tokenIndex].text == "method")
            {
                CompileSubroutine(writer, tokens);
            }

            // }
            WriteElement(writer, tokens);

            writer.WriteEndElement();

            vmWriter.Close();
        }

        private static void CompileClassVarDec(XmlWriter writer, List<Token> tokens)
        {
            //Start Root
            writer.WriteStartElement("classVarDec");

            //Static or field
            string kind = tokens[tokenIndex].text;
            WriteElement(writer, tokens);

            //Type
            string type = tokens[tokenIndex].text;
            WriteElement(writer, tokens);

            //identifier / name
            string name = tokens[tokenIndex].text;
            WriteElement(writer, tokens, kind + "_" + classTable.VarCount(kind));

            //Add new symbol to class table
            classTable.Define(name, type, kind);

            while (tokens[tokenIndex].text == ",")
            {
                // ,
                WriteElement(writer, tokens);

                // varName
                name = tokens[tokenIndex].text;
                WriteElement(writer, tokens, kind + "_" + classTable.VarCount(kind));

                //Add new symbol to class table
                classTable.Define(name, type, kind);
            }

            // ;
            WriteElement(writer, tokens);

            //End Root
            writer.WriteEndElement();
        }

        private static void CompileSubroutine(XmlWriter writer, List<Token> tokens)
        {
            subroutineTable.StartSubroutine();

            //Start Root
            writer.WriteStartElement("subroutineDec");

            // func, method, or ctor
            string routineType = tokens[tokenIndex].text;
            WriteElement(writer, tokens);

            // void or type
            string type = tokens[tokenIndex].text;
            if(type != "void")
            {
                WriteElement(writer, tokens,"class_used");
            }
            else
            {
                WriteElement(writer, tokens);
            }

            // identifier / name
            string name = tokens[tokenIndex].text;
            WriteElement(writer, tokens, "subroutine_defined");

            fileWriter.WriteLine("// " + routineType + " " + type + " " + name);

            if(routineType == "method")
            {
                subroutineTable.Define("this", className, "arg");
            }

            // parameterList
            ComposeParameterList(writer, tokens);

            writer.WriteStartElement("subroutineBody");

            // {
            WriteElement(writer, tokens);

            int numLocalVar = 0;
            while (tokens[tokenIndex].text == "var")
            {
                numLocalVar += CompileVarDec(writer, tokens);
            }

            vmWriter.WriteFunction(className + "." + name, numLocalVar);

            if (routineType == "constructor")
            {
                vmWriter.WritePush("constant", classTable.VarCount("field")); // push num of class vars to stack
                vmWriter.WriteCall("Memory.alloc", 1); // creates memory for the variables
                vmWriter.WritePop("pointer", 0); // pushes the base address to the this segment making it the current object
            }
            else if(routineType == "method")
            {
                vmWriter.WritePush("argument", 0);
                vmWriter.WritePop("pointer", 0);
            }

            CompileStatements(writer, tokens);

            // }
            WriteElement(writer, tokens);

            writer.WriteEndElement();

            //End Root
            writer.WriteEndElement();
        }

        private static void CompileStatements(XmlWriter writer, List<Token> tokens)
        {
            writer.WriteStartElement("statements");

            bool statement = true;

            while (statement)
            {
                //Console.WriteLine(tokens[tokenIndex].text + ", tokenIndex: " + tokenIndex);

                switch(tokens[tokenIndex].text)
                {
                    case "let":
                        {
                            CompileLet(writer, tokens);
                            statement = true;
                            break;
                        }
                    case "if":
                        {
                            CompileIf(writer, tokens);
                            statement = true;
                            break;
                        }
                    case "while":
                        {
                            CompileWhile(writer, tokens);
                            statement = true;
                            break;
                        }
                    case "do":
                        {
                            CompileDo(writer, tokens);
                            statement = true;
                            break;
                        }
                    case "return":
                        {
                            CompileReturn(writer, tokens);
                            statement = true;
                            break;
                        }
                    default:
                        {
                            //Console.WriteLine(tokens[tokenIndex].text + ", tokenIndex: " + tokenIndex);
                            statement = false;
                            break;
                        }
                }
            }

            writer.WriteEndElement();
        }

        private static void CompileReturn(XmlWriter writer, List<Token> tokens)
        {
            writer.WriteStartElement("returnStatement");

            // return
            WriteElement(writer, tokens);

            fileWriter.WriteLine("// return"); 

            if (tokens[tokenIndex].text != ";")
            {
                CompileExpression(writer, tokens);
            }
            else
            {
                vmWriter.WritePush("constant", 0);
            }

            vmWriter.WriteReturn();

            // ;
            WriteElement(writer, tokens);

            writer.WriteEndElement();
        }

        private static void CompileDo(XmlWriter writer, List<Token> tokens)
        {
            writer.WriteStartElement("doStatement");

            // do
            WriteElement(writer, tokens);

            if (tokens[tokenIndex + 1].text == "(") // do method(expression)
            {
                // subroutineName
                string subroutineName = tokens[tokenIndex].text;
                WriteElement(writer, tokens, "subroutine_used");

                // (  
                WriteElement(writer, tokens);

                vmWriter.WritePush("pointer", 0);

                int numArgs = CompileExpressionList(writer, tokens);

                fileWriter.WriteLine("// do " + subroutineName);

                vmWriter.WriteCall(className + "." + subroutineName, numArgs + 1);

                vmWriter.WritePop("temp", 1);

                // )
                WriteElement(writer, tokens);
            }
            else // do var.method() or do class.method()
            {
                // varName or className
                //Check if in class or subtable  else its a class name
                string name = tokens[tokenIndex].text;
                bool isClass = false;
                string kind = "";
                string type = "";
                int index = -1;

                if (subroutineTable.IndexOf(name) != -1)
                {
                    kind = subroutineTable.KindOf(name);
                    type = subroutineTable.TypeOf(name);
                    index = subroutineTable.IndexOf(name);
                    WriteElement(writer, tokens, kind + "_" + index + "_used");
                }
                else if (classTable.IndexOf(name) != -1)
                {
                    kind = classTable.KindOf(name);
                    type = classTable.TypeOf(name);
                    index = classTable.IndexOf(name);
                    WriteElement(writer, tokens, classTable.KindOf(name) + "_" + classTable.IndexOf(name) + "_used");
                }
                else
                {
                    isClass = true;
                    WriteElement(writer, tokens, "class_used");
                }

                // .
                WriteElement(writer, tokens);

                // subroutineName
                string subroutineName = tokens[tokenIndex].text;
                WriteElement(writer, tokens, "subroutine_used");

                fileWriter.WriteLine("// do " + name + "." + subroutineName);

                // (
                WriteElement(writer, tokens);

                if (!isClass)
                {
                    vmWriter.WritePush(kind, index);
                }

                int numArgs = CompileExpressionList(writer, tokens);

                if (!isClass)
                {
                    vmWriter.WriteCall(type + "." + subroutineName, numArgs + 1);
                }
                else
                {
                    vmWriter.WriteCall(name + "." + subroutineName, numArgs);
                }

                vmWriter.WritePop("temp", 1);

                // )
                WriteElement(writer, tokens);
            }

            // ;
            WriteElement(writer, tokens);

            //fileWriter.Write(Environment.NewLine);

            writer.WriteEndElement();
        }

        private static void CompileWhile(XmlWriter writer, List<Token> tokens)
        {
            writer.WriteStartElement("whileStatement");

            string whileLabel = "label_" + labelIndex++;
            string endWhileLabel = "label_" + labelIndex++;

            vmWriter.WriteLabel(whileLabel);

            // while
            WriteElement(writer, tokens);

            // (
            WriteElement(writer, tokens);

            CompileExpression(writer, tokens);

            // Inverts condition and if true goto exit
            vmWriter.WriteArithmetic("not");
            vmWriter.WriteIf(endWhileLabel);

            // )
            WriteElement(writer, tokens);

            // {
            WriteElement(writer, tokens);

            CompileStatements(writer, tokens);

            // }
            WriteElement(writer, tokens);

            vmWriter.WriteGoto(whileLabel);

            vmWriter.WriteLabel(endWhileLabel);

            writer.WriteEndElement();
        }

        private static void CompileIf(XmlWriter writer, List<Token> tokens)
        {
            writer.WriteStartElement("ifStatement");

            string elseLabel = "label_" + labelIndex++;
            string endIfLabel = "label_" + labelIndex++;

            fileWriter.WriteLine("// if statement");

            // if
            WriteElement(writer, tokens);

            // (
            WriteElement(writer, tokens);

            CompileExpression(writer, tokens);

            // inverts consition and skips if block when inverse condition is true
            vmWriter.WriteArithmetic("not");
            vmWriter.WriteIf(elseLabel);

            // )
            WriteElement(writer, tokens);

            // {
            WriteElement(writer, tokens);

            //Console.WriteLine(tokens[tokenIndex].text);
            CompileStatements(writer, tokens);

            // }
            WriteElement(writer, tokens);

            // finished if skip to after else
            vmWriter.WriteGoto(endIfLabel);
            // else to skip to if condition is false
            vmWriter.WriteLabel(elseLabel);

            if (tokens[tokenIndex].text == "else")
            {

                // else
                WriteElement(writer, tokens);

                // {
                WriteElement(writer, tokens);

                CompileStatements(writer, tokens);

                // }
                WriteElement(writer, tokens);
            }

            // after if completes it skips to here
            vmWriter.WriteLabel(endIfLabel);

            writer.WriteEndElement();
        }

        private static void CompileLet(XmlWriter writer, List<Token> tokens)
        {
            bool isArray = false;

            writer.WriteStartElement("letStatement");

            // let
            WriteElement(writer, tokens);

            // varName
            string name = tokens[tokenIndex].text;
            string kind = "";
            int index = -1;

            if (subroutineTable.IndexOf(name) != -1)
            {
                kind = subroutineTable.KindOf(name);
                index = subroutineTable.IndexOf(name);
                WriteElement(writer, tokens, subroutineTable.KindOf(name) + "_" + subroutineTable.IndexOf(name) + "_used");
            }
            else if (classTable.IndexOf(name) != -1)
            {
                kind = subroutineTable.KindOf(name);
                index = subroutineTable.IndexOf(name);
                WriteElement(writer, tokens, classTable.KindOf(name) + "_" + classTable.IndexOf(name) + "_used");
            }
            else
            {
                WriteElement(writer, tokens, "var_MissingNo_used");
            }

            // [ + Expression +  ] 
            if (tokens[tokenIndex].text == "[")
            {
                isArray = true;

                // [
                WriteElement(writer, tokens);

                vmWriter.WritePush(kind, index);

                CompileExpression(writer, tokens);

                vmWriter.WriteArithmetic("+");

                // ]
                WriteElement(writer, tokens);
            }

            // =
            WriteElement(writer, tokens);

            // expression
            CompileExpression(writer, tokens);

            if(isArray) //var[i]
            {
                vmWriter.WritePop("temp", 1); // stores temp val
                vmWriter.WritePop("pointer", 1); // sets var[i] to the current array int in that
                vmWriter.WritePush("temp", 1); // gets the value to push to the array
                vmWriter.WritePop("that", 0); // puts the value into the array at the that segment which is var[i]
            }
            else if(subroutineTable.IndexOf(name) != -1) // local var
            {
                vmWriter.WritePop(subroutineTable.KindOf(name), subroutineTable.IndexOf(name));
            }
            else if(classTable.IndexOf(name) != -1) // class var
            {
                vmWriter.WritePop(classTable.KindOf(name), classTable.IndexOf(name));
            }

            // ;
            WriteElement(writer, tokens);

            writer.WriteEndElement();
        }

        private static void CompileExpression(XmlWriter writer, List<Token> tokens)
        {
            List<string> ops = new List<string>() { "+", "-", "*", "/", "&", "|", "<", ">", "=" };

            writer.WriteStartElement("expression");

            CompileTerm(writer, tokens);

            while (ops.Contains(tokens[tokenIndex].text))
            {
                // op
                string op = tokens[tokenIndex].text;
                WriteElement(writer, tokens);

                CompileTerm(writer, tokens);

                vmWriter.WriteArithmetic(op);
            }

            writer.WriteEndElement();
        }

        private static int CompileExpressionList(XmlWriter writer, List<Token> tokens)
        {
            writer.WriteStartElement("expressionList");

            writer.WriteString("");

            int numExpressions = 0;

            if(tokens[tokenIndex].text != ")")
            {
                CompileExpression(writer, tokens);

                numExpressions = 1;

                while(tokens[tokenIndex].text == ",")
                {
                    // ,
                    WriteElement(writer, tokens);

                    CompileExpression(writer, tokens);

                    numExpressions++;
                }


            }

            writer.WriteEndElement();

            return numExpressions;
        }

        private static void CompileTerm(XmlWriter writer, List<Token> tokens)
        {
            writer.WriteStartElement("term");

            if (tokens[tokenIndex].text == "-" || tokens[tokenIndex].text == "~")
            {
                // unary op - -x or ~(x == y) or etc..
                string op = tokens[tokenIndex].text;
                WriteElement(writer, tokens);

                CompileTerm(writer, tokens);

                if (op == "-")
                    vmWriter.WriteArithmetic("neg");
                else if (op == "~")
                    vmWriter.WriteArithmetic("not");
            }
            else if (tokens[tokenIndex].text == "(") // ( expression )
            {
                // (
                WriteElement(writer, tokens);

                CompileExpression(writer, tokens);

                // )    
                WriteElement(writer, tokens);
            }
            else if (tokens[tokenIndex + 1].text == ".") // foo.Method()
            {
                // varName or className
                //Check if in classtable or subtable else its a class name
                string name = tokens[tokenIndex].text;
                bool isClass = false;
                string kind = "";
                string type = "";
                int index = -1;

                if (subroutineTable.IndexOf(name) != -1)
                {
                    kind = subroutineTable.KindOf(name);
                    type = subroutineTable.TypeOf(name);
                    index = subroutineTable.IndexOf(name);
                    WriteElement(writer, tokens, kind + "_" + index + "_used");
                }
                else if (classTable.IndexOf(name) != -1)
                {
                    kind = classTable.KindOf(name);
                    type = classTable.TypeOf(name);
                    index = classTable.IndexOf(name);
                    WriteElement(writer, tokens, classTable.KindOf(name) + "_" + classTable.IndexOf(name) + "_used");
                }
                else
                {
                    isClass = true;
                    WriteElement(writer, tokens, "class_used");
                }

                // .
                WriteElement(writer, tokens);

                // subroutineName
                string subroutineName = tokens[tokenIndex].text;
                WriteElement(writer, tokens, "subroutine_used");
                
                // (
                WriteElement(writer, tokens);

                if (!isClass)
                {
                    vmWriter.WritePush(kind, index);
                }

                //push pointer if !isClass ?

                int numArgs = CompileExpressionList(writer, tokens);

                if (!isClass)
                {
                    vmWriter.WriteCall(type + "." + subroutineName, numArgs + 1);
                }
                else
                {
                    vmWriter.WriteCall(name + "." + subroutineName, numArgs);
                }

                //vmWriter.WritePop("temp", 1);

                // )
                WriteElement(writer, tokens);
            }
            else if (tokens[tokenIndex + 1].text == "(") // Method()
            {
                // subroutineName
                string name = tokens[tokenIndex].text;
                WriteElement(writer, tokens, "subroutine_used");

                // (
                WriteElement(writer, tokens);

                int numArgs = CompileExpressionList(writer, tokens);

                vmWriter.WriteCall(name, numArgs + 1);

                //vmWriter.WritePop("temp", 1);

                // )
                WriteElement(writer, tokens);
            }
            else if (tokens[tokenIndex + 1].text == "[") // foo[val]
            {
                // varName
                string name = tokens[tokenIndex].text;
                string kind = "";
                int index = -1;

                if (subroutineTable.IndexOf(name) != -1)
                {
                    kind = subroutineTable.KindOf(name);
                    index = subroutineTable.IndexOf(name);
                    WriteElement(writer, tokens, subroutineTable.KindOf(name) + "_" + subroutineTable.IndexOf(name) + "_used");
                }
                else if (classTable.IndexOf(name) != -1)
                {
                    kind = classTable.KindOf(name);
                    index = classTable.IndexOf(name);
                    WriteElement(writer, tokens, classTable.KindOf(name) + "_" + classTable.IndexOf(name) + "_used");
                }
                else
                {
                    WriteElement(writer, tokens, "var_MissingNo_used");
                }

                // [
                WriteElement(writer, tokens);

                vmWriter.WritePush(kind, index);

                CompileExpression(writer, tokens);

                vmWriter.WriteArithmetic("+"); // adds var + [i]

                vmWriter.WritePop("pointer", 1); // sets as current array
                vmWriter.WritePush("that", 0); // gets value stored in array index

                // ]
                WriteElement(writer, tokens);
            }
            else // constants, varName, etc
            {
                string name = tokens[tokenIndex].text;
                int constVal = -1;

                if (name == "true")
                {
                    vmWriter.WritePush("constant", 1);
                    vmWriter.WriteArithmetic("neg");

                    WriteElement(writer, tokens);
                }
                else if (name == "this")
                {
                    vmWriter.WritePush("pointer", 0);

                    WriteElement(writer, tokens);
                }
                else if (name == "false" || name == "null")
                {
                    vmWriter.WritePush("constant", 0);

                    WriteElement(writer, tokens);
                }
                else if (subroutineTable.KindOf(name) != null)
                {
                    WriteElement(writer, tokens, subroutineTable.KindOf(name) + "_" + subroutineTable.IndexOf(name) + "_used");

                    vmWriter.WritePush(subroutineTable.KindOf(name), subroutineTable.IndexOf(name));
                }
                else if (classTable.KindOf(name) != null)
                {
                    WriteElement(writer, tokens, classTable.KindOf(name) + "_" + classTable.IndexOf(name) + "_used");

                    vmWriter.WritePush(classTable.KindOf(name), classTable.IndexOf(name));
                }
                else if(Int32.TryParse(tokens[tokenIndex].text, out constVal))
                {
                    WriteElement(writer, tokens);
                    vmWriter.WritePush("constant", constVal);
                }
                else //string constant
                {
                    WriteElement(writer, tokens);

                    vmWriter.WritePush("constant", name.Length);
                    vmWriter.WriteCall("String.new", 1);

                    foreach (char character in name)
                    {
                        int ascii = character;
                        vmWriter.WritePush("constant", ascii);
                        vmWriter.WriteCall("String.appendChar", 2);
                    }
                }

                //fileWriter.Write(name);
            }

            writer.WriteEndElement();
        }

        private static int CompileVarDec(XmlWriter writer, List<Token> tokens)
        {
            int varNum = 1;

            writer.WriteStartElement("varDec");

            // var
            WriteElement(writer, tokens);

            // type
            string type = tokens[tokenIndex].text;
            if(tokens[tokenIndex].type == "identifier")
            {
                WriteElement(writer, tokens, "class_used");
            }
            else
            {
                WriteElement(writer, tokens);
            }

            // varName
            string name = tokens[tokenIndex].text;
            WriteElement(writer, tokens, type + "_" + subroutineTable.VarCount("var") + "_defined");

            subroutineTable.Define(name, type, "var");

            while (tokens[tokenIndex].text == ",")
            {
                // ,
                WriteElement(writer, tokens);

                //varName
                name = tokens[tokenIndex].text;
                WriteElement(writer, tokens, type + "_" + subroutineTable.VarCount("var") + "_defined");

                subroutineTable.Define(name, type, "var");

                varNum += 1;
            }

            // ;
            WriteElement(writer, tokens);

            writer.WriteEndElement();

            return varNum;
        }

        private static int ComposeParameterList(XmlWriter writer, List<Token> tokens)
        {
            // (
            WriteElement(writer, tokens);

            writer.WriteStartElement("parameterList");

            int numParams = 0;

            if (tokens[tokenIndex].text != ")")
            {
                // type
                string type = tokens[tokenIndex].text; 
                WriteElement(writer, tokens);

                //varName
                string name = tokens[tokenIndex].text;
                WriteElement(writer, tokens, "arg_" + subroutineTable.VarCount("arg") + "_defined");

                subroutineTable.Define(name, type, "arg");

                numParams = 1;

                while (tokens[tokenIndex].text != ")")
                {
                    // ,
                    WriteElement(writer, tokens);

                    // type
                    type = tokens[tokenIndex].text;
                    WriteElement(writer, tokens);

                    //varName
                    name = tokens[tokenIndex].text;
                    WriteElement(writer, tokens, "arg_" + subroutineTable.VarCount("arg") + "_defined");

                    subroutineTable.Define(name, type, "arg");

                    numParams++;
                }
            }
            else
            {
                writer.WriteString("");
            }

            writer.WriteEndElement();

            // )
            WriteElement(writer, tokens);

            return numParams;
        }
    }
}
