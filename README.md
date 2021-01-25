# LexerParser

![](https://raw.githubusercontent.com/JohnnyErnest/LexerParser/main/LexerParser2.png)
**Figure 1**) An example use case of LexerParser, some examples of HTML, CSS, and SQL syntax being processed via EBNF rules to syntax highlight user entered texts, and also a mathematical equation being expressed as a syntax node tree and evaluated.

LexerParser is a C# Lexical Analyzer and Parser for text that uses a JSON configuration as well as partial Extended Backus-Naur Form support (https://en.wikipedia.org/wiki/Extended_Backus%E2%80%93Naur_form) for parsing Context-Free Grammars expressed in EBNF. You can then enter text strings to be parsed by the resulting Parser and retrieve an AST node tree of ParserResults. You can also take parsed results and use them in evaluations, for example, the included expression calculator example parses an input expression and evaluates the end result, giving you the mathematical answer of the input calculation. 

You might use LexerParser in your own programming projects for things like syntax highlighting a code editor document, parsing an HTML document into various Document Object Model components, building the parsing rules for a programming language, building an expression calculator, parsing an SQL query, parsing a style sheet of CSS rules into objects, and anywhere you might need a lexical analyzer and parser.

Rather than building a static code file beforehand from a static set of rules from a third party tool, LexerParser lets you add and remove Lexer Rules and Parser Sequences dynamically at runtime. Say that you want to build an expression calculator, you feed in the rules to the Lexer and Parser and then feed in expression statements as strings to parse, such as "3*(2+1)" and other expression statements.

**How do I use it?**:

- Download Visual Studio 2019 Community for free: https://visualstudio.microsoft.com/downloads/
- Download this Solution, unzip it, and load it in VS2019 Community
- Press F5 to Run while Debugging
- Enjoy
- Grab a Soda
- Then start your own new project in Visual Studio
- Then add a reference to LexerParser via right-click on the Dependencies part of your project and click Add a Reference
- See Example Usage below on how to use it, and take a look at the example console project in the LexerParser Solution

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

**Differences from EBNF**

In addition to EBNF operators **{ ... }** for optional repeating blocks, and **[ ... ]** for optional rule blocks that appear zero or one times, you can also use **%% ... %%** to create mandatory repeating blocks that must appear at least once as an extension to Extended BNF. 

When adding EBNF rules, there is normally a lookup to see if the Lexer Rule or Parser Sequence exists, if not, it is added to a list of Unknowns and is unused. You can override this behavior with putting **@** in front of the Lexer Rule or **&** in front of the Parser Sequence if you know ahead of time that the Rule or Sequence will be added later.

Prepending string with **##** does a case-insensitive string search rather than case-sensitive. Example: 

```
pascalString = ebnfTerminal;
pascalLiteral = number|pascalString|identifier;
pascalAssignment = [whitespaces], identifier, [whitespaces], ':=', [whitespaces], pascalLiteral, semicolon;
pascalRule = pascalAssignment;
pascalRules = [whitespaces], ##'PROGRAM', whitespaces, ##'BEGIN', whitespaces, { pascalRule }, [whitespaces], ##'END', semicolon, [whitespaces];
```

The above searches for PROGRAM, BEGIN, and END as case-insensitive strings.

**Search Support**

**Parser.Parse** will return false on a match if the whole string does not match any sequence rule or a sequence rule you specify. **Parser.Search** on the other hand searches for each occurence of a sequence rule in a string. This allows you to use LexerParser to search large texts that are broken into sections, such as the Bible. Example:

```
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
7 And God made the firmament, and divided the waters which were under the firmament from the waters which were above the firmament: and it was so.
";
    var search1 = parser.Search(bibleText, lexer, sequenceName: "searchRule");
```

The above searches the KJV Holy Bible in Genesis 1:1-7 for the text: "'God ', letters", allowing variable results to come back. Results would be: "God created, God moved, God said, God saw, God divided, God called, God said, God made"

Keep in mind, when the Lexer runs ProcessText, it will run a cartesian product of all substrings within the input string and try to determine which rules apply to each, and assess where boundaries resides between letters, digits, whitespacing, and so on. So it may be best to break a search text into lines for example, prior to feeding them into the Lexer for optimization, but the functionality is there if you want the Lexer to process large strings.

**To Do**

- Unit Testing
- Nuget Packaging
- Variable name capture support in EBNF, this is currently available in JSON with "varName" on sections you want to name.
