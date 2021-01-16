# LexerParser
A C# Lexical Analyzer and Parser with JSON configuration and partial Extended Backus-Naur Form support (https://en.wikipedia.org/wiki/Extended_Backus%E2%80%93Naur_form) for parsing context-free grammars expressed in EBNF, or more directly from JSON configuration, from a user input text string or a document such as Html, CSS, SQL, expression calculators, etc.

**Usage**:

```
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
    if (result.Matched)
    {
        SyntaxWalker walker = new SyntaxWalker(result.Results[0]);
        walker.Visit();
    }    
}
```

Output is an indicator whether the input matched a parsing rule, as well as an Abstract Syntax Tree of ParserResult nodes that can be walked. Parser rules and lexical tokens can be added or removed dynamically at runtime for adding or removing grammars.

In laymen terms, you can parse an HTML to a syntax tree of nodes and analyze it, as in the usage example above. If you add the rules for CSS, SQL, and so on, you can do the same with the respective languages.

Note that adding EBNF rules is a bit costly on performance because it currently reparses internal parts of text while adding subnodes such as "( nodes, nodes, nodes )", "[ nodes, nodes, nodes ]", and "{ nodes, nodes, nodes }" while adding subrules and replacing text internally. However, you could save a custom Lexer.Rules and Parser.Sequences to a serialized JSON and load them once at runtime if you have a set grammar that you want to pre-compile ahead of time and use for subsequent runs, which is most often the case. However, the flexibility is there to add EBNF rules directly at runtime for any specific use case you might have.

Also note, in addition to EBNF operators { ... } for optional repeating blocks, and [ ... ] for optional rule blocks, you can also use %% ... %% for mandatory repeating blocks.

**To Do**

- Unit Testing
- Nuget Packaging
- Variable name capture support in EBNF, this is currently available in JSON with "varName" on sections you want to name

**Output**:

