# LexerParser
A C# Lexical Analyzer and Parser with JSON configuration and partial Extended Backus-Naur Form support (https://en.wikipedia.org/wiki/Extended_Backus%E2%80%93Naur_form) for parsing context-free grammars expressed in EBNF, or more directly from a JSON configuration, from a user input text string or a document such as Html, CSS, SQL, expression calculators, etc. EBNF configurations will generally be smaller than JSON configurations, as the Lexer/Parser generates Rules/Sequences from EBNF at runtime into the same base objects that are built from the included JSON configurations.

Rather than building a static code file beforehand from a static set of rules from a third party tool, LexerParser lets you add and remove Lexer Rules and Parser Sequences dynamically at runtime.

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

![Screenshot](https://raw.githubusercontent.com/JohnnyErnest/LexerParser/main/ParserImage.png)

Output is an indicator whether the input matched a parsing rule, as well as an Abstract Syntax Tree of ParserResult nodes that can be walked. Parser rules and lexical tokens can be added or removed dynamically at runtime for adding or removing grammars.

In laymen terms, you can parse an HTML to a syntax tree of nodes and analyze it, as in the usage example above. If you add the rules for CSS, SQL, and so on, you can do the same with the respective languages.

Note that adding EBNF rules ay runtime is a bit costly on performance currently because it reparses internal parts of text while adding subnodes such as "( nodes, nodes, nodes )", "[ nodes, nodes, nodes ]", and "{ nodes, nodes, nodes }" while adding subrules and replacing text via substitution internally, a faster version is in the works. 

Also note, in addition to EBNF operators { ... } for optional repeating blocks, and [ ... ] for optional rule blocks that appear zero or one times, you can also use %% ... %% to create mandatory repeating blocks that must appear at least once as an extension to Extended BNF. Also when adding EBNF rules, there is normally a lookup to see if the Lexer Rule or Parser Sequence exists, if not, it is added to a list of Unknowns and is unused. You can override this behavior with putting @ in front of the Lexer Rule or & in front of the Parser Sequence if you know ahead of time that the Rule or Sequence will be added later.

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
