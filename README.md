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

**Output of Example Usage**:

```
Adding EBNF rule:htmlAttribute
Adding EBNF rule:htmlOpenTag
Adding EBNF rule:htmlCloseTag
Adding EBNF rule:htmlInnerTagText
Adding EBNF rule:htmlTag
Node: htmlTag, InnerText: <html><head><title>Title</title></head><body><h2>Helloooo hi</h2><div>Here is <span>some</span> text</div></body></html>
 Node: htmlTag:::rule_main_block:0:0, InnerText: <html><head><title>Title</title></head><body><h2>Helloooo hi</h2><div>Here is <span>some</span> text</div></body></html>
  Node: htmlOpenTag, InnerText: <html>
   Node: htmlOpenTag:::rule_main_block:0:0, InnerText: <html>
     Token: strhtmlOpenTag:::ebnfTerminal:11:2 - <
    Node: identifier, InnerText: html
      Token: letter - h
      Token: letter - t
      Token: letter - m
      Token: letter - l
     Token: strhtmlOpenTag:::ebnfTerminal:13:0 - >
  Node: htmlTag:::rhsBlockBrace:11:1, InnerText: <head><title>Title</title></head>
   Node: htmlTag, InnerText: <head><title>Title</title></head>
    Node: htmlTag:::rule_main_block:0:0, InnerText: <head><title>Title</title></head>
     Node: htmlOpenTag, InnerText: <head>
      Node: htmlOpenTag:::rule_main_block:0:0, InnerText: <head>
        Token: strhtmlOpenTag:::ebnfTerminal:11:2 - <
       Node: identifier, InnerText: head
         Token: letter - h
         Token: letter - e
         Token: letter - a
         Token: letter - d
        Token: strhtmlOpenTag:::ebnfTerminal:13:0 - >
     Node: htmlTag:::rhsBlockBrace:11:1, InnerText: <title>Title</title>
      Node: htmlTag, InnerText: <title>Title</title>
       Node: htmlTag:::rule_main_block:0:0, InnerText: <title>Title</title>
        Node: htmlOpenTag, InnerText: <title>
         Node: htmlOpenTag:::rule_main_block:0:0, InnerText: <title>
           Token: strhtmlOpenTag:::ebnfTerminal:11:2 - <
          Node: identifier, InnerText: title
            Token: letter - t
            Token: letter - i
            Token: letter - t
            Token: letter - l
            Token: letter - e
           Token: strhtmlOpenTag:::ebnfTerminal:13:0 - >
        Node: htmlTag:::rhsBlockBrace:11:0, InnerText: Title
         Node: htmlInnerTagText, InnerText: Title
          Node: htmlInnerTagText:::rule_main_block:0:0, InnerText: Title
           Node: htmlInnerTagText:::rhsBlockDoublePercent:9:0, InnerText: Title
             Token: letters - Title
        Node: htmlCloseTag, InnerText: </title>
         Node: htmlCloseTag:::rule_main_block:0:0, InnerText: </title>
           Token: strhtmlCloseTag:::ebnfTerminal:11:0 - </
          Node: identifier, InnerText: title
            Token: letter - t
            Token: letter - i
            Token: letter - t
            Token: letter - l
            Token: letter - e
           Token: strhtmlCloseTag:::ebnfTerminal:13:0 - >
     Node: htmlCloseTag, InnerText: </head>
      Node: htmlCloseTag:::rule_main_block:0:0, InnerText: </head>
        Token: strhtmlCloseTag:::ebnfTerminal:11:0 - </
       Node: identifier, InnerText: head
         Token: letter - h
         Token: letter - e
         Token: letter - a
         Token: letter - d
        Token: strhtmlCloseTag:::ebnfTerminal:13:0 - >
  Node: htmlTag:::rhsBlockBrace:11:1, InnerText: <body><h2>Helloooo hi</h2><div>Here is <span>some</span> text</div></body>
   Node: htmlTag, InnerText: <body><h2>Helloooo hi</h2><div>Here is <span>some</span> text</div></body>
    Node: htmlTag:::rule_main_block:0:0, InnerText: <body><h2>Helloooo hi</h2><div>Here is <span>some</span> text</div></body>
     Node: htmlOpenTag, InnerText: <body>
      Node: htmlOpenTag:::rule_main_block:0:0, InnerText: <body>
        Token: strhtmlOpenTag:::ebnfTerminal:11:2 - <
       Node: identifier, InnerText: body
         Token: letter - b
         Token: letter - o
         Token: letter - d
         Token: letter - y
        Token: strhtmlOpenTag:::ebnfTerminal:13:0 - >
     Node: htmlTag:::rhsBlockBrace:11:1, InnerText: <h2>Helloooo hi</h2>
      Node: htmlTag, InnerText: <h2>Helloooo hi</h2>
       Node: htmlTag:::rule_main_block:0:0, InnerText: <h2>Helloooo hi</h2>
        Node: htmlOpenTag, InnerText: <h2>
         Node: htmlOpenTag:::rule_main_block:0:0, InnerText: <h2>
           Token: strhtmlOpenTag:::ebnfTerminal:11:2 - <
          Node: identifier, InnerText: h2
            Token: letter - h
            Token: digit - 2
           Token: strhtmlOpenTag:::ebnfTerminal:13:0 - >
        Node: htmlTag:::rhsBlockBrace:11:0, InnerText: Helloooo hi
         Node: htmlInnerTagText, InnerText: Helloooo hi
          Node: htmlInnerTagText:::rule_main_block:0:0, InnerText: Helloooo hi
           Node: htmlInnerTagText:::rhsBlockDoublePercent:9:0, InnerText: Helloooo
             Token: letters - Helloooo
           Node: htmlInnerTagText:::rhsBlockDoublePercent:9:0, InnerText:
             Token: spaces -
           Node: htmlInnerTagText:::rhsBlockDoublePercent:9:0, InnerText: hi
             Token: letters - hi
        Node: htmlCloseTag, InnerText: </h2>
         Node: htmlCloseTag:::rule_main_block:0:0, InnerText: </h2>
           Token: strhtmlCloseTag:::ebnfTerminal:11:0 - </
          Node: identifier, InnerText: h2
            Token: letter - h
            Token: digit - 2
           Token: strhtmlCloseTag:::ebnfTerminal:13:0 - >
     Node: htmlTag:::rhsBlockBrace:11:1, InnerText: <div>Here is <span>some</span> text</div>
      Node: htmlTag, InnerText: <div>Here is <span>some</span> text</div>
       Node: htmlTag:::rule_main_block:0:0, InnerText: <div>Here is <span>some</span> text</div>
        Node: htmlOpenTag, InnerText: <div>
         Node: htmlOpenTag:::rule_main_block:0:0, InnerText: <div>
           Token: strhtmlOpenTag:::ebnfTerminal:11:2 - <
          Node: identifier, InnerText: div
            Token: letter - d
            Token: letter - i
            Token: letter - v
           Token: strhtmlOpenTag:::ebnfTerminal:13:0 - >
        Node: htmlTag:::rhsBlockBrace:11:0, InnerText: Here is
         Node: htmlInnerTagText, InnerText: Here is
          Node: htmlInnerTagText:::rule_main_block:0:0, InnerText: Here is
           Node: htmlInnerTagText:::rhsBlockDoublePercent:9:0, InnerText: Here
             Token: letters - Here
           Node: htmlInnerTagText:::rhsBlockDoublePercent:9:0, InnerText:
             Token: spaces -
           Node: htmlInnerTagText:::rhsBlockDoublePercent:9:0, InnerText: is
             Token: letters - is
           Node: htmlInnerTagText:::rhsBlockDoublePercent:9:0, InnerText:
             Token: spaces -
        Node: htmlTag:::rhsBlockBrace:11:1, InnerText: <span>some</span>
         Node: htmlTag, InnerText: <span>some</span>
          Node: htmlTag:::rule_main_block:0:0, InnerText: <span>some</span>
           Node: htmlOpenTag, InnerText: <span>
            Node: htmlOpenTag:::rule_main_block:0:0, InnerText: <span>
              Token: strhtmlOpenTag:::ebnfTerminal:11:2 - <
             Node: identifier, InnerText: span
               Token: letter - s
               Token: letter - p
               Token: letter - a
               Token: letter - n
              Token: strhtmlOpenTag:::ebnfTerminal:13:0 - >
           Node: htmlTag:::rhsBlockBrace:11:0, InnerText: some
            Node: htmlInnerTagText, InnerText: some
             Node: htmlInnerTagText:::rule_main_block:0:0, InnerText: some
              Node: htmlInnerTagText:::rhsBlockDoublePercent:9:0, InnerText: some
                Token: letters - some
           Node: htmlCloseTag, InnerText: </span>
            Node: htmlCloseTag:::rule_main_block:0:0, InnerText: </span>
              Token: strhtmlCloseTag:::ebnfTerminal:11:0 - </
             Node: identifier, InnerText: span
               Token: letter - s
               Token: letter - p
               Token: letter - a
               Token: letter - n
              Token: strhtmlCloseTag:::ebnfTerminal:13:0 - >
        Node: htmlTag:::rhsBlockBrace:11:2, InnerText:  text
         Node: htmlInnerTagText, InnerText:  text
          Node: htmlInnerTagText:::rule_main_block:0:0, InnerText:  text
           Node: htmlInnerTagText:::rhsBlockDoublePercent:9:0, InnerText:
             Token: spaces -
           Node: htmlInnerTagText:::rhsBlockDoublePercent:9:0, InnerText: text
             Token: letters - text
        Node: htmlCloseTag, InnerText: </div>
         Node: htmlCloseTag:::rule_main_block:0:0, InnerText: </div>
           Token: strhtmlCloseTag:::ebnfTerminal:11:0 - </
          Node: identifier, InnerText: div
            Token: letter - d
            Token: letter - i
            Token: letter - v
           Token: strhtmlCloseTag:::ebnfTerminal:13:0 - >
     Node: htmlCloseTag, InnerText: </body>
      Node: htmlCloseTag:::rule_main_block:0:0, InnerText: </body>
        Token: strhtmlCloseTag:::ebnfTerminal:11:0 - </
       Node: identifier, InnerText: body
         Token: letter - b
         Token: letter - o
         Token: letter - d
         Token: letter - y
        Token: strhtmlCloseTag:::ebnfTerminal:13:0 - >
  Node: htmlCloseTag, InnerText: </html>
   Node: htmlCloseTag:::rule_main_block:0:0, InnerText: </html>
     Token: strhtmlCloseTag:::ebnfTerminal:11:0 - </
    Node: identifier, InnerText: html
      Token: letter - h
      Token: letter - t
      Token: letter - m
      Token: letter - l
     Token: strhtmlCloseTag:::ebnfTerminal:13:0 - >
```
