# LexerParser
A C# Lexical Analyzer and Parser with JSON configuration and partial Extended Backus-Naur Form support (https://en.wikipedia.org/wiki/Extended_Backus%E2%80%93Naur_form) for parsing context-free grammars expressed in EBNF, or more directly albeit a in tad bit more verbose fashion from a JSON configuration, from a user input text string or a document such as Html, CSS, SQL, expression calculators, etc.

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
        ParserResultWalker walker = new ParserResultWalker(result.Results[0], showOnConsoleDefault: true);
        walker.Visit();
        
        ConsoleWalker walker = new ConsoleWalker(result.Results[0]);
    }    
}
```

![Screenshot](https://raw.githubusercontent.com/JohnnyErnest/LexerParser/main/ParserImage.png)

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
        Node:htmlTag:::rhsBlockBrace:11:0, Start:19, InnerText:Title
         Node:htmlInnerTagText, Start:19, InnerText:Title
          Node:htmlInnerTagText:::rule_main_block:0:0, Start:19, InnerText:Title
           Node:htmlInnerTagText:::rhsBlockDoublePercent:9:0, Start:19, InnerText:Title
             Token: letters, Start:19, Text:Title
        Node:htmlCloseTag, Start:24, InnerText:</title>
         Node:htmlCloseTag:::rule_main_block:0:0, Start:24, InnerText:</title>
           Token: strhtmlCloseTag:::ebnfTerminal:11:0, Start:24, Text:</
          Node:identifier, Start:26, InnerText:title
            Token: letter, Start:26, Text:t
            Token: letter, Start:27, Text:i
            Token: letter, Start:28, Text:t
            Token: letter, Start:29, Text:l
            Token: letter, Start:30, Text:e
           Token: strhtmlCloseTag:::ebnfTerminal:13:0, Start:31, Text:>
     Node:htmlCloseTag, Start:32, InnerText:</head>
      Node:htmlCloseTag:::rule_main_block:0:0, Start:32, InnerText:</head>
        Token: strhtmlCloseTag:::ebnfTerminal:11:0, Start:32, Text:</
       Node:identifier, Start:34, InnerText:head
         Token: letter, Start:34, Text:h
         Token: letter, Start:35, Text:e
         Token: letter, Start:36, Text:a
         Token: letter, Start:37, Text:d
        Token: strhtmlCloseTag:::ebnfTerminal:13:0, Start:38, Text:>
  Node:htmlTag:::rhsBlockBrace:11:1, Start:39, InnerText:<body><h2>Helloooo hi</h2><div>Here is <span>some</span> text</div></body>
   Node:htmlTag, Start:39, InnerText:<body><h2>Helloooo hi</h2><div>Here is <span>some</span> text</div></body>
    Node:htmlTag:::rule_main_block:0:0, Start:39, InnerText:<body><h2>Helloooo hi</h2><div>Here is <span>some</span> text</div></body>
     Node:htmlOpenTag, Start:39, InnerText:<body>
      Node:htmlOpenTag:::rule_main_block:0:0, Start:39, InnerText:<body>
        Token: strhtmlOpenTag:::ebnfTerminal:11:2, Start:39, Text:<
       Node:identifier, Start:40, InnerText:body
         Token: letter, Start:40, Text:b
         Token: letter, Start:41, Text:o
         Token: letter, Start:42, Text:d
         Token: letter, Start:43, Text:y
        Token: strhtmlOpenTag:::ebnfTerminal:13:0, Start:44, Text:>
     Node:htmlTag:::rhsBlockBrace:11:1, Start:45, InnerText:<h2>Helloooo hi</h2>
      Node:htmlTag, Start:45, InnerText:<h2>Helloooo hi</h2>
       Node:htmlTag:::rule_main_block:0:0, Start:45, InnerText:<h2>Helloooo hi</h2>
        Node:htmlOpenTag, Start:45, InnerText:<h2>
         Node:htmlOpenTag:::rule_main_block:0:0, Start:45, InnerText:<h2>
           Token: strhtmlOpenTag:::ebnfTerminal:11:2, Start:45, Text:<
          Node:identifier, Start:46, InnerText:h2
            Token: letter, Start:46, Text:h
            Token: digit, Start:47, Text:2
           Token: strhtmlOpenTag:::ebnfTerminal:13:0, Start:48, Text:>
        Node:htmlTag:::rhsBlockBrace:11:0, Start:49, InnerText:Helloooo hi
         Node:htmlInnerTagText, Start:49, InnerText:Helloooo hi
          Node:htmlInnerTagText:::rule_main_block:0:0, Start:49, InnerText:Helloooo hi
           Node:htmlInnerTagText:::rhsBlockDoublePercent:9:0, Start:49, InnerText:Helloooo
             Token: letters, Start:49, Text:Helloooo
           Node:htmlInnerTagText:::rhsBlockDoublePercent:9:0, Start:57, InnerText:
             Token: spaces, Start:57, Text:
           Node:htmlInnerTagText:::rhsBlockDoublePercent:9:0, Start:58, InnerText:hi
             Token: letters, Start:58, Text:hi
        Node:htmlCloseTag, Start:60, InnerText:</h2>
         Node:htmlCloseTag:::rule_main_block:0:0, Start:60, InnerText:</h2>
           Token: strhtmlCloseTag:::ebnfTerminal:11:0, Start:60, Text:</
          Node:identifier, Start:62, InnerText:h2
            Token: letter, Start:62, Text:h
            Token: digit, Start:63, Text:2
           Token: strhtmlCloseTag:::ebnfTerminal:13:0, Start:64, Text:>
     Node:htmlTag:::rhsBlockBrace:11:1, Start:65, InnerText:<div>Here is <span>some</span> text</div>
      Node:htmlTag, Start:65, InnerText:<div>Here is <span>some</span> text</div>
       Node:htmlTag:::rule_main_block:0:0, Start:65, InnerText:<div>Here is <span>some</span> text</div>
        Node:htmlOpenTag, Start:65, InnerText:<div>
         Node:htmlOpenTag:::rule_main_block:0:0, Start:65, InnerText:<div>
           Token: strhtmlOpenTag:::ebnfTerminal:11:2, Start:65, Text:<
          Node:identifier, Start:66, InnerText:div
            Token: letter, Start:66, Text:d
            Token: letter, Start:67, Text:i
            Token: letter, Start:68, Text:v
           Token: strhtmlOpenTag:::ebnfTerminal:13:0, Start:69, Text:>
        Node:htmlTag:::rhsBlockBrace:11:0, Start:70, InnerText:Here is
         Node:htmlInnerTagText, Start:70, InnerText:Here is
          Node:htmlInnerTagText:::rule_main_block:0:0, Start:70, InnerText:Here is
           Node:htmlInnerTagText:::rhsBlockDoublePercent:9:0, Start:70, InnerText:Here
             Token: letters, Start:70, Text:Here
           Node:htmlInnerTagText:::rhsBlockDoublePercent:9:0, Start:74, InnerText:
             Token: spaces, Start:74, Text:
           Node:htmlInnerTagText:::rhsBlockDoublePercent:9:0, Start:75, InnerText:is
             Token: letters, Start:75, Text:is
           Node:htmlInnerTagText:::rhsBlockDoublePercent:9:0, Start:77, InnerText:
             Token: spaces, Start:77, Text:
        Node:htmlTag:::rhsBlockBrace:11:1, Start:78, InnerText:<span>some</span>
         Node:htmlTag, Start:78, InnerText:<span>some</span>
          Node:htmlTag:::rule_main_block:0:0, Start:78, InnerText:<span>some</span>
           Node:htmlOpenTag, Start:78, InnerText:<span>
            Node:htmlOpenTag:::rule_main_block:0:0, Start:78, InnerText:<span>
              Token: strhtmlOpenTag:::ebnfTerminal:11:2, Start:78, Text:<
             Node:identifier, Start:79, InnerText:span
               Token: letter, Start:79, Text:s
               Token: letter, Start:80, Text:p
               Token: letter, Start:81, Text:a
               Token: letter, Start:82, Text:n
              Token: strhtmlOpenTag:::ebnfTerminal:13:0, Start:83, Text:>
           Node:htmlTag:::rhsBlockBrace:11:0, Start:84, InnerText:some
            Node:htmlInnerTagText, Start:84, InnerText:some
             Node:htmlInnerTagText:::rule_main_block:0:0, Start:84, InnerText:some
              Node:htmlInnerTagText:::rhsBlockDoublePercent:9:0, Start:84, InnerText:some
                Token: letters, Start:84, Text:some
           Node:htmlCloseTag, Start:88, InnerText:</span>
            Node:htmlCloseTag:::rule_main_block:0:0, Start:88, InnerText:</span>
              Token: strhtmlCloseTag:::ebnfTerminal:11:0, Start:88, Text:</
             Node:identifier, Start:90, InnerText:span
               Token: letter, Start:90, Text:s
               Token: letter, Start:91, Text:p
               Token: letter, Start:92, Text:a
               Token: letter, Start:93, Text:n
              Token: strhtmlCloseTag:::ebnfTerminal:13:0, Start:94, Text:>
        Node:htmlTag:::rhsBlockBrace:11:2, Start:95, InnerText: text
         Node:htmlInnerTagText, Start:95, InnerText: text
          Node:htmlInnerTagText:::rule_main_block:0:0, Start:95, InnerText: text
           Node:htmlInnerTagText:::rhsBlockDoublePercent:9:0, Start:95, InnerText:
             Token: spaces, Start:95, Text:
           Node:htmlInnerTagText:::rhsBlockDoublePercent:9:0, Start:96, InnerText:text
             Token: letters, Start:96, Text:text
        Node:htmlCloseTag, Start:100, InnerText:</div>
         Node:htmlCloseTag:::rule_main_block:0:0, Start:100, InnerText:</div>
           Token: strhtmlCloseTag:::ebnfTerminal:11:0, Start:100, Text:</
          Node:identifier, Start:102, InnerText:div
            Token: letter, Start:102, Text:d
            Token: letter, Start:103, Text:i
            Token: letter, Start:104, Text:v
           Token: strhtmlCloseTag:::ebnfTerminal:13:0, Start:105, Text:>
     Node:htmlCloseTag, Start:106, InnerText:</body>
      Node:htmlCloseTag:::rule_main_block:0:0, Start:106, InnerText:</body>
        Token: strhtmlCloseTag:::ebnfTerminal:11:0, Start:106, Text:</
       Node:identifier, Start:108, InnerText:body
         Token: letter, Start:108, Text:b
         Token: letter, Start:109, Text:o
         Token: letter, Start:110, Text:d
         Token: letter, Start:111, Text:y
        Token: strhtmlCloseTag:::ebnfTerminal:13:0, Start:112, Text:>
  Node:htmlCloseTag, Start:113, InnerText:</html>
   Node:htmlCloseTag:::rule_main_block:0:0, Start:113, InnerText:</html>
     Token: strhtmlCloseTag:::ebnfTerminal:11:0, Start:113, Text:</
    Node:identifier, Start:115, InnerText:html
      Token: letter, Start:115, Text:h
      Token: letter, Start:116, Text:t
      Token: letter, Start:117, Text:m
      Token: letter, Start:118, Text:l
     Token: strhtmlCloseTag:::ebnfTerminal:13:0, Start:119, Text:>
```
