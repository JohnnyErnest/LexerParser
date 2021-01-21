using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LexerParser
{
    public class Parser
    {
        public class ParserSequence : ICloneable
        {
            public string SequenceName { get; set; }
            public List<Parser.SequenceSection> Sections { get; set; }
            public override string ToString()
            {
                return $"[ParserSequence: {SequenceName}, Sections: {(Sections != null ? Sections.Count().ToString() : "(Null)")}]";
            }
            public class EvaluateSequenceSectionResult : ICloneable
            {
                public string MatchedRule { get; set; }
                public string Input { get; set; }
                public string OriginalInput { get; set; }
                public int StartIndex { get; set; }
                public int Length { get; set; }
                public object Clone()
                {
                    var result = new EvaluateSequenceSectionResult()
                    {
                        MatchedRule = this.MatchedRule,
                        Input = this.Input,
                        OriginalInput = this.OriginalInput,
                        Length = this.StartIndex,
                        StartIndex = this.Length
                    };
                    return result;
                }
            }
            EvaluateSequenceSectionResult EvaluateSequenceSection(string input, Lexer lexer, Parser.SequenceSection section)
            {
                for (int i = 1; i <= input.Length; i++)
                {
                    string str = input.Substring(0, i);
                    string[] rules = section.Tokens;
                    bool[] ruleEval = new bool[rules.Length];
                    bool matchAny = false;
                    string matchedRule = "";
                    for (int i1 = 0; i1 < ruleEval.Length; i1++)
                    {
                        bool result = lexer.EvaluateRule(str, rules[i1]);
                        ruleEval[i1] = result;
                        if (result)
                        {
                            matchAny = true;
                            matchedRule = rules[i1];
                            break;
                        }
                    }
                    if (matchAny)
                    {
                        return new EvaluateSequenceSectionResult()
                        {
                            Input = str,
                            OriginalInput = input,
                            StartIndex = 0,
                            Length = i,
                            MatchedRule = matchedRule
                        };
                    }
                }
                return null;
            }
            public bool Evaluate(string input, Lexer lexer)
            {
                int index = 0;
                foreach (var section in Sections)
                {
                    bool optional = section.IsOptional;
                    bool repeating = section.IsRepeating;
                    EvaluateSequenceSectionResult result = null;
                    if (repeating)
                    {
                        bool foundAtLeastOnce = false;
                        for (int i1 = 1; i1 <= input.Length; i1++)
                        {
                            string str = input.Substring(index, i1);
                            EvaluateSequenceSectionResult result1 = EvaluateSequenceSection(str, lexer, section);
                            if (result1 != null)
                            {
                                if (!string.IsNullOrEmpty(result1.MatchedRule))
                                {
                                    foundAtLeastOnce = true;
                                    result = result1;
                                    break;
                                }
                                else
                                {
                                    return foundAtLeastOnce;
                                }
                            }
                            else
                            {
                                return foundAtLeastOnce;
                            }
                        }
                    }
                    else
                    {
                        for (int i1 = 1; i1 <= input.Length; i1++)
                        {
                            string str = input.Substring(index, i1);
                            EvaluateSequenceSectionResult result1 = EvaluateSequenceSection(str, lexer, section);
                            if (result1 != null)
                            {
                                if (!string.IsNullOrEmpty(result1.MatchedRule))
                                {
                                    result = result1;
                                    break;
                                }
                            }
                        }
                    }
                    if (!optional && result == null) { return false; }
                }
                return true;
            }
            public object Clone()
            {
                var result = new ParserSequence()
                {
                    SequenceName = this.SequenceName,
                    Sections = new List<SequenceSection>()
                };
                foreach(var section in Sections)
                {
                    result.Sections.Add(section.Clone() as Parser.SequenceSection);
                }
                return result;
            }
        }
        public class SequenceSection : ICloneable
        {
            public bool IsOptional { get; set; }
            public bool IsRepeating { get; set; }
            public string TokenList { get; set; }
            public string SequenceList { get; set; }
            [JsonProperty("ebnfItem")]
            public string EBNFItemList { get; set; }
            [JsonProperty("varName")]
            public string VariableName { get; set; }
            public string Unknowns { get; set; }
            public string[] Tokens { get { return !string.IsNullOrEmpty(TokenList) ? TokenList.Split(',').Select(x => x.Trim()).ToArray() : null; } }
            public string[] Sequences { get { return !string.IsNullOrEmpty(SequenceList) ? SequenceList.Split(',').Select(x => x.Trim()).ToArray() : null; } }
            public string[] EBNFChoices { get { return !string.IsNullOrEmpty(EBNFItemList) ? EBNFItemList.Split('|').Select(x => x.Trim()).ToArray() : null; } }
            public string[] EBNFConcatenations { get { return !string.IsNullOrEmpty(EBNFItemList) ? EBNFItemList.Split(',').Select(x => x.Trim()).ToArray() : null; } }
            public string SyntaxColor { get; set; }
            public object Clone()
            {
                var result = new SequenceSection()
                {
                    EBNFItemList = this.EBNFItemList,
                    IsOptional = this.IsOptional,
                    IsRepeating = this.IsRepeating,
                    SequenceList = this.SequenceList,
                    SyntaxColor = this.SyntaxColor,
                    TokenList = this.TokenList,
                    Unknowns = this.Unknowns,
                    VariableName = this.VariableName                    
                };
                return result;
            }
            public override string ToString()
            {
                string tokenStr = "";
                string seqStr = "";
                if (Tokens != null)
                {
                    tokenStr = string.Join(", ", Tokens);
                }
                if (Sequences != null)
                {
                    seqStr = string.Join(", ", Sequences);
                }
                return $"[Opt:{IsOptional}, Rep:{IsRepeating}, Tokens:{tokenStr}, Seq:{seqStr}]";
            }
        }
        public class ParserSequenceAndSection : ICloneable
        {
            public ParserSequence Sequence { get; set; }
            public SequenceSection Section { get; set; }
            public ParserResult Node { get; set; }
            public string NodeText { get; set; }
            public object Clone()
            {
                var result = new ParserSequenceAndSection()
                {
                    Sequence = this.Sequence.Clone() as ParserSequence,
                    Node = this.Node.Clone() as ParserResult,
                    NodeText = this.NodeText,
                    Section = this.Section.Clone() as SequenceSection
                };
                return result;
            }
        }
        public class EvaluationResult : ICloneable
        {
            public Type EvaluationType { get; set; }
            public object EvaluationValue { get; set; }
            public string EvaluationText { get; set; }
            public object Clone()
            {
                EvaluationResult eval = new EvaluationResult() {
                    EvaluationText = this.EvaluationText, 
                    EvaluationType = this.EvaluationType, 
                    EvaluationValue = this.EvaluationValue 
                };
                return eval;
            }
            public override string ToString()
            {
                return $"[Eval Result:{EvaluationType.Name}/{EvaluationText}]";
            }
        }
        public class ParserResult : ICloneable
        {
            public class EBNFContainmentInfo : ICloneable
            {
                public string IdentifierName { get; set; }
                public bool Contains { get; set; }
                public bool ContainedBy { get; set; }
                public bool IsSameNode { get; set; }
                public object Clone()
                {
                    return new EBNFContainmentInfo()
                    {
                        ContainedBy = ContainedBy,
                        Contains = Contains,
                        IdentifierName = IdentifierName,
                        IsSameNode = IsSameNode
                    };
                }
            }
            public string Name { get; set; }
            public Lexer.Span Span { get; set; }
            public string VariableName { get; set; }
            public string VariableValue { get; set; }
            public List<(string, string)> Variables { get; set; } = new List<(string, string)>();
            public ParserSequence Sequence { get; set; }
            public SequenceSection Section { get; set; }
            public int Level { get; set; }
            public List<ParserResult> InnerResults { get; set; } = new List<ParserResult>();
            [JsonIgnore()]
            public ParserSequenceAndSection Parent { get; set; }
            [JsonIgnore()]
            public ParserSequenceAndSection Root { get; set; }
            public ParserResult FirstSibling { get
                {
                    if (Parent.Node.InnerResults.Count > 0)
                    {
                        return Parent.Node.InnerResults[0];
                    }
                    return null;
                } 
            }
            public ParserResult LastSibling { get
                {
                    int item = Parent.Node.InnerResults.Count - 1;
                    if (Parent.Node.InnerResults.Count > 0)
                    {
                        return Parent.Node.InnerResults[item];
                    }
                    else return null;
                } 
            }
            public ParserResult PriorSibling
            {
                get
                {
                    int idx = 0;
                    for(int i=0;i<Parent.Node.InnerResults.Count;i++)
                    {
                        if (Parent.Node.InnerResults[i] == this)
                        {
                            idx = i;
                        }
                    }
                    if (idx - 1 > 0)
                    {
                        return Parent.Node.InnerResults[idx - 1];
                    }
                    return null;
                }
            }
            public ParserResult NextSibling
            {
                get
                {
                    int idx = 0;
                    for (int i = 0; i < Parent.Node.InnerResults.Count; i++)
                    {
                        if (Parent.Node.InnerResults[i] == this)
                        {
                            idx = i;
                        }
                    }
                    if (idx + 1 < Parent.Node.InnerResults.Count)
                    {
                        return Parent.Node.InnerResults[idx + 1];
                    }
                    return null;
                }
            }
            public string GroupName { get; set; }
            public bool Evaluated { get; set; }
            public EvaluationResult EvaluationResult { get; set; }
            public Func<ParserResult, EvaluationResult> EvaluationFunction { get; set; }
            public string InnerResultsText
            {
                get
                {
                    string span = (Span == null) ? "" : Span.Text;
                    return GetInnerStringRecursive(InnerResults) + span;
                }
            }
            public (string, int, int, int) InnerResultsMeta { get {
                    if (InnerResults.Count == 0)
                    {
                        if (Span != null)
                        {
                            string text = Span.Text;
                            int minStart = Span.Start;
                            int maxEnd = Span.End;
                            return (text, minStart, maxEnd, maxEnd - minStart);
                        }
                        else
                        {
                            string text = "";
                            int minStart = -1;
                            int maxEnd = -1;
                            return (text, minStart, maxEnd, maxEnd - minStart);
                        }
                    }
                    else
                    {
                        string text = InnerResultsText;
                        int minStart = MinStart();
                        int maxEnd = MaxEnd();
                        return (text, minStart, maxEnd, maxEnd - minStart);
                    }
                } 
            }
            public override string ToString()
            {
                string variableStr = "";
                string innerStr = "";
                string spanStr = "";
                string evalStr = "";
                if (!string.IsNullOrEmpty(VariableName) && !string.IsNullOrEmpty(VariableValue))
                {
                    variableStr = VariableValue;
                }
                if (InnerResults != null)
                {
                    innerStr = GetInnerStringRecursive(InnerResults);
                }
                if (Span != null)
                {
                    spanStr = Span.ToString();
                }
                if (EvaluationResult != null)
                {
                    evalStr = EvaluationResult.EvaluationText;
                }
                string varName = (!string.IsNullOrEmpty(VariableName) ? VariableName : "");
                return $"[Lvl:{Level}, Name:{Name}, Seq:{((Sequence != null) ? Sequence.SequenceName : "null")}, Var:{varName}:{variableStr}, Span:{spanStr}, Inner:{innerStr}, (Start:{MinStart()}/End:{MaxEnd()}), Eval:{evalStr}]";
            }
            string GetInnerStringRecursive(List<ParserResult> results)
            {
                string variableStr = "";
                if (InnerResults != null)
                {
                    if (InnerResults.Count > 0)
                    {
                        foreach (var inner in results)
                        {
                            if (inner.Span != null)
                            {
                                if (!string.IsNullOrEmpty(inner.Span.Text))
                                {
                                    variableStr += inner.Span.Text;
                                }
                            }
                            variableStr += GetInnerStringRecursive(inner.InnerResults);
                        }
                    }
                }
                return variableStr;
            }
            public string GetJson()
            {
                return JsonConvert.SerializeObject(this);
            }
            public List<ParserResult> GetDescendantsOfType(string[] descendantTypes, List<ParserResult> input = null, ParserResult start = null)
            {
                if (start == null) { start = this; }
                List<ParserResult> results = new List<ParserResult>();
                if (input != null) { results = input; }
                if (start.InnerResults != null)
                {
                    foreach (var inner in start.InnerResults)
                    {
                        GetDescendantsOfType(descendantTypes, results, inner);
                    }
                }
                if (descendantTypes.Contains(start.Name)) { results.Add(start); }
                return results;
            }
            public List<ParserResult> GetDescendantsOfTypeByGroupName(string[] descendantTypes, List<ParserResult> input = null, ParserResult start = null)
            {
                if (start == null) { start = this; }
                List<ParserResult> results = new List<ParserResult>();
                if (input != null) { results = input; }
                if (start.InnerResults != null)
                {
                    foreach (var inner in start.InnerResults)
                    {
                        GetDescendantsOfTypeByGroupName(descendantTypes, results, inner);
                    }
                }
                if (descendantTypes.Contains(start.GroupName)) { results.Add(start); }
                return results;
            }
            public int MinStart(int minValue = Int32.MaxValue, ParserResult start = null)
            {
                if (start == null) { start = this; }
                if (start.InnerResults != null)
                {
                    foreach (var node in start.InnerResults)
                    {
                        if (node.Span != null)
                        {
                            minValue = Math.Min(minValue, node.Span.Start);
                        }
                        minValue = MinStart(minValue, node);
                    }
                }
                if (start.Span != null)
                {
                    minValue = Math.Min(minValue, start.Span.Start);
                }
                return minValue;
            }
            public int MaxEnd(int maxValue = Int32.MinValue, ParserResult start = null)
            {
                if (start == null) { start = this; }
                if (start.InnerResults != null)
                {
                    foreach (var node in start.InnerResults)
                    {
                        if (node.Span != null)
                        {
                            maxValue = Math.Max(maxValue, node.Span.End);
                        }
                        maxValue = MaxEnd(maxValue, node);
                    }
                }
                if (start.Span != null)
                {
                    maxValue = Math.Min(maxValue, start.Span.End);
                }
                return maxValue;
            }
            public List<(string, ParserResult)> GetEBNFGroups(string identifierName, bool includeStringLiterals)
            {
                List<string> types = new List<string>() { "rhsBlockBracket", "rhsBlockBrace", "rhsBlockParenthesis", "rhsBlockDoublePercent" };
                if (includeStringLiterals)
                {
                    types.Add("ebnfTerminal");
                }
                var descendants = GetDescendantsOfType(types.ToArray()).OrderByDescending(x => x.Level).ThenByDescending(x => x.MinStart()).ToList();
                var results = new List<(string, ParserResult)>();
                int idx = 0;
                foreach (var item in descendants)
                {
                    string id = identifierName + ":::" + item.Name.Trim() + ":" + item.Level + ":" + idx;
                    results.Add((id, item));
                    idx++;
                }
                return results;
            }
            public List<(string identifierName, ParserResult result, List<(string idName, bool contains, bool containedBy, bool isSameNode)> containerInfo)> GetEBNFGroupsWithContainmentMap(string identifierName, bool includeStringLiterals = false)
            {
                var res1 = GetEBNFGroups(identifierName, includeStringLiterals);
                List<(string identifierName, ParserResult result, List<(string idName, bool contains, bool containedBy, bool isSameNode)>)> results = new List<(string, ParserResult, List<(string, bool, bool, bool)>)>();
                for (int i = 0; i < res1.Count; i++)
                {
                    var r1 = res1[i];
                    List<(string idName, bool contains, bool containedBy, bool isSameNode)> containmentMap = new List<(string, bool, bool, bool)>();
                    for (int i2 = 0; i2 < res1.Count; i2++)
                    {
                        var r2 = res1[i2];
                        bool contains = r1.Item2.ContainsNode(r2.Item2);
                        bool containedBy = r2.Item2.ContainsNode(r1.Item2);
                        bool isSameNode = r1.Item2 == r2.Item2;
                        containmentMap.Add((r2.Item1, contains, containedBy, isSameNode));
                    }
                    results.Add((r1.Item1, r1.Item2, containmentMap));
                }
                return results;
            }
            public List<(string identifierName, ParserResult result, List<string> contains, List<string> containedBy, string firstContainedBy)> GetEBNFGroupsWithContainmentMapSimplified(string identifierName, bool includeStringLiterals = false)
            {
                var res1 = GetEBNFGroups(identifierName, includeStringLiterals);
                //List<(string identifierName, ParserResult result, List<(string idName, bool contains, bool containedBy, bool isSameNode)>)> results = new List<(string, ParserResult, List<(string, bool, bool, bool)>)>();
                List<(string identifierName, ParserResult result, List<string> contains, List<string> containedBy, string firstContainedBy)> results = new List<(string identifierName, ParserResult result, List<string> contains, List<string> containedBy, string firstContainedBy)>();
                for (int i = 0; i < res1.Count; i++)
                {
                    var r1 = res1[i];

                    List<string> containsNodes = new List<string>();
                    List<string> containedByNodes = new List<string>();
                    List<(string, int)> containedByNodesWithLevel = new List<(string, int)>();
                    string firstContainedBy = "";
                    //List<(string idName, bool contains, bool containedBy, bool isSameNode)> containmentMap = new List<(string, bool, bool, bool)>();
                    for (int i2 = 0; i2 < res1.Count; i2++)
                    {
                        var r2 = res1[i2];
                        bool isSameNode = r1.Item2 == r2.Item2;
                        bool contains = r1.Item2.ContainsNode(r2.Item2);
                        bool containedBy = r2.Item2.ContainsNode(r1.Item2);
                        if (contains && !isSameNode) { containsNodes.Add(r2.Item1); }
                        if (containedBy && !isSameNode)
                        {
                            containedByNodes.Add(r2.Item1);
                            containedByNodesWithLevel.Add((r2.Item1, r2.Item2.Level));
                        }
                        //containmentMap.Add((r2.Item1, contains, containedBy, isSameNode));
                    }
                    if (containedByNodesWithLevel.Count > 0)
                    {
                        firstContainedBy = containedByNodesWithLevel.OrderByDescending(x => x.Item2).First().Item1;
                    }
                    //results.Add((r1.Item1, r1.Item2, containmentMap));
                    results.Add((r1.Item1, r1.Item2, containsNodes, containedByNodes, firstContainedBy));
                }
                return results;
            }
            public bool ContainsNode(ParserResult node, ParserResult start = null)
            {
                if (start == null) { start = this; if (start == node) return false; }
                if (start.InnerResults != null)
                {
                    foreach (var item in start.InnerResults)
                    {
                        if (ContainsNode(node, item)) { return true; }
                    }
                }
                if (start == node) { return true; }
                return false;
            }
            public object Clone()
            {
                var result = new ParserResult()
                {
                    Evaluated = this.Evaluated,
                    EvaluationFunction = this.EvaluationFunction,
                    EvaluationResult = this.EvaluationResult,
                    VariableName = this.VariableName,
                    VariableValue = this.VariableValue,
                    InnerResults = new List<ParserResult>(),
                    Level = this.Level,
                    Name = this.Name,
                    GroupName = this.GroupName,
                    Variables = new List<(string, string)>(),
                    Parent = new ParserSequenceAndSection(),
                    Root = new ParserSequenceAndSection()
                };
                if (this.Span != null)
                {
                    result.Span = this.Span.Clone() as Lexer.Span;
                }
                foreach (var inner in InnerResults)
                {
                    result.InnerResults.Add(inner.Clone() as ParserResult);
                }
                foreach(var vari in Variables)
                {
                    result.Variables.Add((vari.Item1, vari.Item2));
                }
                return result;
            }
        }
        public class Result
        {
            public Lexer.LexerResult LexerResult { get; set; }
            public bool Matched { get; set; }
            public ParserSequence Sequence { get; set; }
            public List<ParserResult> Results { get; set; } = new List<ParserResult>();

            public override string ToString()
            {
                return $"Matched:{Matched} Seq:{(Sequence != null ? Sequence.SequenceName : "")}";
            }
        }
        public Lexer InputLexer { get; set; }
        public List<ParserSequence> Sequences { get; set; }
        public List<(int, string, string)> ParsedSequenceLog = new List<(int, string, string)>();
        public List<(int, string, string)> ParsedSequenceSectionLog = new List<(int, string, string)>();
        public List<(int, string, string, string)> ParsedLog = new List<(int, string, string, string)>();
        public int MaxLevel { get; set; } = 100;
        public bool CancelAllParsing = false;
        public bool UseLogging = false;
        public StringBuilder Log { get; set; } = new StringBuilder();
        public Parser(JObject parser)
        {
            InitParser(parser);
        }
        public Parser(Lexer lexer, JObject parser, bool useLogging = false)
        {
            UseLogging = useLogging;
            InputLexer = lexer;
            InitParser(parser);
        }
        public Parser(Lexer lexer, string parserConfigurationFile, bool useLogging = false)
        {
            string config = File.ReadAllText(parserConfigurationFile);
            //dynamic parserObj = JsonConvert.DeserializeObject<dynamic>(config);
            //JObject parserSequences = (JObject)parserObj.parser;

            JObject parserSequences = (JObject)JObject.Parse(config)["parser"];

            UseLogging = useLogging;
            InputLexer = lexer;
            InitParser(parserSequences);
        }
        void InitParser(JObject parser)
        {
            Sequences = new List<ParserSequence>();
            foreach (var kvp in parser)
            {
                if (kvp.Value.Type == JTokenType.Object)
                {
                    string key = kvp.Key;
                    var ch = kvp.Value.Children().First();
                    string name = (ch as JProperty).Name;
                    if (name.ToLower() == "sequence")
                    {
                        List<SequenceSection> sections = new List<SequenceSection>();
                        var nodes = ch.Children().First();
                        foreach (var node in nodes)
                        {
                            sections.Add(JsonConvert.DeserializeObject<SequenceSection>(node.ToString()));
                        }
                        Sequences.Add(new ParserSequence() { SequenceName = key, Sections = sections });
                    }
                    else if (name.ToLower() == "sequenceString")
                    {

                    }
                }
            }
        }
        void InitParserLog()
        {
            ParsedSequenceLog = new List<(int, string, string)>();
            ParsedSequenceSectionLog = new List<(int, string, string)>();
            ParsedLog = new List<(int, string, string, string)>();
        }
        public bool Evaluate(string input, Lexer lexer)
        {
            foreach (var sequence in Sequences)
            {
                if (sequence.Evaluate(input, lexer)) return true;
            }
            return false;
        }
        (int, bool, bool, List<ParserResult>, bool, bool) CheckSequenceSection(Lexer.LexerResult input, Lexer lexer, ParserSequence sequence, SequenceSection section, int index, bool foundOnce, List<ParserResult> variables, int level, ParserSequenceAndSection parent, ParserSequenceAndSection root, bool showOnConsole = false)
        {
            if (level > MaxLevel)
            {
                CancelAllParsing = true;
                return (-1, false, false, null, false, false);
            }
            var tokens = section.Tokens;
            var sequences = section.Sequences;
            var optional = section.IsOptional;
            var repeating = section.IsRepeating;

            var spans = input.OrganizedSpans.Where(x => x.Start <= index && index < x.End).ToArray().Clone() as Lexer.Span[];
            var spans_org = input.ParsedOrganized.Where(x => x.Start <= index && index < x.End).ToArray().Clone() as Lexer.Span[];

            bool found = false;
            if (tokens != null)
            {
                foreach (var item in tokens)
                {
                    var rule = lexer.Rules.Where(x => x.RuleName.ToLower() == item.ToLower()).FirstOrDefault();
                    if (rule != null)
                    {
                        bool foundInParsed = false;
                        foreach(var span in spans_org)
                        {
                            if (!found)
                            {
                                if (span.Rule.RuleName == rule.RuleName)
                                {
                                    found = true;
                                    foundInParsed = true;
                                    index += span.Length;
                                    string groupName = rule.RuleName;
                                    if (groupName.Contains(":::"))
                                    {
                                        int strIndex = groupName.IndexOf(":::");
                                        groupName = groupName.Substring(0, strIndex);
                                    }
                                    var result1 = new ParserResult
                                    {
                                        Level = level,
                                        VariableName = section.VariableName,
                                        VariableValue = span.Text,
                                        Span = span,
                                        Section = section,
                                        Sequence = sequence,
                                        Name = rule.RuleName,
                                        GroupName = groupName,
                                        Parent = parent,
                                        Root = root
                                    };
                                    //parent.Node = result1;
                                    variables.Add(result1);
                                }
                            }
                            if (!found)
                            {
                                foreach (var inner in span.InnerSpans.Where(x => x.Start <= index && index < x.End))
                                {
                                    if (!found)
                                    {
                                        if (inner.Rule.RuleName == rule.RuleName)
                                        {
                                            found = true;
                                            foundInParsed = true;
                                            index += inner.Length;
                                            string groupName = rule.RuleName;
                                            if (groupName.Contains(":::"))
                                            {
                                                int strIndex = groupName.IndexOf(":::");
                                                groupName = groupName.Substring(0, strIndex);
                                            }
                                            var result1 = new ParserResult
                                            {
                                                Level = level + 1,
                                                VariableName = section.VariableName,
                                                VariableValue = inner.Text,
                                                Span = inner,
                                                Section = section,
                                                Sequence = sequence,
                                                Name = rule.RuleName,
                                                GroupName = groupName,
                                                Root = root,
                                                Parent = parent
                                            };
                                            //parent.Node = result1;
                                            variables.Add(result1);
                                        }
                                    }
                                }
                            }
                        }
                        //bool foundInRegular = false;
                        //foreach (var span in spans)
                        //{
                        //    if (!found)
                        //    {
                        //        if (span.Rule.RuleName == rule.RuleName)
                        //        {
                        //            found = true;
                        //            foundInRegular = true;
                        //            index += span.Length;
                        //            var result1 = new ParserResult
                        //            {
                        //                Level = level,
                        //                VariableName = section.VariableName,
                        //                VariableValue = span.Text,
                        //                Span = span,
                        //                Section = section,
                        //                Sequence = sequence,
                        //                Name = rule.RuleName,
                        //                Parent = parent,
                        //                Root = root
                        //            };
                        //            //parent.Node = result1;
                        //            variables.Add(result1);
                        //        }
                        //    }
                        //    if (!found)
                        //    {
                        //        foreach (var inner in span.InnerSpans.Where(x => x.Start <= index && index < x.End))
                        //        {
                        //            if (!found)
                        //            {
                        //                if (inner.Rule.RuleName == rule.RuleName)
                        //                {
                        //                    found = true;
                        //                    foundInRegular = true;
                        //                    index += inner.Length;
                        //                    var result1 = new ParserResult
                        //                    {
                        //                        Level = level + 1,
                        //                        VariableName = section.VariableName,
                        //                        VariableValue = inner.Text,
                        //                        Span = inner,
                        //                        Section = section,
                        //                        Sequence = sequence,
                        //                        Name = rule.RuleName,
                        //                        Root = root,
                        //                        Parent = parent
                        //                    };
                        //                    //parent.Node = result1;
                        //                    variables.Add(result1);
                        //                }
                        //            }
                        //        }
                        //    }
                        //}
                        //if (foundInRegular == true && foundInParsed == false)
                        //{
                        //}
                    }
                }
            }
            if (sequences != null && !found)
            {
                bool anyTrue = false;
                foreach (var item in sequences)
                {
                    var seq = Sequences.Where(x => x.SequenceName.ToLower() == item.ToLower()).FirstOrDefault();
                    if (seq != null)
                    {
                        if (!found)
                        {
                            //var spanText = (spans != null ? (spans.Count() > 0 ? spans.First().Text : "") : "");
                            //Console.WriteLine("".PadLeft(level * 3, ' ') + $"[{item}, {spanText}], ");

                            //ParsedSequenceSectionLog.Add((level, "Before Called from Inside Section", section.ToString()));
                            //ParsedLog.Add((level, "Before Called from Inside Section", sequence.SequenceName, section.ToString()));

                            if (CancelAllParsing)
                            {
                                ParsedLog.Add((level, "SHUTTING DOWN BEFORE from CheckSequenceSection", sequence.SequenceName, section.ToString()));
                                return (-1, false, false, null, false, false);
                            }

                            var newParent = new ParserSequenceAndSection() { Sequence = seq, Section = null/*, Node = parent.Node*/ };
                            var result = CheckSequence(input, lexer, seq, index, level + 1, newParent, root, showOnConsole);
                            //ParsedLog.Add((level, $"Inner CheckSequence Results Idx:{result.Item1}, Found:{result.Item2}", sequence.SequenceName, ""));

                            //ParsedSequenceSectionLog.Add((level, "After Called from Inside Section", section.ToString()));
                            //ParsedLog.Add((level, "After Called from Inside Section", sequence.SequenceName, section.ToString()));

                            if (level > MaxLevel)
                            {
                                ParsedLog.Add((level, "SHUTTING DOWN AFTER from CheckSequenceSection", sequence.SequenceName, section.ToString()));
                                CancelAllParsing = true;
                                return (-1, false, false, null, false, false);
                            }

                            //Console.WriteLine($"NewIndex:{result.Item1}, Found:{result.Item2}");
                            //Console.WriteLine($"Optional:{optional}, Repeating:{repeating}");

                            if (result.Item2)
                            {
                                anyTrue = true;
                                index = result.Item1;
                                found = result.Item2;
                                if (found)
                                {
                                    string groupName = seq.SequenceName;
                                    if (groupName.Contains(":::"))
                                    {
                                        int strIndex = groupName.IndexOf(":::");
                                        groupName = groupName.Substring(0, strIndex);
                                    }
                                    var parserResult = new ParserResult()
                                    {
                                        Level = level + 1,
                                        Name = seq.SequenceName,
                                        GroupName = groupName,
                                        InnerResults = result.Item3,
                                        Sequence = seq,
                                        Section = section,
                                        VariableName = section.VariableName,
                                        Parent = parent,
                                        Root = root
                                    };
                                    //parent.Node = parserResult;
                                    if (!string.IsNullOrEmpty(parserResult.VariableName))
                                    {
                                        parserResult.VariableValue = parserResult.InnerResultsText;
                                    }
                                    variables.Add(parserResult);
                                }
                            }
                            //else
                            //{
                            //    //if (!section.IsOptional)
                            //    //{
                            //    //    return (-1, false, false, null, false, false);
                            //    //}
                            //}
                        }
                    }
                }
                if (!anyTrue)
                {
                    if (section.IsOptional == false && section.IsRepeating == false)
                    {
                        return (-1, false, false, null, false, false);
                    }
                }
            }
            if (found && repeating)
            {
                foundOnce = true;
                var newParent = new ParserSequenceAndSection() { Section = section, Sequence = sequence/*, Node = parent.Node*/ };
                var result = CheckSequenceSection(input, lexer, sequence, section, index, foundOnce, variables, level, newParent, root, showOnConsole);
                if (result.Item2)
                {
                    variables = result.Item4;
                }
                (index, found, foundOnce) = (result.Item1, result.Item2, result.Item3);
            }
            else if (found)
            {
                foundOnce = true;
            }
            return (index, found, foundOnce, variables, optional, repeating);
        }
        (int, bool, List<ParserResult>) CheckSequence(Lexer.LexerResult input, Lexer lexer, ParserSequence sequence, int index, int level, ParserSequenceAndSection parent, ParserSequenceAndSection root, bool showOnConsole = false)
        {
            if (level > MaxLevel)
            {
                CancelAllParsing = true;
                return (-1, false, null);
            }

            int idxResult = -1;
            bool found = false;
            List<ParserResult> foundItems = new List<ParserResult>();
            int countOptional = sequence.Sections.Where(x => x.IsOptional).Count();
            int countSections = sequence.Sections.Count();
            bool foundAllNonOptional = true;
            int sectionIndex = 0;
            foreach (var section in sequence.Sections)
            {
                List<ParserResult> variables = new List<ParserResult>();

                string idString = section.ToString();
                //ParsedSequenceSectionLog.Add((level, "Before", idString));
                //ParsedLog.Add((level, "Before", sequence.SequenceName, idString));

                if (CancelAllParsing)
                {
                    ParsedLog.Add((level, "SHUTTING DOWN BEFORE from CheckSequence", sequence.SequenceName, section.ToString()));
                    return (-1, false, null);
                }

                //string json = parent.Node.GetJson();
                var newParent = new ParserSequenceAndSection() { Sequence = sequence, Section = section/*, Node = parent.Node, NodeText = json*/ };
                var result = CheckSequenceSection(input, lexer, sequence, section, index, false, variables, level + 1, newParent, root, showOnConsole);

                //ParsedLog.Add((level, $"CheckSequenceSection Results Idx:{result.Item1}, Found:{result.Item2}", sequence.SequenceName, ""));
                //ParsedSequenceSectionLog.Add((level, "After", idString));
                //ParsedLog.Add((level, "After", sequence.SequenceName, idString));

                if (level > MaxLevel)
                {
                    ParsedLog.Add((level, "SHUTTING DOWN AFTER from CheckSequence", sequence.SequenceName, section.ToString()));
                    CancelAllParsing = true;
                    return (-1, false, null);
                }

                if (showOnConsole)
                {
                    Console.WriteLine(level + " " + "".PadLeft(level * 3, ' ') + $"{sectionIndex}:{sequence.SequenceName}, Section:{section}, Found:{result.Item2}, FoundOnce:{result.Item3}, IdxResult:{result.Item1}");
                }
                //Log.AppendLine(level + " " + "".PadLeft(level * 3, ' ') + $"{sectionIndex}:{sequence.SequenceName}, Section:{section}, Found:{result.Item2}, FoundOnce:{result.Item3}, IdxResult:{result.Item1}");

                idxResult = result.Item1;
                found = result.Item2;
                bool foundOnce = result.Item3;
                bool optional = section.IsOptional;
                bool repeating = section.IsRepeating;

                if (found == false && optional == false)
                {
                    foundAllNonOptional = false;
                }

                if (found == false && foundOnce && repeating)
                {
                    found = true;
                }

                if (found == false && optional)
                {
                    index = idxResult;
                }
                else if (found == false && optional == false && (countOptional < countSections))
                {
                    foundItems = new List<ParserResult>();
                    return (-1, false, foundItems);
                }
                else
                {
                    index = idxResult;
                    foundItems.AddRange(result.Item4);
                }

                sectionIndex++;
            }
            if (foundAllNonOptional)
            {
                found = true;
            }
            return (index, found, foundItems);
        }
        public (bool, ParserSequence, List<ParserResult>) Check(Lexer.LexerResult input, Lexer lexer, string sequenceName = "", bool showOnConsole = false)
        {
            int index = 0;
            int endIndex = input.OrganizedSpans.OrderByDescending(x => x.End).FirstOrDefault().End;
            bool found = false;
            ParserSequence currentSequence = null;
            List<ParserResult> items = new List<ParserResult>();
            int level = 0;
            List<ParserSequence> seq = new List<ParserSequence>();
            if (sequenceName != "")
            {
                seq = Sequences.Where(x => x.SequenceName == sequenceName).ToList();
            }
            else
            {
                seq = Sequences.ToList();
            }
            foreach (var sequence in seq)
            {
                if (showOnConsole)
                {
                    Console.WriteLine();
                    Console.WriteLine($"Sequence:{sequence.SequenceName}");
                    Console.WriteLine("++++++++++++++++++++++++++++++++++++++++++++");
                }

                //Log.AppendLine();
                //Log.AppendLine($"Sequence:{sequence.SequenceName}");
                //Log.AppendLine("++++++++++++++++++++++++++++++++++++++++++++");

                currentSequence = sequence;

                //ParsedSequenceLog.Add((level, "Before", sequence.SequenceName));
                //ParsedLog.Add((level, "Before", sequence.SequenceName, ""));

                if (CancelAllParsing)
                {
                    return (false, null, null);
                }

                var root = new ParserSequenceAndSection() { Sequence = sequence, Section = null };
                var parent = new ParserSequenceAndSection() { Sequence = sequence, Section = null };
                string groupName = sequence.SequenceName;
                if (groupName.Contains(":::"))
                {
                    int strIndex = groupName.IndexOf(":::");
                    groupName = groupName.Substring(0, strIndex);
                }
                var parserResult = new ParserResult() { 
                    Name = sequence.SequenceName, 
                    GroupName = groupName,
                    Level = level, 
                    Parent = parent, 
                    Root = root 
                };
                root.Node = parserResult;
                parent.Node = parserResult;

                var result = CheckSequence(input, lexer, sequence, index, level + 1, parent, root, showOnConsole);

                //ParsedSequenceLog.Add((level, "After", sequence.SequenceName));
                //ParsedLog.Add((level, "After", sequence.SequenceName, ""));

                if (CancelAllParsing)
                {
                    return (false, null, null);
                }

                index = result.Item1;
                found = result.Item2;
                if (index != endIndex)
                {
                    found = false;
                    index = 0;
                }
                else if (found)
                {
                    parserResult.InnerResults = result.Item3;
                    //root.Node = parserResult;
                    //parent.Node = parserResult;
                    items.Add(parserResult);
                    return (found, currentSequence, items);
                }
            }
            return (found, currentSequence, items);
        }
        public List<ParserResult> OrganizeParentNodes(List<ParserResult> nodes, int level = 0, ParserResult parent = null)
        {
            foreach (var node in nodes)
            {
                if (level == 0)
                {
                    node.Parent.Node = node;
                    node.Root.Node = node;
                }
                else
                {
                    if (parent != null)
                    {
                        node.Parent.Node = parent;
                    }
                }
                if (node.InnerResults != null)
                {
                    foreach (var node1 in node.InnerResults)
                    {
                        node1.Parent.Node = node;
                        if (node1.InnerResults != null)
                        {
                            node1.InnerResults = OrganizeParentNodes(node1.InnerResults, level + 1, node1);
                        }
                    }
                }
            }
            return nodes;
        }
        public Result Parse(string input, Lexer lexer = null, string sequenceName = "", bool showOnConsole = false)
        {
            InitParserLog();
            Lexer.LexerResult lexerResult = InputLexer.GetSpans(input);
            (bool, ParserSequence, List<ParserResult>) result = (false, null, new List<ParserResult>());
            if (lexer != null && sequenceName != null)
            {
                result = this.Check(lexerResult, lexer, sequenceName, showOnConsole);
            }
            else if (lexer != null)
            {
                result = this.Check(lexerResult, lexer, showOnConsole: showOnConsole);
            }
            else if (sequenceName != null)
            {
                result = this.Check(lexerResult, InputLexer, sequenceName, showOnConsole);
            }
            else
            {
                result = this.Check(lexerResult, InputLexer);
            }
            var items = OrganizeParentNodes(result.Item3);
            return new Result() { Matched = result.Item1, LexerResult = lexerResult, Sequence = result.Item2, Results = items };
        }
        void AddEBNFRuleSectionsMainBlock(string identifier, string input)
        {
            var result = Parse(input, sequenceName: "rule");
            var id = result.Results[0].GetDescendantsOfType(new string[] { "lhs" })[0].InnerResultsText.Trim();
            var rhs = result.Results[0].GetDescendantsOfType(new string[] { "rhs" })[0].InnerResultsText.Trim();
            Sequences.Add(new ParserSequence()
            {
                SequenceName = id,
                Sections = new List<SequenceSection>()
                {
                    new SequenceSection() { SequenceList = rhs }
                }
            });
        }
        bool IsSequence(string name)
        {
            return Sequences.Where(x => x.SequenceName == name).Count() > 0;
        }
        bool IsToken(string name)
        {
            return InputLexer.Rules.Where(x => x.RuleName == name).Count() > 0;
        }
        void AddEBNFRuleSections(string ruleIdentifier, string identifier, string inputRule, string outerText)
        {
            List<SequenceSection> seqSections = new List<SequenceSection>();
            var sections = inputRule.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray();
            //Console.WriteLine($"Adding rule:{identifier}");
            foreach (var section in sections)
            {
                bool opt = false;
                bool rep = false;
                string strSection = section;
                if (strSection.Contains("/Opt")) { opt = true; }
                if (strSection.Contains("/Rep")) { rep = true; }
                strSection = strSection.Replace("/Opt", "");
                strSection = strSection.Replace("/Rep", "");
                var choices = strSection.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray();
                List<string> tokens = new List<string>();
                List<string> sequences = new List<string>();
                List<string> unknowns = new List<string>();
                foreach (var choice in choices)
                {
                    if (IsSequence(choice))
                    {
                        sequences.Add(choice);
                    }
                    else if (IsToken(choice))
                    {
                        tokens.Add(choice);
                    }
                    else if (choice == ruleIdentifier || choice == identifier)
                    {
                        sequences.Add(choice);
                    }
                    else if (choice.StartsWith("&"))
                    {
                        sequences.Add(choice.Replace("&", ""));
                    }
                    else if (choice.StartsWith("@"))
                    {
                        tokens.Add(choice.Replace("@", ""));
                    }
                    else
                    {
                        unknowns.Add(choice);
                        //throw new Exception($"Error adding rule section {identifier}");
                    }
                }
                string tokensList = string.Join(",", tokens);
                string sequencesList = string.Join(",", sequences);
                string unknownsList = string.Join(",", unknowns);
                //Console.WriteLine($"Section - Tokens:{tokensList}, Sequences:{sequencesList}, Unknowns:{unknownsList}, Opt:{opt}, Rep:{rep}");
                seqSections.Add(new SequenceSection()
                {
                    Unknowns = unknownsList,
                    TokenList = tokensList,
                    SequenceList = sequencesList,
                    IsOptional = opt,
                    IsRepeating = rep
                });
            }
            Sequences.Add(new ParserSequence()
            {
                SequenceName = identifier,
                Sections = seqSections
            });
        }
        public void RemoveEBNFRule(string ruleName)
        {
            // Remove any existing occurrences with the same identifier
            List<(int, string)> removeList = new List<(int, string)>();
            int idx = 0;
            foreach (var seq in Sequences)
            {
                if (seq.SequenceName.StartsWith(ruleName + ":::"))
                {
                    removeList.Add((idx, seq.SequenceName));
                }
                idx++;
            }
            removeList = removeList.OrderByDescending(x => x.Item1).ToList();
            for (int i = 0; i < removeList.Count; i++)
            {
                Sequences.RemoveAt(removeList[i].Item1);
            }
            removeList = new List<(int, string)>();
            idx = 0;
            foreach (var seq in InputLexer.Rules)
            {
                if (seq.RuleName.StartsWith("str" + ruleName + ":::"))
                {
                    removeList.Add((idx, seq.RuleName));
                }
                idx++;
            }
            removeList = removeList.OrderByDescending(x => x.Item1).ToList();
            for (int i = 0; i < removeList.Count; i++)
            {
                InputLexer.Rules.RemoveAt(removeList[i].Item1);
            }
        }
        public class EBNFUpdateList
        {
            public string IdentifierName { get; set; }
            public string OuterResultsText { get; set; }
            public int OuterResultLength { get { return OuterResultsText.Length; } }
            public int MinStart { get; set; }
            public int MaxEnd { get; set; }
            public int Level { get; set; }
            public bool Updated { get; set; }
            public ParserResult Outer { get; set; }
            public List<ParserResult> Inner { get; set; }
            public string InnerResultsText { get
                {
                    StringBuilder sb = new StringBuilder();
                    foreach(var item in Inner)
                    {
                        sb.Append(item.InnerResultsText);
                    }
                    return sb.ToString();
                } }
            public int InnerMinStart { get
                {
                    int min = int.MaxValue;
                    foreach(var item in Inner)
                    {
                        min = Math.Min(min, item.MinStart());
                    }
                    return min;
                } }
            public int InnerMaxEnd
            {
                get
                {
                    int max = int.MinValue;
                    foreach (var item in Inner)
                    {
                        max = Math.Max(max, item.MaxEnd());
                    }
                    return max;
                }
            }
            public int InnerResultTextLength
            {
                get { return InnerResultsText.Length; }
            }
        }
        public void EBNFAddRuleFast(string input)
        {
            string mainBlock = input;
            var firstResult = Parse(input, sequenceName: "rule");
            if (firstResult.Matched)
            {
                var rhs = firstResult.Results[0].GetDescendantsOfType(new string[] { "rhs" }).OrderBy(x => x.Level).First();
                string rhsText = rhs.InnerResultsText;

                Console.WriteLine($"Original:'{input}' Length:{input.Length}");
                Console.WriteLine($"Main Block RHS:{rhsText}, Length:{rhsText.Length}");
                Console.WriteLine();

                // Get lhs Identifier Name
                var identifier = firstResult.Results[0].GetDescendantsOfType(new string[] { "lhs" })[0].InnerResultsText.Trim();
                // Get EBNF Groups and ensure valid names
                //(string identifierName, ParserResult result, List<(string idName, bool contains, bool containedBy, bool isSameNode)> containrInfo)[] firstDescendants = firstResult.Results[0].GetEBNFGroupsWithContainmentMap(identifier, true).OrderBy(x => x.Item2.MinStart()).ToArray();
                var firstDescendants = firstResult.Results[0].GetEBNFGroupsWithContainmentMapSimplified(identifier, true).OrderBy(x => x.Item2.MinStart()).ToArray();
                var descendantsCopy = (firstDescendants.Clone() as (string identifierName, ParserResult result, List<string> contains, List<string> containedBy, string firstContainedBy)[]).ToList();
                List<(bool finished, string identifierName)> finishedList = new List<(bool, string)>();
                List<string> doneItems = new List<string>();
                foreach (var item in firstDescendants)
                {
                    finishedList.Add((false, item.identifierName));
                }
                bool doneFinished = false;
                List<(string, string, int, int, int)> addARuleForThisItem = new List<(string, string, int, int, int)>();

                var originalText = firstDescendants.Select(x => new EBNFUpdateList()
                {
                    IdentifierName = x.identifierName,
                    OuterResultsText = x.result.InnerResultsText,
                    MinStart = x.result.MinStart(),
                    MaxEnd = x.result.MaxEnd(),
                    Level = x.result.Level,
                    Outer = x.result
                }).ToArray();
                var textToUpdate = firstDescendants.Select(x => new EBNFUpdateList()
                {
                    IdentifierName = x.identifierName,
                    OuterResultsText = x.result.InnerResultsText,
                    MinStart = x.result.MinStart(),
                    MaxEnd = x.result.MaxEnd(),
                    Level = x.result.Level,
                    Outer = x.result
                }).ToArray();
                foreach(var outer in originalText)
                {
                    List<ParserResult> inner = outer.Outer.InnerResults;
                    if (inner.Count > 2)
                    {
                        inner = outer.Outer.InnerResults.GetRange(1, outer.Outer.InnerResults.Count - 2);
                    }
                    else if (outer.IdentifierName == "ebnfTerminal")
                    {
                        inner = new List<ParserResult>();
                        inner.Add(outer.Outer.InnerResults[0].InnerResults[1]);
                    }
                    outer.Inner = inner;
                }
                foreach(var outer in textToUpdate)
                {
                    List<ParserResult> inner = outer.Outer.InnerResults;
                    if (inner.Count > 2)
                    {
                        inner = outer.Outer.InnerResults.GetRange(1, outer.Outer.InnerResults.Count - 2);
                    }
                    else if (outer.IdentifierName == "ebnfTerminal")
                    {
                        inner = new List<ParserResult>();
                        inner.Add(outer.Outer.InnerResults[0].InnerResults[1]);
                    }
                    outer.Inner = inner;
                }
                var notFinishedList = finishedList.Where(x => x.finished == false);

                var joined1 = notFinishedList.Join(descendantsCopy, x => x.identifierName, y => y.identifierName, (notF, desc) => new
                {
                    notF.finished,
                    notF.identifierName,
                    desc.result,
                    desc.containedBy,
                    desc.contains,
                    desc.firstContainedBy
                }).OrderByDescending(x => x.result.MinStart()).Where(x => x.contains.Count == 0).ToArray();

                foreach (var currentItem in joined1.Where(x => x.result.Name == "ebnfTerminal").OrderByDescending(x => x.result.Level))
                {
                    var outerItem = currentItem.result.InnerResults[0].InnerResults;
                    var currentMeta = currentItem.result.InnerResultsMeta;
                    for (int i = 0; i < outerItem.Count; i++)
                    {
                        if (i != 0 && i != outerItem.Count - 1)
                        {
                            var innerMetaItem = outerItem[i];
                            var innerMetaInfo = innerMetaItem.InnerResultsMeta;
                            Console.WriteLine($"-  Inner:{innerMetaItem.InnerResultsText}, Meta:{innerMetaInfo.Item1}/{innerMetaInfo.Item2}/{innerMetaInfo.Item3}/{innerMetaInfo.Item4}");
                            Console.WriteLine($"Add: {currentItem.identifierName}, {innerMetaItem.InnerResultsText}");

                            string addRuleText = innerMetaItem.InnerResultsText;
                            InputLexer.Rules.Add(new Lexer.LexerRules.StringLexerRule("str" + currentItem.identifierName, addRuleText));

                            Sequences.Add(new Parser.ParserSequence()
                            {
                                SequenceName = currentItem.identifierName,
                                Sections = new List<Parser.SequenceSection>()
                                        {
                                            new Parser.SequenceSection() { TokenList = "str" + currentItem.identifierName }
                                        }
                            });

                            input = input.Remove(currentItem.result.MinStart(), currentItem.result.InnerResultsText.Length);
                            input = input.Insert(currentItem.result.MinStart(), currentItem.identifierName);

                            Console.WriteLine($"Update: FirstContainedBy:{currentItem.firstContainedBy}");
                            addARuleForThisItem.Add((currentItem.identifierName, innerMetaItem.InnerResultsText, innerMetaInfo.Item2, innerMetaInfo.Item3, innerMetaInfo.Item4));
                            for (int ii = 0; ii < finishedList.Count; ii++)
                            {
                                if (finishedList[ii].identifierName == currentItem.identifierName)
                                {
                                    finishedList[ii] = (true, currentItem.identifierName);
                                }
                            }

                            for (int ii = 0; ii < textToUpdate.Length; ii++)
                            {
                                if (textToUpdate[ii].IdentifierName == currentItem.firstContainedBy)
                                {
                                    var updateItem = textToUpdate[ii];
                                    string newText = updateItem.OuterResultsText;
                                    int newStart = currentMeta.Item2 - updateItem.MinStart;
                                    int newEnd = currentMeta.Item3 - updateItem.MinStart;
                                    int newLength = currentMeta.Item4;
                                    newText = newText.Remove(newStart, newLength);
                                    //newText = newText.Insert(newStart, currentItem.result.InnerResultsText); // this would get you back to the original
                                    newText = newText.Insert(newStart, currentItem.identifierName);
                                    updateItem.OuterResultsText = newText;
                                    textToUpdate[ii] = updateItem;
                                }
                            }
                        }
                    }
                }

                while (!doneFinished)
                {
                    notFinishedList = finishedList.Where(x => x.finished == false);
                    if (notFinishedList.Count() == 0)
                    {
                        doneFinished = true;
                        break;
                    }
                    var finishedList2 = finishedList.Where(x => x.finished);
                    for (int i = descendantsCopy.Count - 1; i >= 0; i--)
                    {
                        var current = descendantsCopy[i];
                        for (int ii = current.contains.Count - 1; ii >= 0; ii--)
                        {
                            string currentContains = current.contains[ii];
                            if (finishedList2.Count(x => x.identifierName == currentContains) > 0)
                            {
                                current.contains.RemoveAt(ii);
                            }
                        }
                        for (int ii = current.containedBy.Count - 1; ii >= 0; ii--)
                        {
                            string currentContains = current.containedBy[ii];
                            if (finishedList2.Count(x => x.identifierName == currentContains) > 0)
                            {
                                current.containedBy.RemoveAt(ii);
                            }
                        }
                        if (current.containedBy.Count > 0)
                        {
                            current.firstContainedBy = current.containedBy.OrderByDescending(x => x).First();
                        }
                        if (finishedList2.Count(x => x.identifierName == current.identifierName) > 0)
                        {
                            descendantsCopy.RemoveAt(i);
                        }
                    }

                    joined1 = notFinishedList.Join(descendantsCopy, x => x.identifierName, y => y.identifierName, (notF, desc) => new
                    {
                        notF.finished,
                        notF.identifierName,
                        desc.result,
                        desc.containedBy,
                        desc.contains,
                        desc.firstContainedBy
                    }).OrderByDescending(x => x.result.MinStart()).Where(x => x.contains.Count == 0).ToArray();

                    foreach (var currentItem in joined1)
                    {
                        var currentText = currentItem.result.InnerResultsText;
                        var currentMeta = currentItem.result.InnerResultsMeta;
                        string updatedText = "";
                        int updatedMinStart = 0;
                        bool updatedYet = false;
                        if (textToUpdate.Count(x => x.IdentifierName == currentItem.identifierName) > 0)
                        {
                            var updated = textToUpdate.First(x => x.IdentifierName == currentItem.identifierName);
                            updatedText = updated.OuterResultsText;
                            updatedMinStart = updated.MinStart;
                            updatedYet = updated.Updated;
                        }

                        Console.WriteLine($"- FirstContainedBy:{currentItem.firstContainedBy}");
                        Console.WriteLine($"- Outer Current:{currentItem.identifierName} Text:{currentText}, Meta:{currentMeta.Item1}/{currentMeta.Item2}/{currentMeta.Item3}/{currentMeta.Item4}");
                        //if (updatedText != currentText)
                        //{
                        //    Console.WriteLine($"- Outer Difference: Orig:{currentText}, Updated:{updatedText}");
                        //}

                        var outerItem = currentItem.result.GetDescendantsOfType(new string[] { "rhs" }).OrderBy(x => x.Level).First();
                        var innerMetaInfo = outerItem.InnerResultsMeta;
                        Console.WriteLine($"-  Inner:{outerItem.Name}/{innerMetaInfo.Item1}/{innerMetaInfo.Item2}/{innerMetaInfo.Item3}/{innerMetaInfo.Item4}");
                        string addText = innerMetaInfo.Item1;
                        string append = "";
                        bool didUseUpdatedText = false;
                        if (updatedText != addText && updatedYet)
                        {
                            addText = updatedText;
                            didUseUpdatedText = true;
                        }
                        string updateFirstContainedBy = currentItem.firstContainedBy;
                        if (updateFirstContainedBy == "")
                        {
                            updateFirstContainedBy = "MAIN_BLOCK";
                        }
                        Console.WriteLine($"Add: {currentItem.identifierName}:, {addText}");
                        Console.WriteLine($"Update: FirstContainedBy:{updateFirstContainedBy}");
                        string outerText = currentItem.result.InnerResultsText;

                        if (!didUseUpdatedText)
                        {
                            if (outerText.Trim().StartsWith("{")) { append += "/Opt/Rep"; }
                            else if (outerText.Trim().StartsWith("[")) { append += "/Opt"; }
                            else if (outerText.Trim().StartsWith("%%")) { append += "/Rep"; }
                        }
                        else
                        {
                            string at = addText.Trim();
                            if (at.StartsWith("{")) { append += "/Opt/Rep"; at = at.Substring(1); at = at.Substring(0, at.Length - 1); }
                            else if (at.StartsWith("[")) { append += "/Opt"; at = at.Substring(1); at = at.Substring(0, at.Length - 1); }
                            else if (at.StartsWith("%%")) { append += "/Rep"; at = at.Substring(2); at = at.Substring(0, at.Length - 2); }
                            addText = at;
                        }

                        AddEBNFRuleSections(identifier, currentItem.identifierName, addText, outerText);

                        addARuleForThisItem.Add((currentItem.identifierName, outerItem.InnerResultsText, innerMetaInfo.Item2, innerMetaInfo.Item3, innerMetaInfo.Item4));

                        for (int ii = 0; ii < finishedList.Count; ii++)
                        {
                            if (finishedList[ii].identifierName == currentItem.identifierName)
                            {
                                finishedList[ii] = (true, currentItem.identifierName);
                            }
                        }

                        for (int ii = 0; ii < textToUpdate.Length; ii++)
                        {
                            if (textToUpdate[ii].IdentifierName == currentItem.firstContainedBy)
                            {
                                var updateItem = textToUpdate[ii];
                                string newText = updateItem.OuterResultsText;
                                int newStart = currentMeta.Item2 - updateItem.MinStart;
                                int newEnd = currentMeta.Item3 - updateItem.MinStart;
                                int newLength = currentMeta.Item4;
                                newText = newText.Remove(newStart, newLength);
                                //newText = newText.Insert(newStart, currentItem.result.InnerResultsText); // this would get you back to the original
                                newText = newText.Insert(newStart, currentItem.identifierName + append);
                                updateItem.OuterResultsText = newText;
                                updateItem.Updated = true;
                                textToUpdate[ii] = updateItem;
                            }
                        }
                        Console.WriteLine();
                    }
                }

                string id_main_block = identifier + ":::" + "rule_main_block:0:0";
                string mainBlockText = rhsText;
                var rhsMeta = rhs.InnerResultsMeta;

                int idx = 0;
                var hi = Sequences;
                var hi2 = InputLexer.Rules;
            }
            Console.WriteLine();
        }
        public void AddEBNFRule(string input)
        {
            var firstResult = Parse(input, sequenceName: "rule");
            if (firstResult.Matched)
            {
                var identifier = firstResult.Results[0].GetDescendantsOfType(new string[] { "lhs" }).OrderBy(x => x.MinStart()).ToArray();
                string identifierText = identifier[0].InnerResultsText.Trim();
                var allTexts = firstResult.Results[0].GetDescendantsOfType(new string[] { "ebnfTerminal" }).OrderByDescending(x => x.MinStart()).ToArray();
                List<(string identifier, bool optional, bool repeating, string text, bool continueProcessing)> textStrings = new List<(string, bool, bool, string, bool)>();
                int idx = 0;
                foreach(var item in allTexts)
                {
                    string text = item.InnerResultsText;
                    var meta = item.InnerResultsMeta;
                    string newString = identifierText + ":::" + "ebnfTerminal" + ":" + item.Level + ":" + idx;
                    if (text.StartsWith("\"") || text.StartsWith("'")) { text = text.Substring(1, text.Length - 1); }
                    if (text.EndsWith("\"") || text.EndsWith("'")) { text = text.Substring(0, text.Length - 1); }
                    //textStrings.Add((newString, false, false, text, true, true));
                    textStrings.Add((newString, false, false, "str"+newString, true));
                    InputLexer.Rules.Add(new Lexer.LexerRules.StringLexerRule("str"+newString, text));
                    int start = (item.Span != null) ? item.Span.Start : item.InnerResultsMeta.Item2;
                    item.Span = new Lexer.Span() { Text = newString, Start = start, InnerSpans = new List<Lexer.Span>() };
                    item.InnerResults = new List<ParserResult>();
                    idx++;
                }
                var groups = firstResult.Results[0].GetEBNFGroupsWithContainmentMapSimplified(identifierText, includeStringLiterals: false).OrderByDescending(x => x.result.Level).ThenByDescending(x => x.result.MinStart()).ToArray();
                foreach(var item in groups)
                {
                    string text = item.result.InnerResultsText;
                    var meta = item.result.InnerResultsMeta;
                    string newString = identifierText + ":::" + item.result.Name + ":" + item.result.Level + ":" + idx;
                    text = text.Trim();
                    bool isOptional = false;
                    bool isRepeating = false;
                    if (text.StartsWith("[")) { text = text.Substring(1, text.Length - 2); isOptional = true; }
                    else if (text.StartsWith("{")) { text = text.Substring(1, text.Length - 2); isOptional = true; isRepeating = true; }
                    else if (text.StartsWith("(")) { text = text.Substring(1, text.Length - 2); }
                    else if (text.StartsWith("%%")) { text = text.Substring(2, text.Length - 4); isRepeating = true; }
                    text = text.Trim();
                    textStrings.Add((newString, isOptional, isRepeating, text, true));
                    int start = (item.result.Span != null) ? item.result.Span.Start : item.result.InnerResultsMeta.Item2;
                    item.result.Span = new Lexer.Span() { Text = newString, Start = start, InnerSpans = new List<Lexer.Span>() };
                    item.result.InnerResults = new List<ParserResult>();
                    idx++;
                }
                var mainBlock = firstResult.Results[0].GetDescendantsOfType(new string[] { "rhs" }).OrderBy(x => x.Level).ThenBy(x => x.MinStart()).ToArray();
                string mainBlockText = mainBlock[0].InnerResultsText;
                string mainBlockId = identifierText + ":::" + "main_block" + ":" + 0 + ":" + 0;
                textStrings.Add((mainBlockId, false, false, mainBlockText, true));
                textStrings.Add((identifierText, false, false, mainBlockId, false));
                for(int i=0;i<textStrings.Count;i++)
                {
                    int idx1 = i;
                    var item = textStrings[i];
                    if (item.continueProcessing)
                    {
                        for (int ii = 0; ii < idx1; ii++)
                        {
                            var item1 = textStrings[ii];
                            string append = "";
                            if (item1.optional) { append += "/Opt"; }
                            if (item1.repeating) { append += "/Rep"; }
                            string text = item1.identifier + append;
                            item.text = item.text.Replace(item1.identifier, text);
                        }
                        item.continueProcessing = false;
                        textStrings[i] = item;
                    }
                }
                for(int i=0;i<textStrings.Count;i++)
                {
                    AddEBNFRuleSections(identifierText, textStrings[i].identifier, textStrings[i].text, "");
                }
            }
        }
        public void AddEBNFRuleDeprecated(string inputEBNF)
        {
            var firstResult = Parse(inputEBNF, sequenceName: "rule");
            if (firstResult.Matched)
            {
                // Get lhs Identifier Name
                var identifier = firstResult.Results[0].GetDescendantsOfType(new string[] { "lhs" })[0].InnerResultsText.Trim();

                RemoveEBNFRule(identifier);

                // Get EBNF Groups and ensure valid names
                var firstDescendants = firstResult.Results[0].GetEBNFGroups(identifier, true).OrderBy(x => x.Item2.MinStart()).ToList();
                List<(bool finished, string nodeName, ParserResult node)> finishedList = new List<(bool, string, ParserResult)>();
                for (int i = 0; i < firstDescendants.Count; i++)
                {
                    finishedList.Add((false, firstDescendants[i].Item1, firstDescendants[i].Item2));
                }

                foreach (var item in firstDescendants)
                {
                    var results2 = Parse(item.Item1, sequenceName: "ebnfIdentifierGroupPlaceholder");
                    // To Do: do something here if not a valid name
                    if (results2.Matched == false)
                    {
                        throw new Exception("Identifier error adding EBNF rule");
                    }
                }

                Console.WriteLine("Adding EBNF rule:" + identifier);
                //Console.WriteLine(inputEBNF);

                bool doneTokens = false;
                while (!doneTokens)
                {
                    var res_a = Parse(inputEBNF, sequenceName: "rule");
                    var tokenDescendants = res_a.Results[0].GetEBNFGroups(identifier, true);
                    var tokenCount = tokenDescendants.Where(x => x.Item2.Name.StartsWith("ebnfTerminal")).OrderByDescending(x => x.Item2.MinStart()).Count();
                    if (tokenCount == 0)
                    {
                        doneTokens = true;
                    }
                    else
                    {
                        var item = tokenDescendants.OrderByDescending(x => x.Item2.MinStart()).Where(x => x.Item2.Name.StartsWith("ebnfTerminal")).FirstOrDefault();
                        var original = finishedList.OrderByDescending(x => x.node.MinStart()).Where(x => x.node.Name.StartsWith("ebnfTerminal") && x.finished == false).FirstOrDefault();
                        var originalItem = original.node;

                        // Is a string
                        var resOuter = item.Item2;
                        int minOuter = resOuter.MinStart();
                        int maxOuter = resOuter.MaxEnd();
                        string textOuter = resOuter.InnerResultsText;
                        int textOuterLen = textOuter.Length;
                        var resInner = item.Item2.InnerResults[0].InnerResults[1];
                        int minInner = resInner.MinStart();
                        int maxInner = resInner.MaxEnd();
                        string textInner = resInner.InnerResultsText;
                        int textInnerLen = textInner.Length;

                        // Add token rule
                        string tokenName = "str" + item.Item1;
                        Lexer.LexerRules.StringLexerRule strRule = new Lexer.LexerRules.StringLexerRule(tokenName, textInner);
                        InputLexer.Rules.Add(strRule);
                        Sequences.Add(new Parser.ParserSequence()
                        {
                            SequenceName = item.Item1,
                            Sections = new List<Parser.SequenceSection>()
                            {
                                new Parser.SequenceSection() { TokenList = "str" + original.nodeName }
                            }
                        });

                        original.finished = true;
                        for (int a = 0; a < finishedList.Count; a++)
                        {
                            if (finishedList[a].node == original.node)
                            {
                                finishedList[a] = original;
                            }
                        }
                        inputEBNF = inputEBNF.Remove(minOuter, textOuterLen);
                        inputEBNF = inputEBNF.Insert(minOuter, tokenName);
                        //Console.WriteLine(inputEBNF);
                    }
                }

                bool doneSequences = false;
                while (!doneSequences)
                {
                    firstResult = Parse(inputEBNF, sequenceName: "rule", showOnConsole: false);
                    if (!firstResult.Matched)
                    {
                        throw new Exception("A parsing error occurred while adding EBNF Rule.");
                    }
                    var descendantsMap = firstResult.Results[0].GetEBNFGroupsWithContainmentMap(identifier);
                    if (descendantsMap.Count == 0)
                    {
                        doneSequences = true;
                    }
                    var descendantsCurrent = descendantsMap.Where(x => x.Item3.Count(x1 => x1.Item2 == true) == 0).FirstOrDefault();
                    if (!doneSequences)
                    {
                        //var item = tokenDescendants.OrderByDescending(x => x.Item2.MinStart()).Where(x => x.Item2.Name.StartsWith("ebnfTerminal")).FirstOrDefault();
                        //var original = finishedList.OrderByDescending(x => x.node.MinStart()).Where(x => x.node.Name.StartsWith("ebnfTerminal") && x.finished == false).FirstOrDefault();
                        //var originalItem = original.node;

                        var item = descendantsCurrent;
                        //var original = finishedList.Where(x => x.node.InnerResultsText == item.Item2.InnerResultsText && x.finished == false).FirstOrDefault();
                        var original = finishedList.Where(x => x.finished == false &&
                            x.node.Parent.Node.Level == item.result.Parent.Node.Level &&
                            x.node.Parent.Node.Name == item.result.Parent.Node.Name).FirstOrDefault();
                        if (original.node == null)
                        {
                            throw new Exception("An errror happened when adding a rule sequence.");
                        }
                        var originalItem = original.node;

                        var resOuter = item.result;
                        int minOuter = resOuter.MinStart();
                        int maxOuter = resOuter.MaxEnd();
                        string textOuter = resOuter.InnerResultsText;
                        int textOuterLen = textOuter.Length;

                        var resInner = resOuter.InnerResults.Where(x => x.Name == "rhs").ToArray();
                        int minInner = resInner.Min(x => x.MinStart());
                        int maxInner = resInner.Max(x => x.MaxEnd());
                        string textInner = "";
                        foreach (var itemInner in resInner)
                        {
                            textInner += itemInner.InnerResultsText;
                        }
                        int textInnerLen = textInner.Length;

                        var cti = descendantsCurrent.Item3.Where(x => x.isSameNode == false && (x.containedBy || x.contains)).ToArray();
                        var containerInfo = descendantsCurrent.containerInfo;

                        var containsCount = containerInfo.Count(x => x.contains);

                        bool isContainedByMain = firstResult.Results[0].ContainsNode(item.result);
                        bool isMainNode = firstResult.Results[0] == item.result;

                        //string sequenceName = item.identifierName;
                        string sequenceName = original.nodeName;

                        if (containsCount == 0)
                        {
                            string append = "";
                            if (textOuter.StartsWith("{")) { append += "/Opt/Rep"; }
                            else if (textOuter.StartsWith("[")) { append += "/Opt"; }
                            else if (textOuter.StartsWith("%%")) { append += "/Rep"; }

                            inputEBNF = inputEBNF.Remove(minOuter, textOuterLen);
                            inputEBNF = inputEBNF.Insert(minOuter, sequenceName + append);

                            AddEBNFRuleSections(identifier, sequenceName, textInner, textOuter);

                            original.finished = true;
                            for (int a = 0; a < finishedList.Count; a++)
                            {
                                if (finishedList[a].node == original.node)
                                {
                                    finishedList[a] = original;
                                }
                            }

                            //Console.WriteLine(inputEBNF);
                        }
                    }
                }

                firstResult = Parse(inputEBNF, sequenceName: "rule", showOnConsole: false);
                var descendants1 = firstResult.Results[0].GetDescendantsOfType(new string[] { "rhs" }).OrderBy(x => x.Level);

                string id_main_block = identifier + ":::" + "rule_main_block:0:0";
                string mainBlockText = descendants1.ToArray()[0].InnerResultsText;
                AddEBNFRuleSections(identifier, id_main_block, mainBlockText, "");

                if (descendants1.Count() > 0)
                {
                    var resOuter = descendants1.ToArray()[0];
                    int minOuter = resOuter.MinStart();
                    int maxOuter = resOuter.MaxEnd();
                    string textOuter = resOuter.InnerResultsText;
                    int textOuterLen = textOuter.Length;

                    string append = "";
                    if (textOuter.StartsWith("{")) { append += "/Opt/Rep"; }
                    else if (textOuter.StartsWith("[")) { append += "/Opt"; }
                    else if (textOuter.StartsWith("%%")) { append += "/Rep"; }

                    inputEBNF = inputEBNF.Remove(minOuter, textOuterLen);
                    inputEBNF = inputEBNF.Insert(minOuter, id_main_block + append);
                    //Console.WriteLine(inputEBNF);
                }

                string id_rule = identifier + ":::" + "rule:0:0";
                AddEBNFRuleSectionsMainBlock(id_rule, inputEBNF);

                firstResult = Parse(inputEBNF, sequenceName: "rule", showOnConsole: false);
                if (!firstResult.Matched)
                {
                    throw new Exception("Error adding rule");
                }
            }
        }
    }
}