using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LexerParser1
{
    public class Lexer
    {
        public class LexerRules
        {
            public interface ILexerRule
            {
                int Ordinal { get; set; }
                string RuleName { get; set; }
                string RuleType { get; set; }
                bool Evaluate(string input);
            }
            public interface ILexerLookup
            {
                void LookupRule(List<LexerRules.ILexerRule> rules);
            }
            public class CharLexerRule : ILexerRule
            {
                public int Ordinal { get; set; } = 1;
                public string RuleType { get; set; } = "Char";
                public string RuleName { get; set; }
                public char Token { get; set; }

                public CharLexerRule(string ruleName, char input)
                {
                    RuleName = ruleName;
                    Token = input;
                }

                public bool Evaluate(string input)
                {
                    if (input.Length == 0) return false;
                    if (input.Length > 1) return false;
                    return (input == Token.ToString());
                }

                public override string ToString()
                {
                    return $"[{RuleName}, Char:{Token}]";
                }
            }
            public class CharInLexerRule : ILexerRule
            {
                public int Ordinal { get; set; } = 2;
                public string RuleName { get; set; }
                public string RuleType { get; set; } = "CharIn";
                public char[] Tokens { get; set; }
                public bool Evaluate(string input)
                {
                    if (input.Length == 0) return false;
                    if (input.Length > 1) return false;
                    return Tokens.Contains(input[0]);
                }
                public CharInLexerRule(string ruleName, string input)
                {
                    RuleName = ruleName;
                    Tokens = input.ToCharArray();
                }
                public override string ToString()
                {
                    return $"[{RuleName}, CharIn:{string.Join(", ", Tokens)}]";
                }
            }
            public class CaseInsensitiveStringLexerRule : ILexerRule
            {
                public int Ordinal { get; set; } = 6;
                public string RuleName { get; set; }
                public string RuleType { get; set; } = "CaseInsensitiveString";
                public string Token { get; set; }
                public bool Evaluate(string input)
                {
                    return input.ToLower() == Token.ToLower();
                }
                public CaseInsensitiveStringLexerRule(string ruleName, string input)
                {
                    RuleName = ruleName;
                    Token = input;
                }
                public override string ToString()
                {
                    return $"[{RuleName}, CaseInsensitive String:{Token}]";
                }
            }
            public class StringLexerRule : ILexerRule
            {
                public int Ordinal { get; set; } = 7;
                public string RuleName { get; set; }
                public string RuleType { get; set; } = "String";
                public string Token { get; set; }
                public bool Evaluate(string input)
                {
                    return input == Token;
                }
                public StringLexerRule(string ruleName, string input)
                {
                    RuleName = ruleName;
                    Token = input;
                }
                public override string ToString()
                {
                    return $"[{RuleName}, String:{Token}]";
                }
            }
            public class RuleLookupLexerRule : ILexerRule, ILexerLookup
            {
                public int Ordinal { get; set; } = 3;
                public string RuleName { get; set; }
                public string RuleLookupName { get; set; }
                public string RuleType { get; set; } = "RuleLookup";
                public ILexerRule LookedUpRule { get; set; }

                public RuleLookupLexerRule(string ruleName, string ruleLookupName)
                {
                    RuleName = ruleName;
                    RuleLookupName = ruleLookupName;
                }
                public void LookupRule(List<LexerRules.ILexerRule> rules)
                {
                    foreach (var r in rules)
                    {
                        if (r.RuleName == RuleLookupName)
                        {
                            LookedUpRule = r;
                        }
                    }
                }
                public bool Evaluate(string input)
                {
                    if (LookedUpRule != null)
                    {
                        return LookedUpRule.Evaluate(input);
                    }
                    return false;
                }
            }
            public class RuleCollectionRule : ILexerRule
            {
                public int Ordinal { get; set; } = 4;
                public RuleCollectionRule(string key, ILexerRule[] rules)
                {
                    RuleName = key;
                    Rules = rules;
                }

                public string RuleName { get; set; }
                public string RuleType { get; set; } = "RuleCollection";
                public ILexerRule[] Rules { get; set; }

                public bool Evaluate(string input)
                {
                    foreach (var rule in Rules)
                    {
                        if (rule.Evaluate(input)) return true;
                    }
                    return false;
                }
            }
            public class RepeatRuleLexerRule : ILexerRule, ILexerLookup
            {
                public int Ordinal { get; set; } = 5;
                public string RuleName { get; set; }
                public string RepeatRuleName { get; set; }
                public string RuleType { get; set; } = "RepeatRule";
                public LexerRules.ILexerRule Rule { get; set; }
                public void LookupRule(List<LexerRules.ILexerRule> rules)
                {
                    foreach (var r in rules)
                    {
                        if (r.RuleName == RepeatRuleName)
                        {
                            Rule = r;
                        }
                    }
                }
                public bool Evaluate(string input)
                {
                    foreach (var c in input)
                    {
                        if (!Rule.Evaluate(c.ToString())) { return false; }
                    }
                    return true;
                }
                public RepeatRuleLexerRule(string ruleName, string repeatRuleName)
                {
                    RuleName = ruleName;
                    RepeatRuleName = repeatRuleName;
                }
                public override string ToString()
                {
                    return $"[{RuleName}, Repeat:{RepeatRuleName}]";
                }
            }
        }
        public class Span
        {
            public int Start { get; set; }
            public int End { get { return Start + Length; } }
            public int Length { get { return Text.Length; } }
            public string Text { get; set; }
            public LexerRules.ILexerRule Rule { get; set; }
            public List<Span> InnerSpans { get; set; }

            public override string ToString()
            {
                return $"[{Start}:{Text}, {Rule.RuleName}/{Rule.RuleType}]";
            }
        }
        public class LexerResult
        {
            public List<Span> RawSpans { get; set; }
            public List<Span> OrganizedSpans { get; set; }
            public List<Span> SingularSpans { get; set; }
        }
        public List<LexerRules.ILexerRule> Rules { get; set; }
        LexerRules.ILexerRule GetRule(string key, JToken token)
        {
            JToken? value = token;
            if (value.Type == JTokenType.String)
            {
                string val = value.ToString();
                if (val.StartsWith("char:"))
                {
                    val = val.Substring("char:".Length);
                    return new LexerRules.CharLexerRule(key, val[0]);
                }
                else if (val.StartsWith("char_in:"))
                {
                    val = val.Substring("char_in:".Length);
                    return new LexerRules.CharInLexerRule(key, val);
                }
                else if (val.StartsWith("ci_string:"))
                {
                    val = val.Substring("ci_string:".Length);
                    return new LexerRules.CaseInsensitiveStringLexerRule(key, val);
                }
                else if (val.StartsWith("string:"))
                {
                    val = val.Substring("string:".Length);
                    return new LexerRules.StringLexerRule(key, val);
                }
                else if (val.StartsWith("rule:"))
                {
                    val = val.Substring("rule:".Length);
                    return new LexerRules.RuleLookupLexerRule(key, val);
                }
                else if (val.StartsWith("repeat:"))
                {
                    val = val.Substring("repeat:".Length);
                    return new LexerRules.RepeatRuleLexerRule(key, val);
                }
                else
                {

                }
            }
            return null;
        }
        List<LexerRules.ILexerRule> GetRules(JObject lexer)
        {
            List<LexerRules.ILexerRule> rules = new List<LexerRules.ILexerRule>();
            foreach (var l in lexer)
            {
                string key = l.Key;
                JToken? value = l.Value;
                if (value.Type == JTokenType.String)
                {
                    rules.Add(GetRule(key, value));
                }
                else if (value.Type == JTokenType.Array)
                {
                    List<LexerRules.ILexerRule> r1 = new List<LexerRules.ILexerRule>();
                    foreach(var v1 in value)
                    {
                        r1.Add(GetRule(key, v1));
                    }
                    rules.Add(new LexerRules.RuleCollectionRule(key, r1.ToArray()));
                }
            }
            return rules;
        }
        void Init(JObject lexer, JObject userLexer)
        {
            Rules = GetRules(lexer);
            if (userLexer != null)
            {
                Rules.AddRange(GetRules(userLexer));
            }
            for (int i = 0; i < Rules.Count; i++)
            {
                if (Rules[i].RuleType == "RuleLookup")
                {
                    (Rules[i] as LexerRules.ILexerLookup).LookupRule(Rules);
                }
                if (Rules[i].RuleType == "RuleCollection")
                {
                    for (int i2 = 0; i2 < (Rules[i] as LexerRules.RuleCollectionRule).Rules.Length; i2++)
                    {
                        ((Rules[i] as LexerRules.RuleCollectionRule).Rules[i2] as LexerRules.ILexerLookup).LookupRule(Rules);
                    }
                }
                if (Rules[i].RuleType == "RepeatRule")
                {
                    (Rules[i] as LexerRules.ILexerLookup).LookupRule(Rules);
                }
            }
        }
        public Lexer(string defaultConfigurationFile, string userConfigurationFile)
        {
            string defaultConfig = File.ReadAllText(defaultConfigurationFile);
            dynamic defaultObj = JsonConvert.DeserializeObject<dynamic>(defaultConfig);
            JObject lexerRules = defaultObj.lexer as JObject;

            string userConfig = File.ReadAllText(userConfigurationFile);
            dynamic userObj = JsonConvert.DeserializeObject<dynamic>(userConfig);
            JObject lexerUserRules = userObj.userLexer as JObject;

            Init(lexerRules, lexerUserRules);
        }
        public Lexer(JObject lexer, JObject userLexer)
        {
            Init(lexer, userLexer);
        }
        List<LexerRules.ILexerRule> ProcessRules(string input)
        {
            List<LexerRules.ILexerRule> rules = new List<LexerRules.ILexerRule>();
            foreach (var r in Rules)
            {
                if (r.Evaluate(input))
                {
                    //Console.WriteLine($"Rule Passed:{r.RuleType}/{r.RuleName}, {input}");
                    rules.Add(r);
                }
            }
            return rules;
        }
        public LexerResult GetSpans(string input)
        {
            var spans = ProcessText(input);
            return new LexerResult()
            {
                RawSpans = spans.Item1,
                OrganizedSpans = OrganizeSpans(spans.Item1),
                SingularSpans = spans.Item2
            };
        }
        public (List<Span>, List<Span>) ProcessText(string input)
        {
            List<Span> spans = new List<Span>();
            int len1 = 1;
            for(int i1=0; i1<input.Length; i1++)
            {
                for(int i2=0; i2<input.Length; i2++)
                {
                    if ((i2 + len1) > input.Length)
                    {
                        break;
                    }
                    string substr = input.Substring(i2, len1);
                    //Console.WriteLine(substr);
                    var passedRules = ProcessRules(substr);
                    foreach(var p in passedRules)
                    {
                        spans.Add(new Span() { Rule = p, Start = i2, Text = substr });
                    }
                }
                len1++;
            }
            spans = spans.OrderByDescending(x => x.Length).OrderBy(x => x.Start).ThenByDescending(x => x.Rule.Ordinal).ToList();
            List<Span> singularSpans = spans.Where(x => x.Length == 1).OrderBy(x => x.Start).ThenByDescending(x => x.Rule.Ordinal).ToList();
            //spans = spans.Where(x => x.Length > 1).ToList();

            List<(int, Span)> spans2 = new List<(int, Span)>();

            for (int i=0;i<spans.Count;i++)
            {
                for(int i2=i+1;i2<spans.Count;i2++)
                {
                    if (spans[i2].Rule.RuleName == spans[i].Rule.RuleName &&
                        spans[i2].Start >= spans[i].Start &&
                        (spans[i2].Start + spans[i2].Length) <= (spans[i].Start + spans[i].Length)
                        )
                    {
                        if (spans2.Count(x => x.Item1 == i2) == 0)
                        {
                            spans2.Add((i2, spans[i2]));
                        }
                    }
                }
            }
            foreach (var span in spans2.OrderByDescending(x => x.Item1))
            {
                spans.RemoveAt(span.Item1);
            }
            return (spans, singularSpans);
        }
        public bool EvaluateRule(string input, string ruleName)
        {
            var rule = Rules.Where(x => x.RuleName == ruleName).FirstOrDefault();
            if (rule != null)
            {
                return rule.Evaluate(input);
            }
            else { return false; }
        }
        public List<Span> OrganizeSpans(List<Span> spans)
        {
            spans = spans.OrderByDescending(x => x.Length).ThenBy(x => x.Start).ThenByDescending(x => x.Rule.Ordinal).ToList();
            bool done = false;
            int idx = 0;
            List<Span> results = new List<Span>();
            while(!done)
            {
                Span span1 = spans[idx];
                int idx2 = idx + 1;
                span1.InnerSpans = new List<Span>();
                List<int> removeIndexes = new List<int>();
                for(int i=idx2;i<spans.Count;i++)
                {
                    if (spans[i].Start >= span1.Start && 
                        spans[i].End <= span1.End)
                    {
                        span1.InnerSpans.Add(spans[i]);
                        removeIndexes.Add(i);
                    }
                }
                results.Add(span1);
                foreach (var i in removeIndexes.OrderByDescending(x => x).ToArray())
                {
                    spans.RemoveAt(i);
                }
                idx++;
                if (idx >= spans.Count) { done = true; }
            }
            spans = spans.OrderBy(x => x.Start).ToList();
            foreach(var span in spans)
            {
                if (span.InnerSpans != null)
                {
                    span.InnerSpans = span.InnerSpans.OrderBy(x => x.Start).ToList();
                }
            }
            return spans;
        }
    }
    public class Parser
    {
        public class ParserSequence
        {
            public string SequenceName { get; set; }
            public List<Parser.SequenceSection> Sections { get; set; }

            public override string ToString()
            {
                return $"[ParserSequence: {SequenceName}, Sections: {(Sections != null ? Sections.Count().ToString() : "(Null)")}]";
            }

            public class EvaluateSequenceSectionResult
            {
                public string MatchedRule { get; set; }
                public string Input { get; set; }
                public string OriginalInput { get; set; }
                public int StartIndex { get; set; }
                public int Length { get; set; }
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
        }
        public class SequenceSection
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
            public string[] Tokens { get { return !string.IsNullOrEmpty(TokenList) ? TokenList.Split(',').Select(x => x.Trim()).ToArray() : null;  } }
            public string[] Sequences { get { return !string.IsNullOrEmpty(SequenceList) ? SequenceList.Split(',').Select(x => x.Trim()).ToArray() : null; } }
            public string[] EBNFChoices { get { return !string.IsNullOrEmpty(EBNFItemList) ? EBNFItemList.Split('|').Select(x => x.Trim()).ToArray() : null; } }
            public string[] EBNFConcatenations { get { return !string.IsNullOrEmpty(EBNFItemList) ? EBNFItemList.Split(',').Select(x => x.Trim()).ToArray() : null; } }
            public string SyntaxColor { get; set; }
            public override string ToString()
            {
                string tokenStr = "";
                string seqStr = "";
                if (Tokens != null) { 
                    tokenStr = string.Join(", ", Tokens); 
                }
                if (Sequences != null)
                {
                    seqStr = string.Join(", ", Sequences);
                }
                return $"[Opt:{IsOptional}, Rep:{IsRepeating}, Tokens:{tokenStr}, Seq:{seqStr}]";
            }
        }
        public class ParserSequenceAndSection
        {
            public ParserSequence Sequence { get; set; }
            public SequenceSection Section { get; set; }
            public ParserResult Node { get; set; }
            public string NodeText { get; set; }
        }
        public class ParserResult
        {
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
            public string InnerResultsText
            {
                get
                {
                    return GetInnerStringRecursive(InnerResults);
                }
            }
            public override string ToString()
            {
                string variableStr = "";
                string innerStr = "";
                string spanStr = "";
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
                string varName = (!string.IsNullOrEmpty(VariableName) ? VariableName : "");
                return $"[Lvl:{Level}, Name:{Name}, Seq:{((Sequence != null) ? Sequence.SequenceName : "null")}, Var:{varName}:{variableStr}, Span:{spanStr}, Inner:{innerStr}]";
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
            public List<(string identifierName, ParserResult result, List<(string idName, bool contains, bool containedBy, bool isSameNode)> containerInfo)> GetEBNFGroupsWithContainmentMap(string identifierName)
            {
                var res1 = GetEBNFGroups(identifierName, false);
                List<(string identifierName, ParserResult result, List<(string idName, bool contains, bool containedBy, bool isSameNode)>)> results = new List<(string, ParserResult, List<(string, bool, bool, bool)>)>();
                for(int i=0;i<res1.Count;i++)
                {
                    var r1 = res1[i];
                    List<(string idName, bool contains, bool containedBy, bool isSameNode)> containmentMap = new List<(string, bool, bool, bool)>();
                    for(int i2=0;i2<res1.Count;i2++)
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
            dynamic parserObj = JsonConvert.DeserializeObject<dynamic>(config);
            JObject parserSequences = parserObj.parser as JObject;

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
            foreach(var sequence in Sequences)
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

            bool found = false;
            if (tokens != null)
            {
                foreach (var item in tokens)
                {
                    var rule = lexer.Rules.Where(x => x.RuleName.ToLower() == item.ToLower()).FirstOrDefault();
                    if (rule != null)
                    {
                        foreach (var span in spans)
                        {
                            if (!found)
                            {
                                if (span.Rule.RuleName == rule.RuleName) {
                                    found = true; 
                                    index += span.Length;
                                    var result1 = new ParserResult
                                    {
                                        Level = level,
                                        VariableName = section.VariableName,
                                        VariableValue = span.Text,
                                        Span = span,
                                        Section = section,
                                        Sequence = sequence,
                                        Name = rule.RuleName,
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
                                        if (inner.Rule.RuleName == rule.RuleName) {
                                            found = true; 
                                            index += inner.Length;
                                            var result1 = new ParserResult
                                            {
                                                Level = level + 1,
                                                VariableName = section.VariableName,
                                                VariableValue = inner.Text,
                                                Span = inner,
                                                Section = section,
                                                Sequence = sequence,
                                                Name = rule.RuleName,
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

                            if (result.Item2) {
                                anyTrue = true;
                                index = result.Item1; 
                                found = result.Item2; 
                                if (found)
                                {
                                    var parserResult = new ParserResult()
                                    {
                                        Level = level + 1,
                                        Name = seq.SequenceName,
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
            foreach(var section in sequence.Sections)
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

                if (found == false && optional) { 
                    index = idxResult; 
                }
                else if (found == false && optional == false && (countOptional < countSections)) {
                    foundItems = new List<ParserResult>();
                    return (-1, false, foundItems); 
                }
                else { 
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
                var parserResult = new ParserResult() { Name = sequence.SequenceName, Level = level, Parent = parent, Root = root };
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
        List<ParserResult> OrganizeParentNodes(List<ParserResult> nodes, int level = 0, ParserResult parent = null)
        {
            foreach(var node in nodes)
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
            var sections = inputRule.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray();
            //Console.WriteLine($"Adding rule:{identifier}");
            foreach(var section in sections)
            {
                bool opt = false;
                bool rep = false;
                string strSection = section;
                if (strSection.Contains("/Opt")) { opt = true; }
                if (strSection.Contains("/Rep")) { rep = true; }
                strSection = strSection.Replace("/Opt", "");
                strSection = strSection.Replace("/Rep", "");
                var choices = strSection.Split('|', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray();
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
                    Unknowns = unknownsList, TokenList = tokensList, SequenceList = sequencesList, IsOptional = opt, IsRepeating = rep
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

        }
        public void AddEBNFRule(string inputEBNF)
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
                for(int i=0;i<firstDescendants.Count;i++)
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
                while(!doneTokens)
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
                        for (int a=0;a<finishedList.Count;a++)
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
                        var original = finishedList.Where(x => x.node.InnerResultsText == item.Item2.InnerResultsText && x.finished == false).FirstOrDefault();
                        var originalItem = original.node;

                        var resOuter = item.result;
                        int minOuter = resOuter.MinStart();
                        int maxOuter = resOuter.MaxEnd();
                        string textOuter = resOuter.InnerResultsText;
                        int textOuterLen = textOuter.Length;
                        
                        var resInner = resOuter.InnerResults.Where(x => x.Name.Contains("whitespace") == false).ToArray();
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

    class Program
    {
        static void Main(string[] args)
        {
            string defaultConfiguration = $"Configuration/Default.json";
            string userConfiguration = $"Configuration/ParserEBNF.json";
            Lexer lexer = new Lexer(defaultConfiguration, userConfiguration);
            Parser parser = new Parser(lexer, userConfiguration);

            //string inputSql = "select column1, column2 as c2 from _yay";
            //string inputCss = "body { background-color: #512fff; margin-left: auto; color: '#123456'; } h2 { color: 'blue'; }";
            //string inputJs = "var i = \"Hello\"; var x=5;";
            //string inputEBNF = "a=\"5\";";
            //string inputEBNF2 = "PROGRAM BEGIN a:=5; b1:='Hurrooo'; b2:=\"Yay\"; b2 := a; END;";

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
        }
    }
}