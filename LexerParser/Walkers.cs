using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LexerParser
{
    public class ParserResultWalker
    {
        public bool ShowOnConsoleDefault { get; set; }
        public bool FilterOutEBNF { get; set; }
        public bool ShowReturnMessage { get; set; }
        public Parser.ParserResult ParserResult { get; set; }
        public ParserResultWalker(Parser.ParserResult parserResult, bool showOnConsoleDefault = false, bool filterOutEBNF = false, bool showReturnMessage = false)
        {
            ParserResult = parserResult;
            ShowOnConsoleDefault = showOnConsoleDefault;
            ShowReturnMessage = ShowReturnMessage;
            FilterOutEBNF = filterOutEBNF;
        }
        public virtual void Visit()
        {
            VisitSequenceNode(ParserResult);
        }
        public virtual void VisitSequenceNode(Parser.ParserResult node, int level = 0)
        {
            string levelString = "".PadLeft(level, ' ');
            if (node.Span != null)
            {
                VisitToken(node.Span, level + 1);
            }
            else
            {
                if (ShowOnConsoleDefault)
                {
                    if ((FilterOutEBNF == true && node.Name.Contains(":::") == false) || FilterOutEBNF == false)
                    {
                        Console.WriteLine($"{levelString}Node:{node}");
                    }
                }
                foreach (var item in node.InnerResults)
                {
                    VisitSequenceNode(item, level + 1);
                }
                if (ShowOnConsoleDefault)
                {
                    if (ShowReturnMessage)
                    {
                        if ((FilterOutEBNF == true && node.Name.Contains(":::") == false) || FilterOutEBNF == false)
                        {
                            Console.WriteLine($"{levelString}Return from Node:{node}");
                        }
                    }
                }
            }
        }
        public virtual void VisitToken(Lexer.Span span, int level = 0)
        {
            string levelString = "".PadLeft(level, ' ');
            if (ShowOnConsoleDefault)
            {
                Console.WriteLine($"{levelString}Token:{span}");
            }
        }
    }
    public class ConsoleWalker : ParserResultWalker
    {
        public List<(string, string)> Nodes = new List<(string, string)>();
        public ConsoleWalker(Parser.ParserResult parserResult) : base(parserResult, showOnConsoleDefault: false)
        {
            Visit();
        }
        public override void Visit()
        {
            base.Visit();
        }
        void SetColor(Parser.ParserResult node, bool backwards = false)
        {
            if (new string[] { "htmlOpenTag", "htmlCloseTag" }.Contains(node.Name))
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
            }
            else if (node.Name == "htmlAttribute")
            {
                if (backwards)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                }
            }
            else if (node.Name == "htmlTagName")
            {
                if (backwards)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                }
            }
            else if (node.Name == "htmlInnerTagText")
            {
                Console.ForegroundColor = ConsoleColor.White;
            }
        }
        public override void VisitSequenceNode(Parser.ParserResult node, int level = 0)
        {
            Nodes.Add((node.Name, "before"));
            SetColor(node);
            base.VisitSequenceNode(node, level);
            SetColor(node, true);
            Nodes.Add((node.Name, "after"));
        }
        public override void VisitToken(Lexer.Span span, int level = 0)
        {
            Nodes.Add(("token", span.Text));
            if (span.Text == "=")
            {
                Console.ForegroundColor = ConsoleColor.DarkBlue;
                Console.Write(span.Text);
            }
            else if (span.Text == "\"")
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.Write(span.Text);
                Console.ForegroundColor = ConsoleColor.Yellow;
            }
            else
            {
                Console.Write(span.Text);
            }
            base.VisitToken(span, level);
        }
    }
}
