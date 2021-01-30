using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LexerParser
{
    /// <summary>
    /// A parser that parses text using user-defined rules.
    /// </summary>
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
            //public bool Evaluate(string input, Lexer lexer)
            //{
            //    int index = 0;
            //    foreach (var section in Sections)
            //    {
            //        bool optional = section.IsOptional;
            //        bool repeating = section.IsRepeating;
            //        EvaluateSequenceSectionResult result = null;
            //        if (repeating)
            //        {
            //            bool foundAtLeastOnce = false;
            //            for (int i1 = 1; i1 <= input.Length; i1++)
            //            {
            //                string str = input.Substring(index, i1);
            //                EvaluateSequenceSectionResult result1 = EvaluateSequenceSection(str, lexer, section);
            //                if (result1 != null)
            //                {
            //                    if (!string.IsNullOrEmpty(result1.MatchedRule))
            //                    {
            //                        foundAtLeastOnce = true;
            //                        result = result1;
            //                        break;
            //                    }
            //                    else
            //                    {
            //                        return foundAtLeastOnce;
            //                    }
            //                }
            //                else
            //                {
            //                    return foundAtLeastOnce;
            //                }
            //            }
            //        }
            //        else
            //        {
            //            for (int i1 = 1; i1 <= input.Length; i1++)
            //            {
            //                string str = input.Substring(index, i1);
            //                EvaluateSequenceSectionResult result1 = EvaluateSequenceSection(str, lexer, section);
            //                if (result1 != null)
            //                {
            //                    if (!string.IsNullOrEmpty(result1.MatchedRule))
            //                    {
            //                        result = result1;
            //                        break;
            //                    }
            //                }
            //            }
            //        }
            //        if (!optional && result == null) { return false; }
            //    }
            //    return true;
            //}
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
            string _TokenList;
            public bool HasTokens { get; set; }
            public string TokenList { get { return _TokenList; } set {
                    _TokenList = value;
                    HasTokens = !string.IsNullOrEmpty(_TokenList);
                    _Tokens = !string.IsNullOrEmpty(_TokenList) ? _TokenList.Split(',').Select(x => x.Trim()).ToArray() : null;
                } }
            string _SequenceList;
            public bool HasSequences { get; set; }
            public string SequenceList { get { return _SequenceList; } set {
                    _SequenceList = value;
                    HasSequences = !string.IsNullOrEmpty(_SequenceList);
                    _Sequences = !string.IsNullOrEmpty(_SequenceList) ? _SequenceList.Split(',').Select(x => x.Trim()).ToArray() : null;
                } }
            string _DynamicRulesList;
            public bool HasDyanmicRules { get; set; }
            public string DynamicRulesList
            {
                get { return _DynamicRulesList; }
                set
                {
                    _DynamicRulesList = value;
                    HasDyanmicRules = !string.IsNullOrEmpty(_DynamicRulesList);
                    _DynamicRules = !string.IsNullOrEmpty(_DynamicRulesList) ? _DynamicRulesList.Split(',').Select(x => x.Trim()).ToArray() : null;
                }
            }
            string _EBNFItemList;
            [JsonProperty("ebnfItem")]
            public string EBNFItemList { get { return _EBNFItemList; } set {
                    _EBNFItemList = value;
                    _EBNFChoices = !string.IsNullOrEmpty(_EBNFItemList) ? _EBNFItemList.Split('|').Select(x => x.Trim()).ToArray() : null;
                    _EBNFConcatenations = !string.IsNullOrEmpty(_EBNFItemList) ? _EBNFItemList.Split(',').Select(x => x.Trim()).ToArray() : null;
                } }
            [JsonProperty("varName")]
            public string VariableName { get; set; }
            public string Unknowns { get; set; }
            string[] _Tokens;
            public string[] Tokens { get { return _Tokens; } }
            string[] _Sequences;
            public string[] Sequences { get { return _Sequences; } }
            string[] _DynamicRules;
            public string[] DynamicRules { get { return _DynamicRules; } }
            string[] _EBNFChoices;
            public string[] EBNFChoices { get { return _EBNFChoices; } }
            string[] _EBNFConcatenations;
            public string[] EBNFConcatenations { get { return _EBNFConcatenations; } }
            public string SyntaxColor { get; set; }
            public (string ruleName, string[] tokens, bool caseInsensitive)[] TokensLookedUp { get; set; }
            public bool ValidateAgainstTokenLookUps(string input)
            {
                return true;
            }
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
                    maxValue = Math.Max(maxValue, start.Span.End);
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
        public class SearchResult
        {
            public List<Result> Result { get; set; }
            public List<(int Line, ParserResult Data)> CombinedResults { get; set; } = new List<(int, ParserResult)>();
            public override string ToString()
            {
                return $"[Combined Results: {CombinedResults.Count}]";
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
            public string InnerResultsText
            {
                get
                {
                    StringBuilder sb = new StringBuilder();
                    //foreach (var item in Inner)
                    for(int i=0;i<Inner.Count;i++)
                    {
                        ParserResult item = Inner[i];
                        sb.Append(item.InnerResultsText);
                    }
                    return sb.ToString();
                }
            }
            public int InnerMinStart
            {
                get
                {
                    int min = int.MaxValue;
                    //foreach (var item in Inner)
                    for(int i=0;i<Inner.Count;i++)
                    {
                        ParserResult item = Inner[i];
                        min = Math.Min(min, item.MinStart());
                    }
                    return min;
                }
            }
            public int InnerMaxEnd
            {
                get
                {
                    int max = int.MinValue;
                    //foreach (var item in Inner)
                    for(int i=0;i<Inner.Count;i++)
                    {
                        ParserResult item = Inner[i];
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
        public class ReportProgressArgs : EventArgs
        {
            public int InputPosition { get; set; }
            public int InputSize { get; set; }
            public double Percent { get { return (double)InputPosition / (double)InputSize * 100.0; } }
            public ReportProgressArgs() : base()
            {

            }
        }
        public delegate void ReportProgressHandler(object sender, ReportProgressArgs e);
        public event ReportProgressHandler ReportProgress;
        public Lexer InputLexer { get; set; }
        public List<ParserSequence> Sequences { get; set; }
        public Dictionary<string, ParserSequence> SequencesDictionary { get; set; }
        public List<(int, string, string)> ParsedSequenceLog = new List<(int, string, string)>();
        public List<(int, string, string)> ParsedSequenceSectionLog = new List<(int, string, string)>();
        public List<(int, string, string, string)> ParsedLog = new List<(int, string, string, string)>();
        public int MaxLevel { get; set; } = 100;
        public int InputSize { get; set; }
        public int MaxParseLevel { get; set; } = -1;
        public int MaxParseIndex { get; set; } = -1;
        public int CurrentIndex { get; set; } = -1;
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
            SyncSequencesDictionary();
        }
        void InitParserLog()
        {
            ParsedSequenceLog = new List<(int, string, string)>();
            ParsedSequenceSectionLog = new List<(int, string, string)>();
            ParsedLog = new List<(int, string, string, string)>();
        }
        //public bool Evaluate(string input, Lexer lexer)
        //{
        //    foreach (var sequence in Sequences)
        //    {
        //        if (sequence.Evaluate(input, lexer)) return true;
        //    }
        //    return false;
        //}
        public ConcurrentDictionary<int, string[]> TokenMap { get; set; } = new ConcurrentDictionary<int, string[]>();
        public void BuildAllSequenceSectionTokenLookups()
        {
            foreach(var seq in Sequences)
            {
                foreach(var sec in seq.Sections.Where(x => x.Tokens != null))
                {
                    string[] tokens = sec.Tokens;
                    var rules = InputLexer.Rules.Where(x => tokens.Contains(x.RuleName)).ToArray();
                    List<(string, string[], bool)> tokenRules = new List<(string, string[], bool)>();
                    foreach (var r in rules)
                    {
                        var item = InputLexer.StringsPerRule[r.RuleName];
                        tokenRules.Add((r.RuleName, item.tokens.ToArray(), item.caseInsensitive));
                    }
                    sec.TokensLookedUp = tokenRules.ToArray();
                }
            }
        }
        //public void BuildAllTokensForLexer(List<Lexer.Span> spans)
        //{
        //    TokenMap = new ConcurrentDictionary<int, string[]>();
        //    List<string> strings = new List<string>();
        //    int max = spans.Max(x => x.End);
        //    //int idx = 0;
        //    //bool done = false;
        //    Parallel.For(0, max, (idx) =>
        //    {
        //        Parallel.ForEach(spans.Where(x => (x.Start <= idx && idx < x.End)), (span) =>
        //        {
        //            //TokenMap.Add(idx, span.Rule.RuleName); 
        //            if (!TokenMap.ContainsKey(idx))
        //            {
        //                TokenMap[idx] = new string[] { span.Rule.RuleName };
        //            }
        //            else
        //            {
        //                if (!TokenMap[idx].Contains(span.Rule.RuleName))
        //                {
        //                    string[] current = TokenMap[idx];
        //                    Array.Resize<string>(ref current, current.Length + 1);
        //                    current[current.Length - 1] = span.Rule.RuleName;
        //                    TokenMap[idx] = current;
        //                }
        //            }
        //        });
        //        Parallel.ForEach(spans.Where(x => x.InnerSpans.Any(x1 => x1.Start <= idx && idx < x.End)), (span) =>
        //        {
        //            Parallel.ForEach(span.InnerSpans.Where(x => x.Start <= idx && idx < x.End), (inner) =>
        //            {
        //                if (!TokenMap.ContainsKey(idx))
        //                {
        //                    TokenMap[idx] = new string[] { span.Rule.RuleName };
        //                }
        //                else
        //                {
        //                    if (!TokenMap[idx].Contains(inner.Rule.RuleName))
        //                    {
        //                        string[] current = TokenMap[idx];
        //                        Array.Resize<string>(ref current, current.Length + 1);
        //                        current[current.Length - 1] = inner.Rule.RuleName;
        //                        TokenMap[idx] = current;
        //                    }
        //                }
        //            });
        //        });
        //    });
        //}
        (int, bool, bool, List<ParserResult>, bool, bool) CheckSequenceSection(Lexer.LexerResult input, Lexer lexer, ParserSequence sequence, SequenceSection section, int index, bool foundOnce, List<ParserResult> variables, int level, ParserSequenceAndSection parent, ParserSequenceAndSection root, bool showOnConsole = false)
        {
repeat:
            //if (level > MaxLevel)
            //{
            //    CancelAllParsing = true;
            //    return (-1, false, false, null, false, false);
            //}
            //MaxParseLevel = Math.Max(level, MaxParseLevel);

            //string[] tokens = section.Tokens;
            //string[] sequences = section.Sequences;

            bool optional = section.IsOptional;
            bool repeating = section.IsRepeating;
            bool hasTokens = section.HasTokens;
            bool hasSequences = section.HasSequences;
            bool hasDynamicRules = section.HasDyanmicRules;

            //var spans = input.OrganizedSpans.Where(x => x.Start <= index && index < x.End).ToArray();
            //var spans = input.OrganizedSpans.Where(x => x.IsBetween(index)).ToArray();

            //List<Lexer.Span> spans = new List<Lexer.Span>();
            //for (int i1 = 0; i1 < input.OrganizedSpans.Count; i1++)
            //{
            //    Lexer.Span span1 = input.OrganizedSpans[i1];
            //    //if (span1.IsBetween(index)) { spans.Add(span1); }
            //    if (span1.Start <= index && index < span1.End) { spans.Add(span1); }
            //}

            //List<Lexer.Span> spans = new List<Lexer.Span>();
            //foreach (Lexer.Span span in input.OrganizedSpans)
            //{
            //    if (span.Start <= index && index < span.End) { spans.Add(span); }
            //}


            //spans = spans_A.OrderBy(x => x.Start).ToList();
            //if (loopResult.IsCompleted)
            //{
            //    spans = spans_A.OrderBy(x => x.Start).ToList();
            //}
            //else
            //{
            //    Console.WriteLine("NOT COMPLETED");
            //}



            //foreach (var item in input.OrganizedSpans)
            //{
            //    if (item.IsBetween(index)) { spans.Add(item); }
            //}

            bool found = false;
            if (hasTokens)
            {
                string[] tokens = section.Tokens;

                List<Lexer.Span> spans = new List<Lexer.Span>();
                for(int i1=0;i1<input.OrganizedPartitions.Count;i1++)
                {
                    Lexer.LexerResult.ResultPartition partition = input.OrganizedPartitions[i1];
                    if (partition.Minimum <= index && index < partition.Maximum)
                    {
                        List<Lexer.Span> spans_1a = partition.Spans;
                        for (int i3 = 0; i3 < spans_1a.Count; i3++)
                        {
                            Lexer.Span span_1a = spans_1a[i3];
                            if (span_1a.Start <= index && index < span_1a.End)
                            {
                                spans.Add(span_1a);
                            }
                        }
                    }

                    //List<Lexer.LexerResult.ResultPartition> partitions = input.OrganizedPartitions.Where(x => x.Minimum <= index && index < x.Maximum).ToList();
                    //for(int i2=0;i2<partitions.Count;i2++)
                    //{
                    //    List<Lexer.Span> spans_1a = partitions[i2].Spans;
                    //    spans.AddRange(spans_1a.Where(x => x.Start <= index && index < x.End).ToList());
                    //}
                }

                //List<Lexer.Span> spans = new List<Lexer.Span>();
                ////List<Lexer.Span> spans = input.DictionarySpans.Where(x => x.Key.start <= index && x.Key.end > index).Select(x => x.Value).ToList();
                //for (int i1 = 0; i1 < input.OrganizedSpans.Count; i1++)
                //{
                //    Lexer.Span span1 = input.OrganizedSpans[i1];
                //    if (index >= span1.Start && index < span1.End) { spans.Add(span1); }
                //    //if (span1.Start <= index && index < span1.End) { spans.Add(span1); }
                //}

                //foreach (string item in tokens)
                for (int iii = 0; iii < tokens.Length; iii++)
                {

                    string item = tokens[iii];
                    //Lexer.LexerRules.ILexerRule rule = lexer.Rules.Where(x => item.Equals(x.RuleName)).FirstOrDefault();
                    Lexer.LexerRules.ILexerRule rule = null;
                    if (lexer.RulesDictionary.ContainsKey(item))
                    {
                        rule = lexer.RulesDictionary[item];
                    }
                    if (rule != null)
                    {
                        //foreach (Lexer.Span span in spans)
                        for (int iiii = 0; iiii < spans.Count; iiii++)
                        {
                            Lexer.Span span = spans[iiii];
                            if (!found)
                            {
                                //if (span.Rule.RuleName == rule.RuleName)
                                if (rule.RuleName.Equals(span.Rule.RuleName))
                                {
                                    found = true;
                                    //foundInParsed = true;
                                    index += span.Length;
                                    CurrentIndex = index;
                                    //MaxParseIndex = Math.Max(index, MaxParseIndex);
                                    string groupName = rule.RuleName;
                                    if (groupName.IndexOf(":::") > -1)
                                    {
                                        int strIndex = groupName.IndexOf(":::");
                                        groupName = groupName.Substring(0, strIndex);
                                    }
                                    ParserResult result1 = new ParserResult
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
                                    break;
                                }
                            }
                            if (!found)
                            {
                                //Lexer.Span[] innerArray = span.InnerSpans.Where(x => x.Start <= index && index < x.End).ToArray();
                                List<Lexer.Span> innerArray = new List<Lexer.Span>();
                                for (int ii=0;ii<span.InnerSpans.Count;ii++)
                                {
                                    Lexer.Span span1 = span.InnerSpans[ii];
                                    //if (span1.Start <= index && index < span1.End)
                                    if (index >= span1.Start && index < span1.End)
                                    {
                                        innerArray.Add(span1);
                                    }
                                }
                                //Lexer.Span[] innerArray = spans2.ToArray();
                                //foreach (Lexer.Span inner in innerArray)
                                for (int ia = 0; ia < innerArray.Count; ia++)
                                {
                                    Lexer.Span inner = innerArray[ia];
                                    if (!found)
                                    {
                                        if (inner.Rule.RuleName.Equals(rule.RuleName))
                                        {
                                            found = true;
                                            //foundInParsed = true;
                                            index += inner.Length;
                                            CurrentIndex = index;
                                            //MaxParseIndex = Math.Max(index, MaxParseIndex);
                                            string groupName = rule.RuleName;
                                            if (groupName.IndexOf(":::") > -1)
                                            {
                                                int strIndex = groupName.IndexOf(":::");
                                                groupName = groupName.Substring(0, strIndex);
                                            }
                                            ParserResult result1 = new ParserResult
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
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

            }
            if (found)
            {
                if (ReportProgress != null)
                {
                    ReportProgress(this, new ReportProgressArgs() { InputPosition = index, InputSize = this.InputSize });
                }
            }
            if (hasSequences && !found)
            {
                bool anyTrue = false;
                string[] sequences = section.Sequences;
                //foreach (string item in sequences)
                for(int iii=0;iii<sequences.Length;iii++)
                {
                    string item = sequences[iii];
                    //ParserSequence seq = Sequences.FirstOrDefault(x => item.Equals(x.SequenceName, StringComparison.OrdinalIgnoreCase));
                    //ParserSequence seq = Sequences.FirstOrDefault(x => item.Equals(x.SequenceName));
                    ParserSequence seq = null;
                    if (SequencesDictionary.ContainsKey(item))
                    {
                        seq = SequencesDictionary[item];
                    }
                    //if (seq != null && seq2 == null)
                    //{
                    //    Console.WriteLine(seq);
                    //}

                    if (seq != null)
                    {
                        if (!found)
                        {
                            if (CancelAllParsing)
                            {
                                ParsedLog.Add((level, "SHUTTING DOWN BEFORE from CheckSequenceSection", sequence.SequenceName, section.ToString()));
                                return (-1, false, false, null, false, false);
                            }

                            var newParent = new ParserSequenceAndSection() { Sequence = seq, Section = null/*, Node = parent.Node*/ };
                            var result = CheckSequence(input, lexer, seq, index, level + 1, newParent, root, showOnConsole);
                            //if (level > MaxLevel)
                            //{
                            //    ParsedLog.Add((level, "SHUTTING DOWN AFTER from CheckSequenceSection", sequence.SequenceName, section.ToString()));
                            //    CancelAllParsing = true;
                            //    return (-1, false, false, null, false, false);
                            //}

                            if (result.Item2)
                            {
                                anyTrue = true;
                                index = result.Item1;
                                found = result.Item2;
                                if (found)
                                {
                                    string groupName = seq.SequenceName;
                                    if (groupName.IndexOf(":::") > -1)
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
                                    if (!string.IsNullOrEmpty(parserResult.VariableName))
                                    {
                                        parserResult.VariableValue = parserResult.InnerResultsText;
                                    }
                                    variables.Add(parserResult);
                                }
                            }
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
            if (hasDynamicRules && !found)
            {
                string[] rules = section.DynamicRules;
                for(int i=0;i<rules.Length;i++)
                {
                    string rule = rules[i];

                    var rule1 = InputLexer.Rules.Where(x => x.RuleName == rule && x.DynamicEvaluate).FirstOrDefault();
                    if (rule1 != null)
                    {
                        var result1 = (rule1 as Lexer.LexerRules.ILexerDynamicRule).DynamicEvaluation(index, input.OriginalInput);
                        if (result1.Success)
                        {
                            found = true;
                            index = result1.NewIndex;
                            Lexer.Span span = new Lexer.Span()
                            {
                                Start = result1.OriginalIndex,
                                InnerSpans = new List<Lexer.Span>(),
                                Rule = rule1,
                                Text = result1.Text,
                                Length = result1.Text.Length
                            };
                            string groupName = rule1.RuleName;
                            if (groupName.IndexOf(":::") > -1)
                            {
                                int strIndex = groupName.IndexOf(":::");
                                groupName = groupName.Substring(0, strIndex);
                            }
                            variables.Add(new ParserResult() {
                                Level = level,
                                VariableName = section.VariableName,
                                VariableValue = span.Text,
                                Span = span,
                                Section = section,
                                Sequence = sequence,
                                Name = rule1.RuleName,
                                GroupName = groupName,
                                Root = root,
                                Parent = parent
                            });
                            break;
                        }
                    }
                }
            }
            if (found && repeating)
            {
                foundOnce = true;
                //var newParent = new ParserSequenceAndSection() { Section = section, Sequence = sequence/*, Node = parent.Node*/ };
                goto repeat;
                //var result = CheckSequenceSection(input, lexer, sequence, section, index, foundOnce, variables, level, newParent, root, showOnConsole);
                //if (result.Item2)
                //{
                //    variables = result.Item4;
                //}
                //(index, found, foundOnce) = (result.Item1, result.Item2, result.Item3);
            }
            else if (found)
            {
                foundOnce = true;
            }
            return (index, found, foundOnce, variables, optional, repeating);
        }
        (int, bool, List<ParserResult>) CheckSequence(Lexer.LexerResult input, Lexer lexer, ParserSequence sequence, int index, int level, ParserSequenceAndSection parent, ParserSequenceAndSection root, bool showOnConsole = false)
        {
            //if (level > MaxLevel)
            //{
            //    CancelAllParsing = true;
            //    return (-1, false, null);
            //}
            //MaxParseLevel = Math.Max(level, MaxParseLevel);
            int idxResult = -1;
            bool found = false;
            List<ParserResult> foundItems = new List<ParserResult>();
            int countOptional = sequence.Sections.Where(x => x.IsOptional).Count();
            int countSections = sequence.Sections.Count;
            bool foundAllNonOptional = true;
            int sectionIndex = 0;
            //foreach (var section in sequence.Sections)
            for(int i= 0; i < sequence.Sections.Count; i++)
            {
                SequenceSection section = sequence.Sections[i];
                List<ParserResult> variables = new List<ParserResult>();

                //string idString = section.ToString();
                //ParsedSequenceSectionLog.Add((level, "Before", idString));
                //ParsedLog.Add((level, "Before", sequence.SequenceName, idString));

                if (CancelAllParsing)
                {
                    ParsedLog.Add((level, "SHUTTING DOWN BEFORE from CheckSequence", sequence.SequenceName, section.ToString()));
                    return (-1, false, null);
                }

                int idxPrior = index;
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
                    var priorTextSpan = input.OrganizedSpans.Where(x => x.Start <= idxPrior && x.End >= idxPrior).OrderByDescending(x => x.Length).FirstOrDefault();
                    var currentTextSpan = input.OrganizedSpans.Where(x => x.Start <= index && x.End >= index).OrderByDescending(x => x.Length).FirstOrDefault();
                    string textPrior = "";
                    string textCurrent = "";
                    if (priorTextSpan != null)
                    {
                        textPrior = priorTextSpan.Text;
                    }
                    if (currentTextSpan != null)
                    {
                        textCurrent = currentTextSpan.Text;
                    }
                    if (result.Item2) { Console.ForegroundColor = ConsoleColor.Green; }
                    else { Console.ForegroundColor = ConsoleColor.Red; }
                    string resultItem = "";
                    if (result.Item4 != null) { if (result.Item4.Count > 0) { resultItem = result.Item4[0].Name; } }
                    Console.WriteLine(level + " " + "".PadLeft(level * 3, ' ') + $"\"{textPrior}\", \"{textCurrent}\", {sectionIndex}:{sequence.SequenceName}, Section:{section}, Found:{result.Item2}, FoundOnce:{result.Item3}, IdxResult:{result.Item1}, ItemName:{resultItem}");
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
                //seq = Sequences.Where(x => x.SequenceName == sequenceName).ToList();
                ParserSequence seq1 = null;
                if (SequencesDictionary.ContainsKey(sequenceName))
                {
                    seq1 = SequencesDictionary[sequenceName];
                    seq.Add(seq1);
                }
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
                    if (showOnConsole) { Console.ForegroundColor = ConsoleColor.Gray; }
                    return (false, null, null);
                }

                var root = new ParserSequenceAndSection() { Sequence = sequence, Section = null };
                var parent = new ParserSequenceAndSection() { Sequence = sequence, Section = null };
                string groupName = sequence.SequenceName;
                if (groupName.IndexOf(":::") > -1)
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
                    if (showOnConsole) { Console.ForegroundColor = ConsoleColor.Gray; }
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
                    if (showOnConsole) { Console.ForegroundColor = ConsoleColor.Gray; }
                    return (found, currentSequence, items);
                }
            }
            if (showOnConsole) { Console.ForegroundColor = ConsoleColor.Gray; }
            return (found, currentSequence, items);
        }
        public (bool, ParserSequence, List<ParserResult>) CheckSearch(Lexer.LexerResult input, Lexer lexer, string sequenceName = "", bool showOnConsole = false)
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
                ParserSequence seq1 = null;
                if (SequencesDictionary.ContainsKey(sequenceName))
                {
                    seq1 = SequencesDictionary[sequenceName];
                    seq.Add(seq1);
                }
                //seq = Sequences.Where(x => x.SequenceName == sequenceName).ToList();
                //seq = SequencesDictionary
            }
            else
            {
                seq = Sequences.ToList();
            }
            //bool foundOnce = false;
            bool done = false;
            while(!done)
            {
                if (seq.Count == 0) { return (false, null, new List<ParserResult>()); }
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
                        if (showOnConsole) { Console.ForegroundColor = ConsoleColor.Gray; }
                        return (false, null, null);
                    }

                    var root = new ParserSequenceAndSection() { Sequence = sequence, Section = null };
                    var parent = new ParserSequenceAndSection() { Sequence = sequence, Section = null };
                    string groupName = sequence.SequenceName;
                    if (groupName.IndexOf(":::") > -1)
                    {
                        int strIndex = groupName.IndexOf(":::");
                        groupName = groupName.Substring(0, strIndex);
                    }
                    var parserResult = new ParserResult()
                    {
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

                    if (result.Item1 != -1)
                    {
                        index = result.Item1;
                    }

                    found = result.Item2;
                    //if (found) { foundOnce = true; }
                    if (found)
                    {
                        parserResult.InnerResults = result.Item3;
                        //root.Node = parserResult;
                        //parent.Node = parserResult;
                        items.Add(parserResult);
                        //return (found, currentSequence, items);
                        
                        //index++;
                        if (index >= input.OrganizedSpans.Max(x => x.End))
                        {
                            done = true;
                            if (showOnConsole) { Console.ForegroundColor = ConsoleColor.Gray; }
                            return (found, currentSequence, items);
                        }
                    }
                    else if (found == false)
                    {
                        index++;
                        if (index >= input.OrganizedSpans.Max(x => x.End))
                        {
                            done = true;
                            if (items.Count > 0) { found = true; }
                            if (showOnConsole) { Console.ForegroundColor = ConsoleColor.Gray; }
                            return (found, currentSequence, items);
                        }
                    }
                }
            }
            if (items.Count > 0) { found = true; }
            if (showOnConsole) { Console.ForegroundColor = ConsoleColor.Gray; }
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
        public Result Parse(string input, Lexer lexer = null, string sequenceName = "", bool showOnConsole = false, int maxSlidingWindow = -1)
        {
            MaxParseLevel = -1;
            MaxParseIndex = -1;
            InitParserLog();
            Lexer.LexerResult lexerResult = InputLexer.GetSpans(input, maxSlidingWindow: maxSlidingWindow);
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
        public Result Parse(List<Lexer.Span> input, Lexer lexer = null, string sequenceName = "", bool showOnConsole = false, int maxSlidingWindow = -1)
        {
            MaxParseLevel = -1;
            MaxParseIndex = -1;
            InitParserLog();
            //Lexer.LexerResult lexerResult = InputLexer.GetSpans(input, maxSlidingWindow: maxSlidingWindow);
            Lexer.LexerResult lexerResult = new Lexer.LexerResult()
            {
                OrganizedSpans = input
            };

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
        public Result Parse(Lexer.LexerResult input, Lexer lexer = null, string sequenceName = "", bool showOnConsole = false, int maxSlidingWindow = -1)
        {
            MaxParseLevel = -1;
            MaxParseIndex = -1;
            InitParserLog();
            //Lexer.LexerResult lexerResult = InputLexer.GetSpans(input, maxSlidingWindow: maxSlidingWindow);
            //Lexer.LexerResult lexerResult = new Lexer.LexerResult()
            //{
            //    OrganizedSpans = input
            //};

            (bool, ParserSequence, List<ParserResult>) result = (false, null, new List<ParserResult>());
            if (lexer != null && sequenceName != null)
            {
                result = this.Check(input, lexer, sequenceName, showOnConsole);
            }
            else if (lexer != null)
            {
                result = this.Check(input, lexer, showOnConsole: showOnConsole);
            }
            else if (sequenceName != null)
            {
                result = this.Check(input, InputLexer, sequenceName, showOnConsole);
            }
            else
            {
                result = this.Check(input, InputLexer);
            }
            var items = OrganizeParentNodes(result.Item3);
            return new Result() { Matched = result.Item1, LexerResult = input, Sequence = result.Item2, Results = items };
        }
        public Result Search(string input, Lexer lexer = null, string sequenceName = "", bool showOnConsole = false, int maxSlidingWindow = -1)
        {
            MaxParseLevel = -1;
            MaxParseIndex = -1;
            InitParserLog();
            Lexer.LexerResult lexerResult = InputLexer.GetSpans(input, maxSlidingWindow: maxSlidingWindow);
            (bool, ParserSequence, List<ParserResult>) result = (false, null, new List<ParserResult>());
            if (lexer != null && sequenceName != null)
            {
                result = this.CheckSearch(lexerResult, lexer, sequenceName, showOnConsole);
            }
            else if (lexer != null)
            {
                result = this.CheckSearch(lexerResult, lexer, showOnConsole: showOnConsole);
            }
            else if (!string.IsNullOrEmpty(sequenceName))
            {
                result = this.CheckSearch(lexerResult, InputLexer, sequenceName, showOnConsole);
            }
            else
            {
                result = this.CheckSearch(lexerResult, InputLexer);
            }
            var items = OrganizeParentNodes(result.Item3);
            return new Result() { Matched = result.Item1, LexerResult = lexerResult, Sequence = result.Item2, Results = items };
        }
        public SearchResult Search(string[] inputs, Lexer lexer = null, string sequenceName = "", bool showOnConsole = false, int maxSlidingWindow = -1)
        {
            MaxParseLevel = -1;
            MaxParseIndex = -1;
            InitParserLog();
            //bool foundAny = false;
            //(bool, ParserSequence, List<ParserResult>) result1 = (false, null, new List<ParserResult>());
            List<ParserResult> results = new List<ParserResult>();
            SearchResult searchResults = new SearchResult();
            List<Result> results1 = new List<Result>();

            foreach (var line in inputs)
            {
                Lexer.LexerResult lexerResult = InputLexer.GetSpans(line, maxSlidingWindow: maxSlidingWindow);
                (bool, ParserSequence, List<ParserResult>) result = (false, null, new List<ParserResult>());
                if (lexer != null && sequenceName != null)
                {
                    result = this.CheckSearch(lexerResult, lexer, sequenceName, showOnConsole);
                }
                else if (lexer != null)
                {
                    result = this.CheckSearch(lexerResult, lexer, showOnConsole: showOnConsole);
                }
                else if (!string.IsNullOrEmpty(sequenceName))
                {
                    result = this.CheckSearch(lexerResult, InputLexer, sequenceName, showOnConsole);
                }
                else
                {
                    result = this.CheckSearch(lexerResult, InputLexer);
                }
                var items = OrganizeParentNodes(result.Item3);
                if (result.Item1)
                {
                    results1.Add(new Result() { Matched = result.Item1, LexerResult = lexerResult, Sequence = result.Item2, Results = items });
                }
                //return new Result() { Matched = result.Item1, LexerResult = lexerResult, Sequence = result.Item2, Results = items };
            }
            if (results1.Count == 0)
            {
                results1.Add(new Result() { Matched = false, Results = new List<ParserResult>() });
            }
            searchResults.Result = results1;
            searchResults.CombinedResults = new List<(int, ParserResult)>();
            int lineIdx = 0;
            foreach(var item in results1)
            {
                foreach(var item1 in item.Results)
                {
                    searchResults.CombinedResults.Add((lineIdx, item1));
                }
                lineIdx++;
            }
            return searchResults;
        }
        public Result Search(Lexer.LexerResult input, Lexer lexer = null, string sequenceName = "", bool showOnConsole = false, int maxSlidingWindow = -1)
        {
            MaxParseLevel = -1;
            MaxParseIndex = -1;
            InitParserLog();
            //Lexer.LexerResult lexerResult = InputLexer.GetSpans(input, maxSlidingWindow: maxSlidingWindow);
            //Lexer.LexerResult lexerResult = new Lexer.LexerResult()
            //{
            //    OrganizedSpans = input
            //};

            (bool, ParserSequence, List<ParserResult>) result = (false, null, new List<ParserResult>());
            if (lexer != null && sequenceName != null)
            {
                result = this.CheckSearch(input, lexer, sequenceName, showOnConsole);
            }
            else if (lexer != null)
            {
                result = this.CheckSearch(input, lexer, showOnConsole: showOnConsole);
            }
            else if (sequenceName != null)
            {
                result = this.CheckSearch(input, InputLexer, sequenceName, showOnConsole);
            }
            else
            {
                result = this.CheckSearch(input, InputLexer);
            }
            var items = OrganizeParentNodes(result.Item3);
            return new Result() { Matched = result.Item1, LexerResult = input, Sequence = result.Item2, Results = items };
            //return new SearchResult() { Result = items }
        }
        bool IsSequence(string name)
        {
            return Sequences.Where(x => x.SequenceName == name).Count() > 0;
            //return SequencesDictionary.ContainsKey(name);
        }
        bool IsToken(string name)
        {
            return InputLexer.Rules.Where(x => x.RuleName == name && x.DynamicEvaluate == false).Count() > 0;
            //return InputLexer.RulesDictionary.ContainsKey(name);
        }
        bool IsDynamicLexerRule(string name)
        {
            return InputLexer.Rules.Where(x => x.RuleName == name && x.DynamicEvaluate == true).Count() > 0;
            //return InputLexer.RulesDictionary.ContainsKey(name);
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
                if (strSection.IndexOf("/Opt") > -1) { opt = true; }
                if (strSection.IndexOf("/Rep") > -1) { rep = true; }
                strSection = strSection.Replace("/Opt", "");
                strSection = strSection.Replace("/Rep", "");
                var choices = strSection.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray();
                List<string> tokens = new List<string>();
                List<string> sequences = new List<string>();
                List<string> dynamicRules = new List<string>();
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
                    else if (IsDynamicLexerRule(choice))
                    {
                        dynamicRules.Add(choice);
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
                string dynamicRulesList = string.Join(",", dynamicRules);
                string unknownsList = string.Join(",", unknowns);
                //Console.WriteLine($"Section - Tokens:{tokensList}, Sequences:{sequencesList}, Unknowns:{unknownsList}, Opt:{opt}, Rep:{rep}");
                seqSections.Add(new SequenceSection()
                {
                    Unknowns = unknownsList,
                    TokenList = tokensList,
                    SequenceList = sequencesList,
                    DynamicRulesList = dynamicRulesList,
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
                    bool caseInsensitive = false;
                    string newString = identifierText + ":::" + "ebnfTerminal" + ":" + item.Level + ":" + idx;
                    if (text.StartsWith("##\"") || text.StartsWith("##'")) { text = text.Substring(3); caseInsensitive = true; }
                    else if (text.StartsWith("\"") || text.StartsWith("'")) { text = text.Substring(1, text.Length - 1); }
                    if (text.EndsWith("\"") || text.EndsWith("'")) { text = text.Substring(0, text.Length - 1); }
                    //textStrings.Add((newString, false, false, text, true, true));
                    textStrings.Add((newString, false, false, "str"+newString, true));
                    if (caseInsensitive)
                    {
                        InputLexer.Rules.Add(new Lexer.LexerRules.CaseInsensitiveStringLexerRule("str" + newString, text));
                    }
                    else
                    {
                        InputLexer.Rules.Add(new Lexer.LexerRules.StringLexerRule("str" + newString, text));
                    }
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
        public (bool Success, int NewIndex, int OriginalIndex, string Text) ExecuteDynamicLexerRule(string input, int index, string ruleText)
        {
            var rule = Parse(ruleText, sequenceName: "dynamicLexerAction");
            (bool Success, int NewIndex, int OriginalIndex, string Text) result = (false, 0, 0, "");
            if (rule.Matched)
            {
                string ruleType = "";
                var ruleType_a = rule.Results[0].GetDescendantsOfType(new string[] { "dynamicLexerRegex", "dynamicLexerExclusive", "dynamicLexerInclusive" });
                foreach(var rt in ruleType_a)
                {
                    if (rt.Name == "dynamicLexerRegex") { ruleType = "Regex"; }
                    else if (rt.Name == "dynamicLexerExclusive") { ruleType = "Exclusive"; }
                    else if (rt.Name == "dynamicLexerInclusive") { ruleType = "Inclusive"; }
                }
                var res1 = rule.Results[0].GetDescendantsOfType(new string[] { "dynamicLexerAttribute" });
                List<(string Name, string Value)> attributes = new List<(string, string)>();
                foreach(var r1 in res1)
                {
                    string name = r1.GetDescendantsOfType(new[] { "identifier" })[0].InnerResultsText;
                    string value = r1.GetDescendantsOfType(new[] { "ebnfTerminal" })[0].GetDescendantsOfType(new[] { "ebnfCharacters" })[0].InnerResultsText;
                    attributes.Add((name, value));
                }
                Lexer.LexerRules.ILexerDynamicRule ruleNew = null;
                if (ruleType == "Regex") {
                    string options = RegexOptions.None.ToString();
                    if (attributes.Where(x => x.Name == "Options").Count() > 0)
                    {
                        options = attributes.Where(x => x.Name == "Options").First().Value;
                    }
                    ruleNew = new Lexer.LexerRules.RegexLexerRule(
                        attributes.Where(x => x.Name == "Name").First().Value, 
                        (RegexOptions)Enum.Parse(typeof(RegexOptions), options),
                        attributes.Where(x => x.Name == "Pattern").First().Value);
                    (ruleNew as Lexer.LexerRules.RegexLexerRule).Pattern = attributes.Where(x => x.Name == "Pattern").First().Value;
                }
                else if (ruleType == "Exclusive")
                {
                    ruleNew = new Lexer.LexerRules.SearchUntilStringExclusiveRule(
                        attributes.Where(x => x.Name == "Name").First().Value, 
                        input);
                    (ruleNew as Lexer.LexerRules.SearchUntilStringExclusiveRule).Token = attributes.Where(x => x.Name == "EndingString").First().Value;
                    (ruleNew as Lexer.LexerRules.SearchUntilStringExclusiveRule).ExcludeStrings = attributes.Where(x => x.Name == "ExcludeStrings").First().Value.Split(',').ToList();
                }
                else if (ruleType == "Inclusive")
                {
                    ruleNew = new Lexer.LexerRules.SearchUntilStringInclusiveRule(
                        attributes.Where(x => x.Name == "Name").First().Value,
                        input);
                    (ruleNew as Lexer.LexerRules.SearchUntilStringInclusiveRule).Token = attributes.Where(x => x.Name == "EndingString").First().Value;
                    (ruleNew as Lexer.LexerRules.SearchUntilStringInclusiveRule).ExcludeStrings = attributes.Where(x => x.Name == "ExcludeStrings").First().Value.Split(',').ToList();
                }
                if (ruleNew != null)
                {
                    result = ruleNew.DynamicEvaluation(index, input);
                }
            }
            return result;
        }
        public void AddDynamicLexerRule(string ruleText)
        {
            var rule = Parse(ruleText, sequenceName: "dynamicLexerAction");
            //(bool Success, int NewIndex, int OriginalIndex, string Text) result = (false, 0, 0, "");
            if (rule.Matched)
            {
                string ruleType = "";
                var ruleType_a = rule.Results[0].GetDescendantsOfType(new string[] { "dynamicLexerRegex", "dynamicLexerExclusive", "dynamicLexerInclusive" });
                foreach (var rt in ruleType_a)
                {
                    if (rt.Name == "dynamicLexerRegex") { ruleType = "Regex"; }
                    else if (rt.Name == "dynamicLexerExclusive") { ruleType = "Exclusive"; }
                    else if (rt.Name == "dynamicLexerInclusive") { ruleType = "Inclusive"; }
                }
                var res1 = rule.Results[0].GetDescendantsOfType(new string[] { "dynamicLexerAttribute" });
                List<(string Name, string Value)> attributes = new List<(string, string)>();
                foreach (var r1 in res1)
                {
                    string name = r1.GetDescendantsOfType(new[] { "identifier" })[0].InnerResultsText;
                    string value = r1.GetDescendantsOfType(new[] { "ebnfTerminal" })[0].GetDescendantsOfType(new[] { "ebnfCharacters" })[0].InnerResultsText;
                    attributes.Add((name, value));
                }
                Lexer.LexerRules.ILexerDynamicRule ruleNew = null;
                if (ruleType == "Regex")
                {
                    string options = RegexOptions.None.ToString();
                    if (attributes.Where(x => x.Name == "Options").Count() > 0)
                    {
                        options = attributes.Where(x => x.Name == "Options").First().Value;
                    }
                    ruleNew = new Lexer.LexerRules.RegexLexerRule(
                        attributes.Where(x => x.Name == "Name").First().Value,
                        (RegexOptions)Enum.Parse(typeof(RegexOptions), options),
                        attributes.Where(x => x.Name == "Pattern").First().Value);
                    (ruleNew as Lexer.LexerRules.RegexLexerRule).Pattern = attributes.Where(x => x.Name == "Pattern").First().Value;
                }
                else if (ruleType == "Exclusive")
                {
                    ruleNew = new Lexer.LexerRules.SearchUntilStringExclusiveRule(
                        attributes.Where(x => x.Name == "Name").First().Value,
                        attributes.Where(x => x.Name == "EndingString").First().Value);
                    (ruleNew as Lexer.LexerRules.SearchUntilStringExclusiveRule).Token = attributes.Where(x => x.Name == "EndingString").First().Value;
                    (ruleNew as Lexer.LexerRules.SearchUntilStringExclusiveRule).ExcludeStrings = attributes.Where(x => x.Name == "ExcludeStrings").First().Value.Split(',').ToList();
                }
                else if (ruleType == "Inclusive")
                {
                    ruleNew = new Lexer.LexerRules.SearchUntilStringInclusiveRule(
                        attributes.Where(x => x.Name == "Name").First().Value,
                        attributes.Where(x => x.Name == "EndingString").First().Value);
                    (ruleNew as Lexer.LexerRules.SearchUntilStringInclusiveRule).Token = attributes.Where(x => x.Name == "EndingString").First().Value;
                    (ruleNew as Lexer.LexerRules.SearchUntilStringInclusiveRule).ExcludeStrings = attributes.Where(x => x.Name == "ExcludeStrings").First().Value.Split(',').ToList();
                }
                if (ruleNew != null)
                {
                    //result = ruleNew.DynamicEvaluation(index, input);
                    RemoveEBNFRule((ruleNew as Lexer.LexerRules.ILexerRule).RuleName);
                    InputLexer.Rules.Add(ruleNew as Lexer.LexerRules.ILexerRule);
                    InputLexer.SyncRuleDictionary();
                }
            }
            //return result;
        }
        public void AddEBNFGrammar(string input)
        {
            var grammar = Parse(input, sequenceName: "grammar");
            if (grammar.Matched)
            {
                foreach (var rule in grammar.Results[0].InnerResults)
                {
                    string ruleText = rule.InnerResultsText.Trim();
                    AddEBNFRule(ruleText);
                }
            }
            InputLexer.SyncRuleDictionary();
            SyncSequencesDictionary();
        }
        public void AddEBNFGrammar(string[] input, bool checkFirst = false)
        {
            if (checkFirst)
            {
                foreach (var item in input)
                {
                    var rule = Parse(item, sequenceName: "rule");
                    if (rule.Matched)
                    {
                        string ruleText = rule.Results[0].InnerResultsText.Trim();
                        AddEBNFRule(ruleText);
                    }
                }
            }
            else
            {
                foreach (var item in input)
                {
                    AddEBNFRule(item.Trim());
                }
            }
            InputLexer.SyncRuleDictionary();
            SyncSequencesDictionary();
        }
        public void SyncSequencesDictionary()
        {
            SequencesDictionary = new Dictionary<string, ParserSequence>();
            foreach(var seq in Sequences)
            {
                SequencesDictionary[seq.SequenceName] = seq;
            }
        }
    }
}