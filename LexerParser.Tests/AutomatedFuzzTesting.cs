using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LexerParser.Tests
{
    public class AutomatedFuzzTesting
    {
        Lexer lexer;
        Parser parser;

        [SetUp]
        public void Setup()
        {
            string path = "../../../../Configuration";

            string defaultConfiguration = $"{path}/Default.json";
            string userConfiguration = $"{path}/ParserEBNF.json";
            lexer = new Lexer(defaultConfiguration, userConfiguration);
            parser = new Parser(lexer, userConfiguration);
        }
    }
}
