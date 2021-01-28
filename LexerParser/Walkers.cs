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
    public class SqlConsoleWalker : ParserResultWalker
    {
        public List<(string, string)> Nodes = new List<(string, string)>();
        public SqlConsoleWalker(Parser.ParserResult parserResult, bool visitOnInit = true) : base(parserResult, showOnConsoleDefault: false)
        {
            if (visitOnInit)
            {
                Visit();
            }
        }
        public override void Visit()
        {
            base.Visit();
        }
        void SetColor(Parser.ParserResult node, bool backwards = false)
        {
            if (node.Name == "sqlIdentifier")
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
            }
            if (node.Name == "comma")
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
            }
            if (node.InnerResultsText.Contains("as"))
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
            }
            if (node.InnerResultsText.Contains("select"))
            {
                Console.ForegroundColor = ConsoleColor.Blue;
            }
            if (node.InnerResultsText.Contains("from"))
            {
                Console.ForegroundColor = ConsoleColor.Blue;
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
    public class HtmlConsoleWalker : ParserResultWalker
    {
        public List<(string, string)> Nodes = new List<(string, string)>();
        public HtmlConsoleWalker(Parser.ParserResult parserResult, bool visitOnInit = true) : base(parserResult, showOnConsoleDefault: false)
        {
            if (visitOnInit) {
                Visit();
            }
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
                    Console.ForegroundColor = ConsoleColor.Cyan;
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
                    Console.ForegroundColor = ConsoleColor.Blue;
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
                Console.ForegroundColor = ConsoleColor.DarkCyan;
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
    public class CssConsoleWalker : ParserResultWalker
    {
        public List<(string, string)> Nodes = new List<(string, string)>();
        public CssConsoleWalker(Parser.ParserResult parserResult, bool visitOnInit = true) : base(parserResult, showOnConsoleDefault: false)
        {
            if (visitOnInit) {
                Visit();
            }
        }
        public override void Visit()
        {
            base.Visit();
        }
        void SetColor(Parser.ParserResult node, bool backwards = false)
        {
            if (new string[] { "cssCodeBlock" }.Contains(node.Name))
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
            }
            else if (node.Name == "semicolon" || node.Name == "colon")
            {
                if (backwards)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkBlue;
                }
            }
            else if (node.Name == "cssPropertyName")
            {
                if (backwards)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Blue;
                }
            }
            else if (node.Name == "cssSelectorName")
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
            else if (node.Name == "cssStringDblQuote" || node.Name == "cssStringQuote")
            {
                if (backwards)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                }
            }
            else if (node.Name == "cssString")
            {
                if (backwards)
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                }
            }
            else if (node.Name == "cssColorIdentifier")
            {
                if (backwards)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                }
            }
            else if (node.Name == "cssValueLiteral")
            {
                if (backwards)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                }
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
    public class CalculatorConsoleWalker : ParserResultWalker
    {
        public List<(string, string)> Nodes = new List<(string, string)>();
        public CalculatorConsoleWalker(Parser.ParserResult parserResult, bool visitOnInit = true) : base(parserResult, showOnConsoleDefault: false)
        {
            if (visitOnInit)
            {
                Visit();
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }
        public override void Visit()
        {
            base.Visit();
        }
        void SetColor(Parser.ParserResult node, bool backwards = false)
        {
            if (new string[] { "parenthesisOpen", "parenthesisClose" }.Contains(node.Name))
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
            }
            else if (node.Name == "mathNum")
            {
                if (backwards)
                {
                    Console.ForegroundColor = ConsoleColor.DarkBlue;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Blue;
                }
            }
            else if (new[] { "mathMultiply", "mathDivide", "mathAdd", "mathSubtract" }.Contains(node.Name))
            {
                Console.ForegroundColor = ConsoleColor.DarkBlue;
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
    public class EvaluationResultConsoleWalker
    {
        public List<(string, string)> Nodes = new List<(string, string)>();
        public EvaluationResultConsoleWalker(Parser.EvaluationResult parserResult, bool visitOnInit = true)
        {
            if (visitOnInit)
            {
                VisitEvaluationNode(parserResult, 0);
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }
        void SetColor(ConsoleColor consoleColor, string text)
        {
            Console.ForegroundColor = consoleColor;
            Console.Write(text);
        }
        public void VisitEvaluationNode(Parser.EvaluationResult node, int level = 0)
        {
            SetColor(ConsoleColor.DarkGray, "[");
            SetColor(ConsoleColor.Blue, "Eval Result");
            SetColor(ConsoleColor.DarkBlue, ": ");
            SetColor(ConsoleColor.Yellow, node.EvaluationType.Name);
            SetColor(ConsoleColor.DarkYellow, "/");
            SetColor(ConsoleColor.Yellow, node.EvaluationText);
            SetColor(ConsoleColor.DarkGray, "]");
            SetColor(ConsoleColor.Gray, "");
            Console.WriteLine();
        }
    }

    public class LexerResultWalker
    {
        List<(int Line, int Index, Lexer.Span Data)> Data { get; set; }
        Func<string, string> setColor = new Func<string, string>((rule) =>
        {
            switch (rule)
            {
                case "quote":
                case "doubleQuote":
                case "quoting":
                case "parenthesisOpen":
                case "parenthesisClose":
                    Console.ForegroundColor = ConsoleColor.Yellow; break;
                case "digits":
                    Console.ForegroundColor = ConsoleColor.Cyan; break;
                case "letters":
                    Console.ForegroundColor = ConsoleColor.Gray; break;
                case "greaterThan":
                case "lessThan":
                    Console.ForegroundColor = ConsoleColor.Green; break;
                case "forwardSlash":
                    Console.ForegroundColor = ConsoleColor.White; break;
                default: break;
            }
            return rule;
        });
        public LexerResultWalker(List<Lexer.Span> data)
        {
            int line = 0;
            foreach (var span in data)
            {
                //if (span.Line != line) { Console.WriteLine(); line = span.Line; }
                foreach (var rule in span.InnerSpans.Select(x => x.Rule))
                {
                    string currentRule = rule.RuleName;
                    setColor(currentRule);
                }
                setColor(span.Rule.RuleName);
                Console.Write(span.Text);
            }
        }
        public LexerResultWalker(Lexer.LexerResult lexer)
        {
            int line = 0;
            foreach(var span in lexer.CollectionInnerSpans)
            {
                if (span.Line != line) { Console.WriteLine(); line = span.Line; }
                foreach(var rule in span.Data.InnerSpans.Select(x => x.Rule))
                {
                    string currentRule = rule.RuleName;
                    setColor(currentRule);
                }
                setColor(span.Data.Rule.RuleName);
                Console.Write(span.Data.Text);
            }
        }
    }
}