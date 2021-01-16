# LexerParser
A C# Lexical Analyzer and Parser with JSON configuration and partial Extended Backus-Naur Form support (https://en.wikipedia.org/wiki/Extended_Backus%E2%80%93Naur_form) for parsing context-free grammars expressed in EBNF, or more directly from JSON configuration, from a user input text string or a document such as Html, CSS, SQL, expression calculators, etc.

**Usage**:

        static void Main(string[] args)
        {
            string defaultConfiguration = $"Configuration/Default.json";
            string userConfiguration = $"Configuration/ParserEBNF.json";
            Lexer lexer = new Lexer(defaultConfiguration, userConfiguration);
            Parser parser = new Parser(lexer, userConfiguration);

            string htmlAttribute = @"htmlAttribute = whitespaces, identifier, ""="", [whitespaces], ebnfTerminalDoubleQuote;";
            string htmlOpenTag = @"htmlOpenTag = ""<"", identifier, { htmlAttribute }, [whitespaces], "">"";";
            string htmlCloseTag = @"htmlCloseTag = ""</"", identifier, "">"";";
            string htmlInnerTagText = @"htmlInnerTagText = %%letters|spaces|digits|whitespaces|semicolon|underscore|equals%%;";
            string htmlTag = @"htmlTag = htmlOpenTag, {htmlInnerTagText}, {htmlTag}, {htmlInnerTagText}, htmlCloseTag;";

            parser.AddEBNFRule(htmlAttribute);
            parser.AddEBNFRule(htmlOpenTag);
            parser.AddEBNFRule(htmlCloseTag);
            parser.AddEBNFRule(htmlInnerTagText);
            parser.AddEBNFRule(htmlTag);

            string inputHtml = "<html><head><title>Title</title></head><body><h2>Helloooo hi</h2><div>Here is <span>some</span> text</div></body></html>";
            var result = parser.Parse(inputHtml, sequenceName: "htmlTag", showOnConsole: false);
        }

Output is an indicator whether the input matched a parsing rule, as well as an Abstract Syntax Tree of ParserResult nodes that can be walked, SyntaxWalker visitor pattern coming shortly. Parser rules and lexical tokens can be added or removed dynamically at runtime for adding or removing grammars.

In laymen terms, you can parse an HTML to a syntax tree of nodes and analyze it, as in the usage example above. If you add the rules for CSS, SQL, and so on, you can do the same with the respective languages.

Note that adding EBNF rules is a bit costly on performance because it currently reparses internal parts of text while adding subnodes such as "( nodes, nodes, nodes )", "[ nodes, nodes, nodes ]", and "{ nodes, nodes, nodes }" while adding subrules and replacing text internally. **However**, you could save a custom Lexer.Rules and Parser.Sequences to a serialized JSON and load them once at runtime if you have a set grammar that you want to pre-compile ahead of time and use for subsequent runs, which is most often the case. However, the flexibility is there to add EBNF rules directly at runtime for any specific use case you might have.

Also note, in addition to EBNF operators { ... } for optional repeating blocks, and [ ... ] for optional rule blocks, you can also use %% ... %% for mandatory repeating blocks.

**To Do**

- SyntaxWalker visitor pattern
- Unit Testing
- Nuget Packaging
- Variable name capture support in EBNF, this is currently available in JSON with "varName" on sections you want to name
