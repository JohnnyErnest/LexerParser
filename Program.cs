using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

            Task<Parser.Result> t = Task.Run(() =>
            {
                Lexer lexerA = new Lexer(defaultConfiguration, userConfiguration);
                Parser parserA = new Parser(lexerA, userConfiguration);
                string htmlGrammarA = @"
                    htmlIdentifier = letter, { letter | digit | hyphen | underscore };
                    htmlAttribute = whitespaces, htmlIdentifier, { {whitespaces}, ""="", {whitespaces}, ebnfTerminalDoubleQuote };
                    htmlTagName = htmlIdentifier;
                    htmlOpenTag = ""<"", htmlTagName, { htmlAttribute }, [whitespaces], "">"";
                    htmlOpenAndCloseTag = {whitespaces}, ""<"", htmlTagName, { htmlAttribute }, {whitespaces}, ""/>"", {whitespaces};
                    htmlCloseTag = ""</"", htmlTagName, "">"";
                    htmlInnerTagText = %% letters|spaces|digits|whitespaces|period|hyphen|colon|semicolon|comma|ampersand|asterisk|doubleQuote|quote|forwardSlash|backSlash|underscore|equals|parenthesisOpen|parenthesisClose|bracketOpen|bracketClose|braceOpen|braceClose|pipe|atSign %%;
                    htmlComment = ""<!--"", {whitespaces}, {letters|digits|period|whitespaces}, {whitespaces}, ""-->"";
                    htmlTag = {whitespaces|htmlComment}, htmlOpenTag, {htmlComment|htmlInnerTagText}, { htmlComment | htmlTag | htmlOpenAndCloseTag }, {htmlComment | htmlInnerTagText}, htmlCloseTag, {whitespaces|htmlComment};
                    htmlTagNoInnerTag = htmlOpenTag, {htmlInnerTagText}, htmlCloseTag;
                    htmlTagSearch = htmlOpenTag|htmlCloseTag|htmlOpenAndCloseTag;";
                parserA.AddEBNFGrammar(htmlGrammarA.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries));

                //parserA.InputLexer.BuildTokenLookupMap();
                //parserA.BuildAllSequenceSectionTokenLookups();

                string text1 = File.ReadAllText($"Samples/index.html");
                //System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();

                //watch.Start();
                var search = text1.Split(new string[] { Environment.NewLine, "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                var lexer1 = parserA.InputLexer.GetSpans(search);
                //var spans = parserA.InputLexer.TransposeSpans(lexer1.CollectionInnerSpans);
                //parserA.BuildAllTokensForLexer(spans);
                parserA.InputSize = text1.Replace("\r", "").Replace("\n", "").Length;
                parserA.ReportProgress += ParserA_ReportProgress;
                var parseResult = parserA.Parse(lexer1, sequenceName: "htmlTag", showOnConsole: false);

                return parseResult;
            });

            //t.Wait();
            //Console.WriteLine("Matched: " + t.Result.Matched);

            Lexer lexer = new Lexer(defaultConfiguration, userConfiguration);
            Parser parser = new Parser(lexer, userConfiguration);

            Task t1 = Task.Run(() =>
            {
                string someRule = "searchRule = 'God ', letters;"; // <- Add repetition for sequences/tokens
                parser.AddEBNFRule(someRule);
                string bibleText = @"
                    1 In the beginning God created the heaven and the earth. 
                    2 And the earth was without form, and void; and darkness was upon the face of the deep. And the Spirit of God moved upon the face of the waters.
                    The First Day: Light
                    3 And God said, Let there be light: and there was light. 
                    4 And God saw the light, that it was good: and God divided the light from the darkness. 
                    5 And God called the light Day, and the darkness he called Night. And the evening and the morning were the first day.
                    The Second Day: Firmament
                    6 And God said, Let there be a firmament in the midst of the waters, and let it divide the waters from the waters. 
                    7 And God made the firmament, and divided the waters which were under the firmament from the waters which were above the firmament: and it was so.";
                var search1 = parser.Search(bibleText, lexer, sequenceName: "searchRule");
                foreach (var item in search1.Results)
                {
                    System.Diagnostics.Debug.WriteLine(item.InnerResultsText);
                }
            });

            Console.WriteLine("Adding grammar rules for SQL");
            string sqlGrammar = @"
                sqlIdentifier = [underscore], letters, { letters|hyphen|underscore }, [digits];
                sqlAlias = [whitespaces], ##'as', [whitespaces], sqlIdentifier, [whitespaces];
                sqlIdentifierListPart = [whitespaces], comma, [whitespaces], sqlIdentifier, [sqlAlias], [whitespaces];
                sqlIdentifierList = [whitespaces], sqlIdentifier, [whitespaces, sqlAlias], [whitespaces], {sqlIdentifierListPart};
                sqlSelect = ##'select', sqlIdentifierList, ##'from', sqlIdentifierList;";
            parser.AddEBNFGrammar(sqlGrammar.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries));

            Console.WriteLine("Adding grammar rules for HTML");
            string htmlGrammar = @"
                htmlIdentifier = letter, { letter | digit | hyphen | underscore };
                htmlAttribute = whitespaces, htmlIdentifier, { [whitespaces], ""="", [whitespaces], ebnfTerminalDoubleQuote };
                htmlTagName = htmlIdentifier;
                htmlOpenTag = ""<"", htmlTagName, { htmlAttribute }, [whitespaces], "">"";
                htmlOpenAndCloseTag = ""<"", htmlTagName, { htmlAttribute }, [whitespaces], ""/>"";
                htmlOpenTag1 = ""&lt;"", htmlTagName, { htmlAttribute }, [whitespaces], ""&gt;"";
                htmlCloseTag = ""</"", htmlTagName, "">"";
                htmlCloseTag1 = ""&lt;/"", htmlTagName, ""&gt;"";
                htmlInnerTagText = %% letters|spaces|digits|whitespaces|semicolon|underscore|equals %%;
                htmlTag = htmlOpenTag, {htmlInnerTagText}, {htmlTag}, {htmlInnerTagText}, htmlCloseTag;
                htmlTagNoInnerTag = htmlOpenTag, {htmlInnerTagText}, htmlCloseTag;
                htmlTagSearch = htmlOpenTag|htmlCloseTag|htmlOpenAndCloseTag;";
            parser.AddEBNFGrammar(htmlGrammar.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries));

            Console.WriteLine("Adding grammar rules for CSS");
            string cssGrammar = @"
                cssSelectorName = letters, { letters | hyphen | digits };
                cssClass = period, cssSelectorName;
                cssSubClass = cssClass, cssClass;
                cssClassDescendant = cssClass, whitespace, cssClass;
                cssID = asterisk;
                cssID = hashMark, cssSelectorName;
                cssElement = cssSelectorName;
                cssElementWithClass = cssElement, cssClass;
                cssElementList = cssElement, %% comma, [whitespace], cssElement %%;
                cssElementInsideList = cssElement, %% whitespace, cssElement %%;
                cssElementDescendantList = cssElement, %% [whitespace], greaterThan, [whitespace], cssElement %%;
                cssElementPlusList = cssElement, %% [whitespace], plus, [whitespace], cssElement %%;
                cssElementTildeList = cssElement, %% [whitespace], tilde, [whitespace], cssElement %%;
                cssSelectors = cssSelectorName | cssClass | cssSubClass | cssClassDescendant | cssAll | cssID | cssElement | cssElementWithClass | cssElementList | cssElementInsideList | cssElementDescendantList | cssElementPlusList | cssElementTildeList;
                cssString = { letters | digits | parenthesisOpen | parenthesisClose | hashMark | colon | forwardSlash | period | comma | strEscapedQuote | strEscapedDoubleQuote };
                cssStringDblQuote = doubleQuote, cssString, doubleQuote;
                cssStringQuote = quote, cssString, quote;
                cssStringLiteral = cssStringDblQuote|cssStringQuote;
                cssColorIdentifier = hashMark, ((hexadecimal, hexadecimal, hexadecimal, hexadecimal, hexadecimal, hexadecimal)|(hexadecimal, hexadecimal, hexadecimal)) ;
                cssValueLiteral = letters|digits;
                cssLiteral = cssStringLiteral|cssValueLiteral|cssColorIdentifier;
                cssPropertyName = letters, { letters|hyphen|digits };
                cssProperty = [whitespaces], cssPropertyName, [whitespaces], colon, [whitespaces], cssLiteral, [whitespaces], semicolon, [whitespaces];
                cssCodeBlock = [whitespaces], braceOpen, [whitespaces], %% cssProperty, [whitespaces] %%, braceClose;
                cssStatement = cssSelectors, %% whitespaces %%, cssCodeBlock;
                cssStatements = [whitespaces], %% cssStatement, [whitespaces] %%;";
            parser.AddEBNFGrammar(cssGrammar.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries));

            Console.WriteLine("Adding grammar rules for JS");
            string jsGrammar = File.ReadAllText("Configuration\\EBNFJavaScript.txt");
            parser.AddEBNFGrammar(jsGrammar.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries));

            Console.WriteLine("Adding grammar rules for Math Equations");
            string mathGrammar = @"
                mathNegative = hyphen;
                mathDecimal = period;
                mathNum = [mathNegative], digits, [ mathDecimal, digits ];
                mathAdd = plus;
                mathSubtract = hyphen;
                mathMultiply = asterisk;
                mathDivide = forwardSlash;
                mathFunction = 'sqrt'|'cos'|'sin'|'tan';
                mathParentheses = [mathFunction], parenthesisOpen, &mathExpr, parenthesisClose;
                mathFactor = (mathNum | mathParentheses);
                mathTerm = mathFactor, { (mathMultiply, mathFactor) | (mathDivide, mathFactor) };
                mathExpr = mathTerm, { (mathAdd, mathTerm) | (mathSubtract, mathTerm) };";
            parser.AddEBNFGrammar(mathGrammar.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries));

            Console.WriteLine("Adding grammar rules for Pascal");
            string pascalGrammar = @"
                pascalString = ebnfTerminal;
                pascalLiteral = number|pascalString|identifier;
                pascalAssignment = [whitespaces], identifier, [whitespaces], ':=', [whitespaces], pascalLiteral, semicolon;
                pascalRule = pascalAssignment;
                pascalRules = [whitespaces], ##'PROGRAM', whitespaces, ##'BEGIN', whitespaces, { pascalRule }, [whitespaces], ##'END', semicolon, [whitespaces];";
            parser.AddEBNFGrammar(pascalGrammar.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries));

            string inputSql = "Select column1, column2 as Hello from _yay";
            var resultSql = parser.Parse(inputSql, sequenceName: "sqlSelect", showOnConsole: false);
            Console.WriteLine();
            if (resultSql.Matched)
            {
                Console.Write("SQL Example: ");
                SqlConsoleWalker walker = new SqlConsoleWalker(resultSql.Results[0]);
                Console.WriteLine();
                Console.WriteLine();
            }
            Console.ForegroundColor = ConsoleColor.Gray;

            string inputHtml = "<html><head><title>Title</title><script type=\"text/javascript\" src=\"someFile.js\"></script></head><body><h2 selected>Helloooo hi</h2><div class=\"someClass\">Here is <span>some</span> text</div></body></html>";
            var resultHtml = parser.Parse(inputHtml, sequenceName: "htmlTag", showOnConsole: false, maxSlidingWindow: 1024);
            if (resultHtml.Matched)
            {
                Console.Write("HTML Example: ");
                HtmlConsoleWalker walker2 = new HtmlConsoleWalker(resultHtml.Results[0]);
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine();
            }
            Console.ForegroundColor = ConsoleColor.Gray;

            string inputCss = "body { background-color: #512fff; margin-left: auto; color: '#123456'; } h2 { color: 'blue'; }";
            var resultCss = parser.Parse(inputCss, sequenceName: "cssStatements", showOnConsole: false);
            Console.WriteLine();
            if (resultCss.Matched)
            {
                Console.Write("CSS Example: ");
                CssConsoleWalker walker = new CssConsoleWalker(resultCss.Results[0]);
            }
            Console.ForegroundColor = ConsoleColor.Gray;

            string inputCalc = "((3*2.5)+1.2)+((2*7*5)*12.5)+5";
            var resultCalc = parser.Parse(inputCalc, sequenceName: "mathExpr", showOnConsole: false);
            Console.WriteLine();
            Console.WriteLine();
            if (resultCalc.Matched)
            {
                Console.Write("Calculation Example: ");
                CalculatorConsoleWalker walker = new CalculatorConsoleWalker(resultCalc.Results[0]);

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
                        //Console.WriteLine($"{levels}Group:{input1.GroupName}, Name:{input1.Name}, Inner:{input1.InnerResultsText}, Eval:{input1.EvaluationResult}");
                        foreach (var g in input1.InnerResults)
                        {
                            checkGroups(g, level + 1);
                        }
                        return null;
                    });

                    checkGroups(input, 0);

                    var exprs = input.GetDescendantsOfType(new string[] { "mathExpr" });

                    bool done = false;
                    int idx = 0;
                    while (!done)
                    {
                        //Console.WriteLine("Evaluating Iteration " + idx);

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
                                //Console.WriteLine($"Number:{num}");
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

                        //Console.WriteLine(inputCalc);
                        checkGroups(input, 0);
                        if (input.InnerResults.Count == 0)
                        {
                            done = true;
                        }
                        idx++;
                    }
                    Console.WriteLine();

                    //Program.exprFunc(exprs.Last());

                    //Console.WriteLine(inputCalc);

                    return new Parser.EvaluationResult()
                    {
                        EvaluationText = input.InnerResultsText,
                        EvaluationType = typeof(double),
                        EvaluationValue = double.Parse(input.InnerResultsText)
                    };
                });

                results1[0].EvaluationFunction = funcEvaluate;
                results1[0].EvaluationResult = results1[0].EvaluationFunction(results1[0]);

                Console.Write("Result: ");
                EvaluationResultConsoleWalker walker1 = new EvaluationResultConsoleWalker(results1[0].EvaluationResult);
                //ParserResultWalker walker3 = new ParserResultWalker(results1[0], showOnConsoleDefault: true, filterOutEBNF: true);
                //walker3.Visit();
            }
            Console.ForegroundColor = ConsoleColor.Gray;

            string inputJs = "var i = \"Hello\"; var x=5;";
            var resultJs = parser.Search(inputJs, sequenceName: "jsVariable");

            string inputPascal = "PROGRAM BEGIN a:=5; b1:='Hurrooo'; b2:=\"Yay\"; b2 := a; END;";
            var resultPascal = parser.Parse(inputPascal, sequenceName: "pascalRules");

            Console.WriteLine();
            CanReport = true;

            //string input1 = string.Join(Environment.NewLine, search);
            //Console.WriteLine(input1.Substring(0, parserA.MaxParseIndex));

            //watch.Stop();
            //(long, long, TimeSpan) elapsed = (watch.ElapsedMilliseconds, watch.ElapsedTicks, watch.Elapsed);
            //Console.WriteLine(elapsed.Item1);

            //LexerResultWalker lWalker = new LexerResultWalker(lexer1);

            //string search = "<script></script><do><you><like></pina><collada>";
            //var resultsSearch = parserA.Search(search, sequenceName: "htmlTagSearch", showOnConsole: false);

            //Task.WaitAll(lexer1.OrganizableSpans);
            t.Wait();
            Console.WriteLine("Matched: " + t.Result.Matched);
            HtmlConsoleWalker htmlConsoleWalker = new HtmlConsoleWalker(t.Result.Results[0]);
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        static int PercentDone = 0;
        static bool CanReport = false;
        private static void ParserA_ReportProgress(object sender, Parser.ReportProgressArgs e)
        {
            if (CanReport)
            {
                if ((int)e.Percent / 30 != PercentDone)
                {
                    Console.WriteLine($"Parsing Document %: {e.Percent.ToString("N2")}, {e.InputPosition}/{e.InputSize}");
                    PercentDone = (int)e.Percent / 30;
                }
            }
        }
    }
}