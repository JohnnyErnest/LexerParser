# LexerParser

![Screenshot](https://raw.githubusercontent.com/JohnnyErnest/LexerParser/main/ParserImage.png)
**Figure 1**) An example use case of LexerParser, some HTML syntax being processed via EBNF rules to syntax highlight a user entered HTML source code document.

LexerParser is a C# Lexical Analyzer and Parser for text that uses a JSON configuration as well as partial Extended Backus-Naur Form support (https://en.wikipedia.org/wiki/Extended_Backus%E2%80%93Naur_form) for parsing context-free grammars expressed in EBNF. You can then enter text strings to be parsed by the resulting Parser and retrieve an AST node tree of ParserResults.

You might use LexerParser in your own programming projects for things like syntax highlighting a code editor document, parsing an HTML document into various Document Object Model components, building the parsing rules for a programming language, building an expression calculator, parsing an SQL query, parsing a style sheet of CSS rules into objects, and anywhere you need a lexical analyzer and parser.

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

**Output of Example Usage**:

```
Adding EBNF rule:htmlAttribute
Adding EBNF rule:htmlOpenTag
Adding EBNF rule:htmlCloseTag
Adding EBNF rule:htmlInnerTagText
Adding EBNF rule:htmlTag
Node:htmlTag, Start:0, InnerText:<html><head><title>Title</title></head><body><h2>Helloooo hi</h2><div>Here is <span>some</span> text</div></body></html>
 Node:htmlTag:::rule_main_block:0:0, Start:0, InnerText:<html><head><title>Title</title></head><body><h2>Helloooo hi</h2><div>Here is <span>some</span> text</div></body></html>
  Node:htmlOpenTag, Start:0, InnerText:<html>
   Node:htmlOpenTag:::rule_main_block:0:0, Start:0, InnerText:<html>
     Token: strhtmlOpenTag:::ebnfTerminal:11:2, Start:0, Text:<
    Node:identifier, Start:1, InnerText:html
      Token: letter, Start:1, Text:h
      Token: letter, Start:2, Text:t
      Token: letter, Start:3, Text:m
      Token: letter, Start:4, Text:l
     Token: strhtmlOpenTag:::ebnfTerminal:13:0, Start:5, Text:>
  Node:htmlTag:::rhsBlockBrace:11:1, Start:6, InnerText:<head><title>Title</title></head>
   Node:htmlTag, Start:6, InnerText:<head><title>Title</title></head>
    Node:htmlTag:::rule_main_block:0:0, Start:6, InnerText:<head><title>Title</title></head>
     Node:htmlOpenTag, Start:6, InnerText:<head>
      Node:htmlOpenTag:::rule_main_block:0:0, Start:6, InnerText:<head>
        Token: strhtmlOpenTag:::ebnfTerminal:11:2, Start:6, Text:<
       Node:identifier, Start:7, InnerText:head
         Token: letter, Start:7, Text:h
         Token: letter, Start:8, Text:e
         Token: letter, Start:9, Text:a
         Token: letter, Start:10, Text:d
        Token: strhtmlOpenTag:::ebnfTerminal:13:0, Start:11, Text:>
     Node:htmlTag:::rhsBlockBrace:11:1, Start:12, InnerText:<title>Title</title>
      Node:htmlTag, Start:12, InnerText:<title>Title</title>
       Node:htmlTag:::rule_main_block:0:0, Start:12, InnerText:<title>Title</title>
        Node:htmlOpenTag, Start:12, InnerText:<title>
         Node:htmlOpenTag:::rule_main_block:0:0, Start:12, InnerText:<title>
           Token: strhtmlOpenTag:::ebnfTerminal:11:2, Start:12, Text:<
          Node:identifier, Start:13, InnerText:title
            Token: letter, Start:13, Text:t
            Token: letter, Start:14, Text:i
            Token: letter, Start:15, Text:t
            Token: letter, Start:16, Text:l
            Token: letter, Start:17, Text:e
           Token: strhtmlOpenTag:::ebnfTerminal:13:0, Start:18, Text:>
           ... and so on ...
```
