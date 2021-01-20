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

            //string ruleNum = "ruleNum = %% digits %%;";
            //string ruleFactor = "ruleFactor = ['-'], (ruleNum | ('(', &ruleExpr, ')'));";
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

            string inputCalc = "sqrt((-3*2.5)+1.2+(2*7*5))*12.5";
            var resultCalc = parser.Parse(inputCalc, sequenceName: "mathExpr", showOnConsole: false);
            if (resultCalc.Matched)
            {
                var exprs = resultCalc.Results[0].GetDescendantsOfType(new string[] { "mathExpr" });

                for(int i=0;i<exprs.Count;i++)
                {
                    Console.WriteLine($"Expression:{exprs[i]}");
                    var nums = exprs[i].GetDescendantsOfType(new string[] { "mathNum" });
                    var signs = exprs[i].GetDescendantsOfType(new string[] { "mathAdd", "mathSubtract", "mathMultiply", "mathDivide" });
                    var functions = exprs[i].GetDescendantsOfType(new string[] { "mathFunction" });
                    var all = exprs[i].GetDescendantsOfType(new string[] { "mathAdd", "mathSubtract", "mathMultiply", "mathDivide", "mathNum", "mathFunction", "mathFactor", "mathTerm", "mathExpr" }).OrderBy(x => x.Level).ThenBy(x => x.MinStart()).ToArray();
                    var allFactors = exprs[i].GetDescendantsOfType(new string[] { "mathMultiply", "mathDivide", "mathFactor" }).ToArray();
                    foreach (var num in exprs[i].GetDescendantsOfType(new string[] { "mathNum" }))
                    {
                        num.EvaluationFunction = new Func<Parser.ParserResult, Parser.EvaluationResult>(num =>
                        {
                            return new Parser.EvaluationResult()
                            {
                                EvaluationType = typeof(double),
                                EvaluationValue = double.Parse(num.InnerResultsText),
                                EvaluationText = double.Parse(num.InnerResultsText).ToString()
                            };
                        });
                        if (num.EvaluationResult == null)
                        {
                            num.EvaluationResult = num.EvaluationFunction(num);
                        }
                        Console.WriteLine($"- Number:{num}");
                    }
                    Console.WriteLine();
                }

                Console.WriteLine(inputCalc);

                ParserResultWalker walker3 = new ParserResultWalker(resultCalc.Results[0], showOnConsoleDefault: true, filterOutEBNF: true);
                walker3.Visit();
            }
        }
    }
}