﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LexerParser1
{
    public class ParserResultWalker
    {
        public bool ShowOnConsoleDefault { get; set; }
        public Parser.ParserResult ParserResult { get; set; }
        public ParserResultWalker(Parser.ParserResult parserResult, bool showOnConsoleDefault = false)
        {
            ParserResult = parserResult;
            ShowOnConsoleDefault = showOnConsoleDefault;
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
                    Console.WriteLine($"{levelString}Node:{node.Name}, Start:{node.MinStart()}, InnerText:{node.InnerResultsText}");
                }
                foreach (var item in node.InnerResults)
                {
                    VisitSequenceNode(item, level + 1);
                }
                if (ShowOnConsoleDefault)
                {
                    Console.WriteLine($"{levelString}Return from Node:{node.Name}, End:{node.MaxEnd()}, InnerText:{node.InnerResultsText}");
                }
            }
        }
        public virtual void VisitToken(Lexer.Span span, int level = 0)
        {
            string levelString = "".PadLeft(level, ' ');
            if (ShowOnConsoleDefault)
            {
                Console.WriteLine($"{levelString}Token: {span.Rule.RuleName}, Start:{span.Start}, Text:{span.Text}");
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
                if (backwards) {
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
                Console.ForegroundColor = ConsoleColor.Gray;
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
    class Program
    {
        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            string defaultConfiguration = $"Configuration/Default.json";
            string userConfiguration = $"Configuration/ParserEBNF.json";
            Lexer lexer = new Lexer(defaultConfiguration, userConfiguration);
            Parser parser = new Parser(lexer, userConfiguration);

            //string inputSql = "select column1, column2 as c2 from _yay";
            //string inputCss = "body { background-color: #512fff; margin-left: auto; color: '#123456'; } h2 { color: 'blue'; }";
            //string inputJs = "var i = \"Hello\"; var x=5;";
            //string inputEBNF = "a=\"5\";";
            //string inputEBNF2 = "PROGRAM BEGIN a:=5; b1:='Hurrooo'; b2:=\"Yay\"; b2 := a; END;";

            string htmlIdentifier = @"htmlIdentifier = letter, { letter|digit|hyphen|underscore };";
            string htmlAttribute = @"htmlAttribute = whitespaces, htmlIdentifier, { [whitespaces], ""="", [whitespaces], ebnfTerminalDoubleQuote };";
            string htmlTagName = @"htmlTagName = htmlIdentifier;";
            string htmlOpenTag = @"htmlOpenTag = ""<"", htmlTagName, { htmlAttribute }, [whitespaces], "">"";";
            string htmlCloseTag = @"htmlCloseTag = ""</"", htmlTagName, "">"";";
            string htmlInnerTagText = @"htmlInnerTagText = %% letters|spaces|digits|whitespaces|semicolon|underscore|equals %%;";
            string htmlTag = @"htmlTag = htmlOpenTag, {htmlInnerTagText}, {htmlTag}, {htmlInnerTagText}, htmlCloseTag;";

            //parser.EBNFAddRuleFast(htmlIdentifier);
            //parser.EBNFAddRuleFast(htmlAttribute);
            //parser.EBNFAddRuleFast(htmlTagName);
            //parser.EBNFAddRuleFast(htmlOpenTag);
            //parser.EBNFAddRuleFast(htmlCloseTag);
            //parser.EBNFAddRuleFast(htmlInnerTagText);
            //parser.EBNFAddRuleFast(htmlTag);

            parser.AddEBNFRule(htmlIdentifier);
            parser.AddEBNFRule(htmlAttribute);
            parser.AddEBNFRule(htmlTagName);
            parser.AddEBNFRule(htmlOpenTag);
            parser.AddEBNFRule(htmlCloseTag);
            parser.AddEBNFRule(htmlInnerTagText);
            parser.AddEBNFRule(htmlTag);

            string inputHtml = "<html><head><title>Title</title></head><body><h2 selected>Helloooo hi</h2><div class=\"someClass\">Here is <span>some</span> text</div></body></html>";
            var result = parser.Parse(inputHtml, sequenceName: "htmlTag", showOnConsole: false);
            if (result.Matched)
            {
                ParserResultWalker walker = new ParserResultWalker(result.Results[0], showOnConsoleDefault: true);
                walker.Visit();

                Console.WriteLine();
                ConsoleWalker walker2 = new ConsoleWalker(result.Results[0]);
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine();
                Console.WriteLine();
            }
            Console.ForegroundColor = ConsoleColor.Gray;

            string ruleNum = "ruleNum = %% digits %%;";
            string ruleFactor = "ruleFactor = ['-'], (ruleNum | ('(', &ruleExpr, ')'));";
            string ruleTerm = "ruleTerm = ruleFactor, { ('*', ruleFactor) | ('/', ruleFactor) };";
            string ruleExpr = "ruleExpr = ruleTerm, { ('+', ruleTerm) | ('-', ruleTerm) };";

            //parser.EBNFAddRuleFast(ruleNum);
            //parser.EBNFAddRuleFast(ruleFactor);
            //parser.EBNFAddRuleFast(ruleTerm);
            //parser.EBNFAddRuleFast(ruleExpr);

            parser.AddEBNFRule(ruleNum);
            parser.AddEBNFRule(ruleFactor);
            parser.AddEBNFRule(ruleTerm);
            parser.AddEBNFRule(ruleExpr);

            string inputCalc = "3*(2+1)";
            var resultCalc = parser.Parse(inputCalc, sequenceName: "ruleExpr", showOnConsole: false);
            if (resultCalc.Matched)
            {
                ParserResultWalker walker3 = new ParserResultWalker(resultCalc.Results[0], showOnConsoleDefault: true);
                walker3.Visit();
            }
        }
    }
}