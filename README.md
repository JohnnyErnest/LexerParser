# LexerParser
A C# Lexical Analyzer and Parser with JSON configuration and partial Extended Backus-Naur Form support (https://en.wikipedia.org/wiki/Extended_Backus%E2%80%93Naur_form) for parsing context-free grammars expressed in EBNF, or more directly from JSON configuration, from a user inputted text or document such as Html, CSS, SQL, expression calculators, etc.

Current Usage:

            string defaultConfig = File.ReadAllText($"Configuration/Default.json");
            dynamic defaultObj = JsonConvert.DeserializeObject<dynamic>(defaultConfig);
            JObject lexerRules = defaultObj.lexer as JObject;
            string EBNFConfig = File.ReadAllText($"Configuration/ParserEBNF.json");
            dynamic ebnfObj = JsonConvert.DeserializeObject<dynamic>(EBNFConfig);

            JObject ebnfUserLexer = ebnfObj.userLexer as JObject;
            JObject ebnfParserRules = ebnfObj.parser as JObject;
            Lexer ebnfLexer = new Lexer(lexerRules, ebnfUserLexer);
            Parser ebnfParser = new Parser(ebnfLexer, ebnfParserRules);

            string htmlAttribute = @"htmlAttribute = whitespaces, identifier, ""="", [whitespaces], ebnfTerminalDoubleQuote;";
            string htmlOpenTag = @"htmlOpenTag = ""<"", identifier, { htmlAttribute }, [whitespaces], "">"";";
            string htmlCloseTag = @"htmlCloseTag = ""</"", identifier, "">"";";
            string htmlInnerTagText = @"htmlInnerTagText = %%letters|spaces|digits|whitespaces|semicolon|underscore|equals%%;";
            string htmlTag = @"htmlTag = htmlOpenTag, {htmlInnerTagText}, {htmlTag}, {htmlInnerTagText}, htmlCloseTag;";

            ebnfParser.AddEBNFRule(htmlAttribute);
            ebnfParser.AddEBNFRule(htmlOpenTag);
            ebnfParser.AddEBNFRule(htmlCloseTag);
            ebnfParser.AddEBNFRule(htmlInnerTagText);
            ebnfParser.AddEBNFRule(htmlTag);

            string inputHtml = "<html><head><title>Title</title></head><body><h2>Helloooo hi</h2><div>Here is <span>some</span> text</div></body></html>";
            var result = ebnfParser.Parse(inputHtml, sequenceName: "htmlTag", showOnConsole: false);

Output is an indicator whether the input matched a parsing rule, as well as an Abstract Syntax Tree of ParserResult nodes that can be walked, SyntaxWalker visitor pattern coming shortly. Parser rules and lexical tokens can be added or removed dynamically at runtime for adding or removing grammars.

In laymen terms, you can parse an HTML to a syntax tree of nodes and analyze it like so.

Note that adding EBNF rules is a bit costly on performance because it currently reparses internal parts of text while adding subnodes such as "( nodes, nodes, nodes )", "[ nodes, nodes, nodes ]", and "{ nodes, nodes, nodes }" while adding subrules and replacing text internally. However, you could save Lexer.Rules and Parser.Sequences to JSON and load them once at runtime if you have a set grammar that you want to pre-compile and use for subsequent runs, which is most often the case. However, the flexibility is there to add EBNF rules directly at runtime for any specific use case you might have.
