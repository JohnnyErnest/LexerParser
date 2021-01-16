# LexerParser
A C# Lexical Analyzer and Parser with JSON configuration and partial Extended Backus-Naur Form support (https://en.wikipedia.org/wiki/Extended_Backus%E2%80%93Naur_form) for parsing context-free grammars expressed in EBNF, or more directly from a JSON configuration, from a user input text string or a document such as Html, CSS, SQL, expression calculators, etc. EBNF configurations will generally be smaller than JSON configurations, and the Lexer/Parser generates JSON configurations from EBNF at runtime.

**Example Usage**:

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
        ParserResultWalker walker = new ParserResultWalker(result.Results[0], showOnConsoleDefault: true);
        walker.Visit();
        
        ConsoleWalker walker = new ConsoleWalker(result.Results[0]);
    }    
}
```

![Screenshot](https://raw.githubusercontent.com/JohnnyErnest/LexerParser/main/ParserImage.png)

Output is an indicator whether the input matched a parsing rule, as well as an Abstract Syntax Tree of ParserResult nodes that can be walked. Parser rules and lexical tokens can be added or removed dynamically at runtime for adding or removing grammars.

In laymen terms, you can parse an HTML to a syntax tree of nodes and analyze it, as in the usage example above. If you add the rules for CSS, SQL, and so on, you can do the same with the respective languages.

Note that adding EBNF rules ay runtime is a bit costly on performance because it currently reparses internal parts of text while adding subnodes such as "( nodes, nodes, nodes )", "[ nodes, nodes, nodes ]", and "{ nodes, nodes, nodes }" while adding subrules and replacing text via substitution internally. 

However, you could load up EBNF rules before-hand as you would with a third party tool like Yacc/Bison, and once precompiled you can serialized the custom Lexer.Rules and Parser.Sequences as they are runtime Objects to a serialized JSON and load them once at runtime on subsequent runs without the performance hit from the process of parsing and adding EBNF rules, if you have a set grammar that you want to pre-compile ahead of time, which is most often the case. However, the flexibility is there to add EBNF rules directly at runtime for any specific use case you might have, you can build new rules and test them at runtime in, say, a REPL shell.

Also note, in addition to EBNF operators { ... } for optional repeating blocks, and [ ... ] for optional rule blocks that appear zero or one times, you can also use %% ... %% for mandatory repeating blocks that must appear at least once as an extension to Extended BNF.

**To Do**

- Unit Testing
- Nuget Packaging
- Variable name capture support in EBNF, this is currently available in JSON with "varName" on sections you want to name, it means I would need to make more extensions to  Extended BNF, which, if anything, is a mild source of humor.

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
