# LexerParser

![](https://raw.githubusercontent.com/JohnnyErnest/LexerParser/main/LexerParser2.png")

**Figure 1**) An example use case of LexerParser, some examples of HTML, CSS, and SQL syntax being processed via EBNF rules to syntax highlight user entered texts, and also a mathematical equation being expressed as a syntax node tree and evaluated.

LexerParser is a C# Lexical Analyzer and Parser for text that uses a JSON configuration as well as partial Extended Backus-Naur Form support (https://en.wikipedia.org/wiki/Extended_Backus%E2%80%93Naur_form) for parsing Context-Free Grammars expressed in EBNF. You can then enter text strings to be parsed by the resulting Parser and retrieve an AST node tree of ParserResults. You can also take parsed results and use them in evaluations, for example, the included expression calculator example parses an input expression and evaluates the end result, giving you the mathematical answer of the input calculation. 

You might use LexerParser in your own programming projects for things like syntax highlighting a code editor document, parsing an HTML document into various Document Object Model components, building the parsing rules for a programming language, building an expression calculator, parsing an SQL query, parsing a style sheet of CSS rules into objects, and anywhere you might need a lexical analyzer and parser.

Rather than building a static code file beforehand from a static set of rules from a third party tool, LexerParser lets you add and remove Lexer Rules and Parser Sequences dynamically at runtime. Say that you want to build an expression calculator, you feed in the rules to the Lexer and Parser and then feed in expression statements as strings to parse, such as "3*(2+1)" and other expression statements.

**Example Usage**:

```
static void Main(string[] args)
{
    string defaultConfiguration = $"Configuration/Default.json";
    string userConfiguration = $"Configuration/ParserEBNF.json";
    Lexer lexer = new Lexer(defaultConfiguration, userConfiguration);
    Parser parser = new Parser(lexer, userConfiguration);

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

    string inputHtml = "<html><head><title>Title</title></head><body><h2>Helloooo hi</h2><div>Here is <span>some</span> text</div></body></html>";
    var result = parser.Parse(inputHtml, sequenceName: "htmlTag", showOnConsole: false);
    if (result.Matched)
    {
        ParserResultWalker walker = new ParserResultWalker(result.Results[0], showOnConsoleDefault: true);
        walker.Visit();
        
        ConsoleWalker walker = new ConsoleWalker(result.Results[0]);
    }    
}
```
In addition to EBNF operators **{ ... }** for optional repeating blocks, and **[ ... ]** for optional rule blocks that appear zero or one times, you can also use **%% ... %%** to create mandatory repeating blocks that must appear at least once as an extension to Extended BNF. 

When adding EBNF rules, there is normally a lookup to see if the Lexer Rule or Parser Sequence exists, if not, it is added to a list of Unknowns and is unused. You can override this behavior with putting **@** in front of the Lexer Rule or **&** in front of the Parser Sequence if you know ahead of time that the Rule or Sequence will be added later.

**To Do**

- Unit Testing
- Nuget Packaging
- Variable name capture support in EBNF, this is currently available in JSON with "varName" on sections you want to name.
