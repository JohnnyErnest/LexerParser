using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LexerParser;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LexerParser1
{
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
                //ParserResultWalker walker = new ParserResultWalker(result.Results[0], showOnConsoleDefault: true);
                //walker.Visit();

                Console.WriteLine();
                ConsoleWalker walker2 = new ConsoleWalker(result.Results[0]);
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine();
                Console.WriteLine();
            }
            Console.ForegroundColor = ConsoleColor.Gray;

            string mathNegative = "mathNegative = hyphen;";
            string mathDecimal = "mathDecimal = period;";
            string mathNum = "mathNum = [mathNegative], digits, [ mathDecimal, digits ];";
            string mathAdd = "mathAdd = plus;";
            string mathSubtract = "mathSubtract = hyphen;";
            string mathMultiply = "mathMultiply = asterisk;";
            string mathDivide = "mathDivide = forwardSlash;";

            string mathFunction = "mathFunction = 'sqrt'|'cos'|'sin'|'tan';";

            string mathParentheses = "mathParentheses = [mathFunction], parenthesisOpen, &mathExpr, parenthesisClose;";
            string mathFactor = "mathFactor = (mathNum | mathParentheses);";
            string mathTerm = "mathTerm = mathFactor, { (mathMultiply, mathFactor) | (mathDivide, mathFactor) };";
            string mathExpr = "mathExpr = mathTerm, { (mathAdd, mathTerm) | (mathSubtract, mathTerm) };";

            parser.AddEBNFRule(mathNegative);
            parser.AddEBNFRule(mathDecimal);
            parser.AddEBNFRule(mathNum);
            parser.AddEBNFRule(mathAdd);
            parser.AddEBNFRule(mathSubtract);
            parser.AddEBNFRule(mathMultiply);
            parser.AddEBNFRule(mathDivide);
            parser.AddEBNFRule(mathFunction);
            parser.AddEBNFRule(mathParentheses);
            parser.AddEBNFRule(mathFactor);
            parser.AddEBNFRule(mathTerm);
            parser.AddEBNFRule(mathExpr);

            string inputCalc = "((3*2.5)+1.2)+((2*7*5)*12.5)+5";
            //string inputCalc = "((3*2.5)+1.2)*5.7";
            var resultCalc = parser.Parse(inputCalc, sequenceName: "mathExpr", showOnConsole: false);
            if (resultCalc.Matched)
            {
                var secondary = resultCalc.Results[0].Clone() as Parser.ParserResult;
                List<Parser.ParserResult> results1 = new List<Parser.ParserResult>();
                results1.Add(secondary);
                results1 = parser.OrganizeParentNodes(results1);

                Func<Parser.ParserResult, Parser.EvaluationResult> funcEvaluate = new Func<Parser.ParserResult, Parser.EvaluationResult>((input) =>
                {
                    Func<Parser.ParserResult, int, Parser.ParserResult> checkGroups = null;
                    checkGroups = new Func<Parser.ParserResult, int, Parser.ParserResult>((input1, level) =>
                    {
                        string levels = "".PadLeft(level, ' ');
                        Console.WriteLine($"{levels}Group:{input.GroupName}, Name:{input1.Name}, Inner:{input1.InnerResultsText}, Eval:{input1.EvaluationResult}");
                        foreach (var g in input1.InnerResults)
                        {
                            checkGroups(g, level + 1);
                        }
                        return null;
                    });

                    Console.WriteLine("Initial Calculation:" + inputCalc);
                    checkGroups(input, 0);

                    var exprs = input.GetDescendantsOfType(new string[] { "mathExpr" });

                    bool done = false;
                    int idx = 0;
                    while (!done)
                    {
                        Console.WriteLine("Evaluating Iteration " + idx);

                        List<Parser.EvaluationResult> numbers = new List<Parser.EvaluationResult>();
                        // Roll Up Numbers
                        for (int i = 0; i < exprs.Count; i++)
                        {
                            double value = 0.0;
                            foreach (var num in exprs[i].GetDescendantsOfType(new string[] { "mathNum" }).Where(x => x.Evaluated == false))
                            {
                                num.EvaluationFunction = new Func<Parser.ParserResult, Parser.EvaluationResult>(num =>
                                {
                                    value = double.Parse(num.InnerResultsText);
                                    return new Parser.EvaluationResult()
                                    {
                                        EvaluationType = typeof(double),
                                        EvaluationValue = value,
                                        EvaluationText = value.ToString()
                                    };
                                });
                                if (num.EvaluationResult == null)
                                {
                                    num.EvaluationResult = num.EvaluationFunction(num);
                                    num.Evaluated = true;
                                }
                                int min = num.MinStart();
                                num.Span = new Lexer.Span()
                                {
                                    Start = min,
                                    Text = value.ToString(),
                                    Rule = new Lexer.LexerRules.StringLexerRule("", ""),
                                    InnerSpans = new List<Lexer.Span>(),
                                };
                                num.InnerResults = new List<Parser.ParserResult>();
                                numbers.Add(num.EvaluationResult);
                                Console.WriteLine($"Number:{num}");
                            }
                        }
                        // Roll Up Signs
                        for (int i = 0; i < exprs.Count; i++)
                        {
                            foreach (var num in exprs[i].GetDescendantsOfType(new string[] { "mathMultiply", "mathDivide", "mathAdd", "mathSubtract" }).Where(x => x.Evaluated == false))
                            {
                                if (num.Name == "mathMultiply" || num.Name == "mathDivide" ||
                                    num.Name == "mathAdd" || num.Name == "mathSubtract")
                                {
                                    string text = "";
                                    switch (num.Name)
                                    {
                                        case "mathMultiply": text = "*"; break;
                                        case "mathDivide": text = "*"; break;
                                        case "mathAdd": text = "*"; break;
                                        case "mathSubtract": text = "*"; break;
                                        default: break;
                                    }
                                    num.Evaluated = true;
                                    num.EvaluationResult = new Parser.EvaluationResult() { EvaluationType = typeof(string), EvaluationText = text, EvaluationValue = text };
                                    int minStart = num.MinStart();
                                    num.InnerResults = new List<Parser.ParserResult>();
                                    num.Span = new Lexer.Span() { Start = minStart, Text = text, InnerSpans = new List<Lexer.Span>(), Rule = new Lexer.LexerRules.StringLexerRule("", text) };
                                }
                            }
                        }
                        // Roll Up Non-Parenthetical Factors from Numbers
                        for (int i = 0; i < exprs.Count; i++)
                        {
                            foreach (var num in exprs[i].GetDescendantsOfType(new string[] { "mathFactor" }).Where(x => x.Evaluated == false))
                            {
                                if (num.GetDescendantsOfType(new[] { "mathParentheses" }).Count() == 0 &&
                                    num.GetDescendantsOfType(new[] { "mathNum" }).Count() == 1)
                                {
                                    var numbers1 = num.GetDescendantsOfType(new[] { "mathNum" }).First();
                                    num.Span = numbers1.Span.Clone() as Lexer.Span;
                                    num.EvaluationResult = numbers1.EvaluationResult.Clone() as Parser.EvaluationResult;
                                    num.Evaluated = true;
                                    num.InnerResults = new List<Parser.ParserResult>();
                                    num.Name = "mathNum";
                                    num.GroupName = "mathNum";
                                }
                            }
                        }
                        // Roll Up Non-Parenthetical Term Equations
                        for (int i = 0; i < exprs.Count; i++)
                        {
                            foreach (var num in exprs[i].GetDescendantsOfType(new string[] { "mathTerm" }).Where(x => x.Evaluated == false &&
                                x.GetDescendantsOfType(new[] { "mathParentheses" }).Count() == 0))
                            {
                                var terms = num.GetDescendantsOfType(new[] { "mathMultiply", "mathDivide", "mathNum" }).OrderBy(x => x.MinStart());
                                double value = 0.0;
                                string currentOp = "";
                                foreach (var term in terms)
                                {
                                    if (term.Name == "mathMultiply") { currentOp = "*"; }
                                    else if (term.Name == "mathDivide") { currentOp = "/"; }
                                    else
                                    {
                                        if (currentOp == "" && term.Name == "mathNum") { value = (double)term.EvaluationResult.EvaluationValue; }
                                        else if (currentOp == "*" && term.Name == "mathNum") { value *= (double)term.EvaluationResult.EvaluationValue; }
                                        else if (currentOp == "/" && term.Name == "mathNum") { value /= (double)term.EvaluationResult.EvaluationValue; }
                                    }
                                }
                                int start = num.MinStart();
                                num.EvaluationResult = new Parser.EvaluationResult()
                                {
                                    EvaluationText = value.ToString(),
                                    EvaluationType = typeof(double),
                                    EvaluationValue = value
                                };
                                num.Evaluated = true;
                                num.Name = "mathNum";
                                num.GroupName = "mathNum";
                                num.InnerResults = new List<Parser.ParserResult>();
                                num.Span = new Lexer.Span() { InnerSpans = new List<Lexer.Span>(), Start = start, Text = value.ToString(), Rule = new Lexer.LexerRules.StringLexerRule("", value.ToString()) };
                            }

                        }
                        // Roll Up Non-Parenthetical Expressions
                        for (int i = 0; i < exprs.Count; i++)
                        {
                            foreach (var num in exprs[i].GetDescendantsOfType(new string[] { "mathExpr" }).Where(x => x.Evaluated == false &&
                                x.GetDescendantsOfType(new[] { "mathParentheses" }).Count() == 0))
                            {
                                var terms = num.GetDescendantsOfType(new[] { "mathAdd", "mathSubtract", "mathNum" }).OrderBy(x => x.MinStart());
                                double value = 0.0;
                                string currentOp = "";
                                foreach (var term in terms)
                                {
                                    if (term.Name == "mathAdd") { currentOp = "+"; }
                                    else if (term.Name == "mathSubtract") { currentOp = "-"; }
                                    else
                                    {
                                        if (currentOp == "" && term.Name == "mathNum") { value = (double)term.EvaluationResult.EvaluationValue; }
                                        else if (currentOp == "+" && term.Name == "mathNum") { value += (double)term.EvaluationResult.EvaluationValue; }
                                        else if (currentOp == "-" && term.Name == "mathNum") { value -= (double)term.EvaluationResult.EvaluationValue; }
                                    }
                                }
                                int start = num.MinStart();
                                num.EvaluationResult = new Parser.EvaluationResult()
                                {
                                    EvaluationText = value.ToString(),
                                    EvaluationType = typeof(double),
                                    EvaluationValue = value
                                };
                                num.Evaluated = true;
                                num.Name = "mathNum";
                                num.GroupName = "mathNum";
                                num.InnerResults = new List<Parser.ParserResult>();
                                num.Span = new Lexer.Span() { InnerSpans = new List<Lexer.Span>(), Start = start, Text = value.ToString(), Rule = new Lexer.LexerRules.StringLexerRule("", value.ToString()) };
                            }
                        }
                        // Roll Up Parentheses with No Expressions
                        for (int i = 0; i < exprs.Count; i++)
                        {
                            foreach (var num in exprs[i].GetDescendantsOfType(new string[] { "mathParentheses" }).Where(x => x.Evaluated == false &&
                                x.GetDescendantsOfType(new[] { "mathExpr" }).Count() == 0))
                            {
                                var numbers1 = num.GetDescendantsOfType(new[] { "mathNum" }).OrderBy(x => x.MinStart());
                                if (numbers1.Count() == 1)
                                {
                                    var num1 = numbers1.First();
                                    int start = num.MinStart();
                                    num.EvaluationResult = new Parser.EvaluationResult()
                                    {
                                        EvaluationText = num1.EvaluationResult.EvaluationValue.ToString(),
                                        EvaluationType = typeof(double),
                                        EvaluationValue = num1.EvaluationResult.EvaluationValue
                                    };
                                    num.Evaluated = true;
                                    num.Name = "mathNum";
                                    num.GroupName = "mathNum";
                                    num.InnerResults = new List<Parser.ParserResult>();
                                    num.Span = new Lexer.Span() { InnerSpans = new List<Lexer.Span>(), Start = start, Text = num1.EvaluationResult.EvaluationValue.ToString(), Rule = new Lexer.LexerRules.StringLexerRule("", num1.EvaluationResult.EvaluationValue.ToString()) };
                                }
                            }
                        }

                        Console.WriteLine(inputCalc);
                        checkGroups(input, 0);
                        if (input.InnerResults.Count == 0)
                        {
                            done = true;
                        }
                        idx++;
                    }
                    Console.WriteLine();

                    //Program.exprFunc(exprs.Last());

                    Console.WriteLine(inputCalc);
                    
                    return new Parser.EvaluationResult()
                    {
                        EvaluationText = input.InnerResultsText,
                        EvaluationType = typeof(double),
                        EvaluationValue = double.Parse(input.InnerResultsText)
                    };
                });

                results1[0].EvaluationFunction = funcEvaluate;
                results1[0].EvaluationResult = results1[0].EvaluationFunction(results1[0]);

                ParserResultWalker walker3 = new ParserResultWalker(resultCalc.Results[0], showOnConsoleDefault: true, filterOutEBNF: true);
                walker3.Visit();
            }
        }
    }
}