```
Adding EBNF rule:htmlAttribute
Adding EBNF rule:htmlOpenTag
Adding EBNF rule:htmlCloseTag
Adding EBNF rule:htmlInnerTagText
Adding EBNF rule:htmlTag
htmlTag
 htmlTag:::rule_main_block:0:0
  htmlOpenTag
   htmlOpenTag:::rule_main_block:0:0
     strhtmlOpenTag:::ebnfTerminal:11:2 - <
    identifier
      letter - h
      letter - t
      letter - m
      letter - l
     strhtmlOpenTag:::ebnfTerminal:13:0 - >
  htmlTag:::rhsBlockBrace:11:1
   htmlTag
    htmlTag:::rule_main_block:0:0
     htmlOpenTag
      htmlOpenTag:::rule_main_block:0:0
        strhtmlOpenTag:::ebnfTerminal:11:2 - <
       identifier
         letter - h
         letter - e
         letter - a
         letter - d
        strhtmlOpenTag:::ebnfTerminal:13:0 - >
     htmlTag:::rhsBlockBrace:11:1
      htmlTag
       htmlTag:::rule_main_block:0:0
        htmlOpenTag
         htmlOpenTag:::rule_main_block:0:0
           strhtmlOpenTag:::ebnfTerminal:11:2 - <
          identifier
            letter - t
            letter - i
            letter - t
            letter - l
            letter - e
           strhtmlOpenTag:::ebnfTerminal:13:0 - >
        htmlTag:::rhsBlockBrace:11:0
         htmlInnerTagText
          htmlInnerTagText:::rule_main_block:0:0
           htmlInnerTagText:::rhsBlockDoublePercent:9:0
             letters - Title
        htmlCloseTag
         htmlCloseTag:::rule_main_block:0:0
           strhtmlCloseTag:::ebnfTerminal:11:0 - </
          identifier
            letter - t
            letter - i
            letter - t
            letter - l
            letter - e
           strhtmlCloseTag:::ebnfTerminal:13:0 - >
     htmlCloseTag
      htmlCloseTag:::rule_main_block:0:0
        strhtmlCloseTag:::ebnfTerminal:11:0 - </
       identifier
         letter - h
         letter - e
         letter - a
         letter - d
        strhtmlCloseTag:::ebnfTerminal:13:0 - >
  htmlTag:::rhsBlockBrace:11:1
   htmlTag
    htmlTag:::rule_main_block:0:0
     htmlOpenTag
      htmlOpenTag:::rule_main_block:0:0
        strhtmlOpenTag:::ebnfTerminal:11:2 - <
       identifier
         letter - b
         letter - o
         letter - d
         letter - y
        strhtmlOpenTag:::ebnfTerminal:13:0 - >
     htmlTag:::rhsBlockBrace:11:1
      htmlTag
       htmlTag:::rule_main_block:0:0
        htmlOpenTag
         htmlOpenTag:::rule_main_block:0:0
           strhtmlOpenTag:::ebnfTerminal:11:2 - <
          identifier
            letter - h
            digit - 2
           strhtmlOpenTag:::ebnfTerminal:13:0 - >
        htmlTag:::rhsBlockBrace:11:0
         htmlInnerTagText
          htmlInnerTagText:::rule_main_block:0:0
           htmlInnerTagText:::rhsBlockDoublePercent:9:0
             letters - Helloooo
           htmlInnerTagText:::rhsBlockDoublePercent:9:0
             spaces -
           htmlInnerTagText:::rhsBlockDoublePercent:9:0
             letters - hi
        htmlCloseTag
         htmlCloseTag:::rule_main_block:0:0
           strhtmlCloseTag:::ebnfTerminal:11:0 - </
          identifier
            letter - h
            digit - 2
           strhtmlCloseTag:::ebnfTerminal:13:0 - >
     htmlTag:::rhsBlockBrace:11:1
      htmlTag
       htmlTag:::rule_main_block:0:0
        htmlOpenTag
         htmlOpenTag:::rule_main_block:0:0
           strhtmlOpenTag:::ebnfTerminal:11:2 - <
          identifier
            letter - d
            letter - i
            letter - v
           strhtmlOpenTag:::ebnfTerminal:13:0 - >
        htmlTag:::rhsBlockBrace:11:0
         htmlInnerTagText
          htmlInnerTagText:::rule_main_block:0:0
           htmlInnerTagText:::rhsBlockDoublePercent:9:0
             letters - Here
           htmlInnerTagText:::rhsBlockDoublePercent:9:0
             spaces -
           htmlInnerTagText:::rhsBlockDoublePercent:9:0
             letters - is
           htmlInnerTagText:::rhsBlockDoublePercent:9:0
             spaces -
        htmlTag:::rhsBlockBrace:11:1
         htmlTag
          htmlTag:::rule_main_block:0:0
           htmlOpenTag
            htmlOpenTag:::rule_main_block:0:0
              strhtmlOpenTag:::ebnfTerminal:11:2 - <
             identifier
               letter - s
               letter - p
               letter - a
               letter - n
              strhtmlOpenTag:::ebnfTerminal:13:0 - >
           htmlTag:::rhsBlockBrace:11:0
            htmlInnerTagText
             htmlInnerTagText:::rule_main_block:0:0
              htmlInnerTagText:::rhsBlockDoublePercent:9:0
                letters - some
           htmlCloseTag
            htmlCloseTag:::rule_main_block:0:0
              strhtmlCloseTag:::ebnfTerminal:11:0 - </
             identifier
               letter - s
               letter - p
               letter - a
               letter - n
              strhtmlCloseTag:::ebnfTerminal:13:0 - >
        htmlTag:::rhsBlockBrace:11:2
         htmlInnerTagText
          htmlInnerTagText:::rule_main_block:0:0
           htmlInnerTagText:::rhsBlockDoublePercent:9:0
             spaces -
           htmlInnerTagText:::rhsBlockDoublePercent:9:0
             letters - text
        htmlCloseTag
         htmlCloseTag:::rule_main_block:0:0
           strhtmlCloseTag:::ebnfTerminal:11:0 - </
          identifier
            letter - d
            letter - i
            letter - v
           strhtmlCloseTag:::ebnfTerminal:13:0 - >
     htmlCloseTag
      htmlCloseTag:::rule_main_block:0:0
        strhtmlCloseTag:::ebnfTerminal:11:0 - </
       identifier
         letter - b
         letter - o
         letter - d
         letter - y
        strhtmlCloseTag:::ebnfTerminal:13:0 - >
  htmlCloseTag
   htmlCloseTag:::rule_main_block:0:0
     strhtmlCloseTag:::ebnfTerminal:11:0 - </
    identifier
      letter - h
      letter - t
      letter - m
      letter - l
     strhtmlCloseTag:::ebnfTerminal:13:0 - >
```
