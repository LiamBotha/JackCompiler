using System;
using System.Collections.Generic;
using System.IO;
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

                    if(line.Contains("/*") && line.Contains("*/"))
                    {
                        var startIndex = line.IndexOf("/*");
                        var endIndex = line.IndexOf("*/") + 2;

                        line = line.Remove(startIndex, endIndex - startIndex);
                    }
                    else if(line.StartsWith("/*"))
                    {
                        isComment = true;
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

                    line = line.Replace("  ", " ");

                    string token = "";

                    Console.WriteLine(">>" + line);

                    for (int i = 0; i < line.Length; i++)
                    {
                        token = token.Trim();
                        string curr = line[i].ToString();

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

                                token = "";
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

                            if (keywords.Contains(token))// exists in keywords so its a keyword
                            {
                                tokens.Add(new Token(token, "keyword"));

                                token = "";
                            }
                        }
                    }
                }
            }

            foreach (var token in tokens)
            {
                Console.WriteLine(token.text + ", " + token.type);
            }

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
        }

        private static void WriteElement(XmlWriter writer, List<Token> tokens)
        {
            writer.WriteStartElement(tokens[tokenIndex].type);
            writer.WriteString(" " + tokens[tokenIndex].text + " ");
            writer.WriteEndElement();

            tokenIndex++;
        }

        private static void CompileClass(XmlWriter writer, List<Token> tokens)
        {
            // Root
            writer.WriteStartElement("class");

            // class
            WriteElement(writer, tokens);

            // className
            WriteElement(writer, tokens);

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
        }

        private static void CompileClassVarDec(XmlWriter writer, List<Token> tokens)
        {
            //Start Root
            writer.WriteStartElement("classVarDec");

            //Static or field
            WriteElement(writer, tokens);

            //Type
            WriteElement(writer, tokens);

            //identifier / name
            WriteElement(writer, tokens);

            while (tokens[tokenIndex].text == ",")
            {
                // ,
                WriteElement(writer, tokens);

                // varName
                WriteElement(writer, tokens);
            }

            // ;
            WriteElement(writer, tokens);

            //End Root
            writer.WriteEndElement();
        }

        private static void CompileSubroutine(XmlWriter writer, List<Token> tokens)
        {
            //Start Root
            writer.WriteStartElement("subroutineDec");

            // func, method, or ctor
            WriteElement(writer, tokens);

            //void or type
            WriteElement(writer, tokens);

            // identifier / name
            WriteElement(writer, tokens);

            // parameterList
            ComposeParameterList(writer, tokens);

            writer.WriteStartElement("subroutineBody");

            // {
            WriteElement(writer, tokens);

            while (tokens[tokenIndex].text == "var")
            {
                CompileVarDec(writer, tokens);
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

            if (tokens[tokenIndex].text != ";")
            {
                CompileExpression(writer, tokens);
            }

            // ;
            WriteElement(writer, tokens);

            writer.WriteEndElement();
        }

        private static void CompileDo(XmlWriter writer, List<Token> tokens)
        {
            writer.WriteStartElement("doStatement");

            // do
            WriteElement(writer, tokens);

            if (tokens[tokenIndex + 1].text == "(")
            {
                // subroutineName
                WriteElement(writer, tokens);

                // (
                WriteElement(writer, tokens);

                CompileExpressionList(writer, tokens);

                // )
                WriteElement(writer, tokens);
            }
            else
            {
                // varName or className
                WriteElement(writer, tokens);

                // .
                WriteElement(writer, tokens);

                // subroutineName
                WriteElement(writer, tokens);

                // (
                WriteElement(writer, tokens);

                CompileExpressionList(writer, tokens);

                // )
                WriteElement(writer, tokens);
            }

            // ;
            WriteElement(writer, tokens);

            writer.WriteEndElement();
        }

        private static void CompileWhile(XmlWriter writer, List<Token> tokens)
        {
            writer.WriteStartElement("whileStatement");

            // while
            WriteElement(writer, tokens);

            // (
            WriteElement(writer, tokens);

            CompileExpression(writer, tokens);

            // )
            WriteElement(writer, tokens);

            // {
            WriteElement(writer, tokens);

            CompileStatements(writer, tokens);

            // }
            WriteElement(writer, tokens);

            writer.WriteEndElement();
        }

        private static void CompileIf(XmlWriter writer, List<Token> tokens)
        {
            writer.WriteStartElement("ifStatement");

            // if
            WriteElement(writer, tokens);

            // (
            WriteElement(writer, tokens);

            CompileExpression(writer, tokens);

            // )
            WriteElement(writer, tokens);

            // {
            WriteElement(writer, tokens);

            CompileStatements(writer, tokens);

            // }
            WriteElement(writer, tokens);

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

            writer.WriteEndElement();
        }

        private static void CompileLet(XmlWriter writer, List<Token> tokens)
        {
            writer.WriteStartElement("letStatement");

            // let
            WriteElement(writer, tokens);

            // varName
            WriteElement(writer, tokens);

            // [ + Expression +  ] 
            if (tokens[tokenIndex].text == "[")
            {
                // [
                WriteElement(writer, tokens);

                CompileExpression(writer, tokens);

                // ]
                WriteElement(writer, tokens);
            }

            // =
            WriteElement(writer, tokens);

            // expression
            CompileExpression(writer, tokens);

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
                WriteElement(writer, tokens);

                CompileTerm(writer, tokens);
            }

            writer.WriteEndElement();
        }

        private static void CompileExpressionList(XmlWriter writer, List<Token> tokens)
        {
            writer.WriteStartElement("expressionList");

            writer.WriteString("");

            if(tokens[tokenIndex].text != ")")
            {
                CompileExpression(writer, tokens);
                
                while(tokens[tokenIndex].text == ",")
                {
                    // ,
                    WriteElement(writer, tokens);

                    CompileExpression(writer, tokens);
                }
            }

            writer.WriteEndElement();
        }

        private static void CompileTerm(XmlWriter writer, List<Token> tokens)
        {
            writer.WriteStartElement("term");

            if (tokens[tokenIndex].text == "-" || tokens[tokenIndex].text == "~")
            {
                // unary op
                WriteElement(writer, tokens);

                CompileTerm(writer, tokens);
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
                WriteElement(writer, tokens);

                // .
                WriteElement(writer, tokens);

                // subroutineName
                WriteElement(writer, tokens);

                // (
                WriteElement(writer, tokens);

                CompileExpressionList(writer, tokens);

                // )
                WriteElement(writer, tokens);
            }
            else if (tokens[tokenIndex + 1].text == "(") // Method()
            {
                // subroutineName
                WriteElement(writer, tokens);

                // (
                WriteElement(writer, tokens);

                CompileExpressionList(writer, tokens);

                // )
                WriteElement(writer, tokens);
            }
            else if (tokens[tokenIndex + 1].text == "[") // foo[val]
            {
                // varName
                WriteElement(writer, tokens);

                // [
                WriteElement(writer, tokens);

                CompileExpression(writer, tokens);

                // ]
                WriteElement(writer, tokens);
            }

            else // constants, varName, etc
            {
                WriteElement(writer, tokens);
            }

            writer.WriteEndElement();
        }

        private static void CompileVarDec(XmlWriter writer, List<Token> tokens)
        {
            writer.WriteStartElement("varDec");

            // var
            WriteElement(writer, tokens);

            // type
            WriteElement(writer, tokens);

            // varName
            WriteElement(writer, tokens);

            while (tokens[tokenIndex].text == ",")
            {
                // ,
                WriteElement(writer, tokens);

                //varName
                WriteElement(writer, tokens);
            }

            // ;
            WriteElement(writer, tokens);

            writer.WriteEndElement();
        }

        private static void ComposeParameterList(XmlWriter writer, List<Token> tokens)
        {
            // (
            WriteElement(writer, tokens);

            writer.WriteStartElement("parameterList");

            writer.WriteString("");

            if (tokens[tokenIndex].text != ")")
            {
                // type
                WriteElement(writer, tokens);

                //varName
                WriteElement(writer, tokens);

                while (tokens[tokenIndex].text != ")")
                {
                    // ,
                    WriteElement(writer, tokens);

                    // type
                    WriteElement(writer, tokens);

                    //varName
                    WriteElement(writer, tokens);
                }
            }

            writer.WriteEndElement();

            // )
            WriteElement(writer, tokens);
        }
    }
}
