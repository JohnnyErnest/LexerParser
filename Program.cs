using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LexerParser1
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
                foreach(var r in rules)
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
                foreach(var c in input)
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

    public class Lexer
    {
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

        public Lexer(JObject lexer, JObject userLexer)
        {
            Rules = GetRules(lexer);
            if (userLexer != null)
            {
                Rules.AddRange(GetRules(userLexer));
            }
            for (int i=0; i<Rules.Count; i++)
            {
                if (Rules[i].RuleType == "RuleLookup")
                {
                    (Rules[i] as LexerRules.ILexerLookup).LookupRule(Rules);
                }
                if (Rules[i].RuleType == "RuleCollection")
                {
                    for (int i2=0; i2<(Rules[i] as LexerRules.RuleCollectionRule).Rules.Length; i2++)
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

        public class LexerResult
        {
            public List<Span> RawSpans { get; set; }
            public List<Span> OrganizedSpans { get; set; }
            public List<Span> SingularSpans { get; set; }
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
                        var contains = spans2.Count(x => x.Item1 == i2);
                        if (contains == 0)
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
            for(int i=1; i <= input.Length; i++)
            {
                string str = input.Substring(0, i);
                string[] rules = section.Tokens;
                bool[] ruleEval = new bool[rules.Length];
                bool matchAny = false;
                string matchedRule = "";
                for(int i1=0;i1<ruleEval.Length;i1++)
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
                        Input = str, OriginalInput = input, StartIndex = 0, Length = i, MatchedRule = matchedRule
                    };
                }
            }
            return null;
        }

        public bool Evaluate(string input, Lexer lexer)
        {
            int index = 0;
            foreach(var section in Sections)
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

    public class Parser
    {
        public class SequenceSection
        {
            public bool IsOptional { get; set; }
            public bool IsRepeating { get; set; }
            public string TokenList { get; set; }
            public string SequenceList { get; set; }
            [JsonProperty("varName")]
            public string VariableName { get; set; }
            public string[] Tokens { get { return !string.IsNullOrEmpty(TokenList) ? TokenList.Split(',') : null;  } }
            public string[] Sequences { get { return !string.IsNullOrEmpty(SequenceList) ? SequenceList.Split(',') : null; } }
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

        public List<ParserSequence> Sequences { get; set; }

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
                }
            }
        }

        public Parser(JObject parser)
        {
            InitParser(parser);
        }

        public bool Evaluate(string input, Lexer lexer)
        {
            foreach(var sequence in Sequences)
            {
                if (sequence.Evaluate(input, lexer)) return true;
            }
            return false;
        }

        public class ParserResult
        {
            public string Name { get; set; }
            public Lexer.Span Span { get; set; }
            public string VariableName { get; set; }
            public string VariableValue { get; set; }
            public ParserSequence Sequence { get; set; }
            public SequenceSection Section { get; set; }
            public int Level { get; set; }
            public List<ParserResult> InnerResults { get; set; } = new List<ParserResult>();

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

            public string InnerResultsText { 
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
        }

        (int, bool, bool, List<ParserResult>, bool, bool) CheckSequenceSection(Lexer.LexerResult input, Lexer lexer, ParserSequence sequence, SequenceSection section, int index, bool foundOnce, List<ParserResult> variables, int level)
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
                                    variables.Add(new ParserResult { 
                                        Level = level, 
                                        VariableName = section.VariableName, 
                                        VariableValue = span.Text,
                                        Span = span,
                                        Section = section,
                                        Sequence = sequence,
                                        Name = rule.RuleName
                                    }); 
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
                                            variables.Add(new ParserResult { 
                                                Level = level + 1, 
                                                VariableName = section.VariableName, 
                                                VariableValue = inner.Text,
                                                Span = inner,
                                                Section = section,
                                                Sequence = sequence,
                                                Name = rule.RuleName
                                            }); 
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
                foreach (var item in sequences)
                {
                    var seq = Sequences.Where(x => x.SequenceName.ToLower() == item.ToLower()).FirstOrDefault();
                    if (seq != null)
                    {
                        if (!found)
                        {
                            //var spanText = (spans != null ? (spans.Count() > 0 ? spans.First().Text : "") : "");
                            //Console.WriteLine("".PadLeft(level * 3, ' ') + $"[{item}, {spanText}], ");
                            if (CancelAllParsing)
                            {
                                return (-1, false, false, null, false, false);
                            }

                            var result = CheckSequence(input, lexer, seq, index, level + 1);
                            if (level > MaxLevel)
                            {
                                CancelAllParsing = true;
                                return (-1, false, false, null, false, false);
                            }

                            //Console.WriteLine($"NewIndex:{result.Item1}, Found:{result.Item2}");
                            //Console.WriteLine($"Optional:{optional}, Repeating:{repeating}");

                            if (result.Item2) { 
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
                                        VariableName = section.VariableName                                        
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
            }
            if (found && repeating)
            {
                foundOnce = true;
                var result = CheckSequenceSection(input, lexer, sequence, section, index, foundOnce, variables, level);
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

        (int, bool, List<ParserResult>) CheckSequence(Lexer.LexerResult input, Lexer lexer, ParserSequence sequence, int index, int level)
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

                if (CancelAllParsing)
                {
                    return (-1, false, null);
                }

                var result = CheckSequenceSection(input, lexer, sequence, section, index, false, variables, level + 1);
                if (level > MaxLevel)
                {
                    CancelAllParsing = true;
                    return (-1, false, null);
                }

                Console.WriteLine(level + " " + "".PadLeft(level * 3, ' ') + $"{sectionIndex}:{sequence.SequenceName}, Section:{section}, Found:{result.Item2}, FoundOnce:{result.Item3}, IdxResult:{result.Item1}");

                idxResult = result.Item1;
                found = result.Item2;
                bool foundOnce = result.Item3;
                bool optional = section.IsOptional;
                bool repeating = section.IsRepeating;

                if (!found && !optional)
                {
                    foundAllNonOptional = false;
                }

                if (!found && foundOnce && repeating)
                {
                    found = true;
                }

                if (!found && optional) { 
                    index = idxResult; 
                }
                else if (!found && !optional && (countOptional < countSections)) {
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

        public (bool, ParserSequence, List<ParserResult>) Check(Lexer.LexerResult input, Lexer lexer)
        {
            int index = 0;
            int endIndex = input.OrganizedSpans.OrderByDescending(x => x.End).FirstOrDefault().End;
            bool found = false;
            ParserSequence currentSequence = null;
            List<ParserResult> items = new List<ParserResult>();
            int level = 0;
            foreach(var sequence in Sequences)
            {
                Console.WriteLine();
                Console.WriteLine($"Sequence:{sequence.SequenceName}");
                Console.WriteLine("++++++++++++++++++++++++++++++++++++++++++++");

                currentSequence = sequence;

                if (CancelAllParsing)
                {
                    return (false, null, null);
                }

                var result = CheckSequence(input, lexer, sequence, index, level + 1);

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
                else if (found) {
                    items.Add(new ParserResult() { Name = sequence.SequenceName, InnerResults = result.Item3, Level = level });
                    return (found, currentSequence, items);
                }
            }
            return (found, currentSequence, items);
        }

        public (bool, ParserSequence, List<ParserResult>) Check(Lexer.LexerResult input, Lexer lexer, string sequenceName)
        {
            int index = 0;
            int endIndex = input.OrganizedSpans.OrderByDescending(x => x.End).FirstOrDefault().End;
            bool found = false;
            ParserSequence currentSequence = null;
            List<ParserResult> items = new List<ParserResult>();
            int level = 0;
            var seq = Sequences.Where(x => x.SequenceName == sequenceName);
            foreach (var sequence in seq)
            {
                Console.WriteLine();
                Console.WriteLine($"Sequence:{sequence.SequenceName}");
                Console.WriteLine("++++++++++++++++++++++++++++++++++++++++++++");

                currentSequence = sequence;

                if (CancelAllParsing)
                {
                    return (false, null, null);
                }

                var result = CheckSequence(input, lexer, sequence, index, level + 1);

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
                    items.Add(new ParserResult() { Name = sequence.SequenceName, InnerResults = result.Item3, Level = level });
                    return (found, currentSequence, items);
                }
            }
            return (found, currentSequence, items);
        }

        public Lexer InputLexer { get; set; }
        public Parser(Lexer lexer, JObject parser)
        {
            InputLexer = lexer;
            InitParser(parser);
        }

        public int MaxLevel { get; set; } = 100;
        public bool CancelAllParsing = false;

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

        public Result Parse(string input, Lexer lexer)
        {
            Lexer.LexerResult lexerResult = lexer.GetSpans(input);
            (bool, ParserSequence, List<ParserResult>) result = this.Check(lexerResult, lexer);
            return new Result() { Matched = result.Item1, LexerResult = lexerResult, Sequence = result.Item2, Results = result.Item3 };
        }

        public Result Parse(string input)
        {
            Lexer.LexerResult lexerResult = InputLexer.GetSpans(input);
            (bool, ParserSequence, List<ParserResult>) result = this.Check(lexerResult, InputLexer);
            return new Result() { Matched = result.Item1, LexerResult = lexerResult, Sequence = result.Item2, Results = result.Item3 };
        }

        public Result Parse(string input, string sequenceName)
        {
            Lexer.LexerResult lexerResult = InputLexer.GetSpans(input);
            (bool, ParserSequence, List<ParserResult>) result = this.Check(lexerResult, InputLexer, sequenceName);
            return new Result() { Matched = result.Item1, LexerResult = lexerResult, Sequence = result.Item2, Results = result.Item3 };
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            string defaultConfig = File.ReadAllText($"Configuration/Default.json");
            dynamic defaultObj = JsonConvert.DeserializeObject<dynamic>(defaultConfig);
            JObject lexerRules = defaultObj.lexer as JObject;

            string CSSConfig = File.ReadAllText($"Configuration/ParserCSS.json");
            string SQLConfig = File.ReadAllText($"Configuration/ParserSQL.json");
            string HtmlConfig = File.ReadAllText($"Configuration/ParserHtml.json");
            string JsConfig = File.ReadAllText($"Configuration/ParserJS.json");
            string EBNFConfig = File.ReadAllText($"Configuration/ParserEBNF.json");

            dynamic htmlObj = JsonConvert.DeserializeObject<dynamic>(HtmlConfig);
            dynamic sqlObj = JsonConvert.DeserializeObject<dynamic>(SQLConfig);
            dynamic cssObj = JsonConvert.DeserializeObject<dynamic>(CSSConfig);
            dynamic jsObj = JsonConvert.DeserializeObject<dynamic>(JsConfig);
            dynamic ebnfObj = JsonConvert.DeserializeObject<dynamic>(EBNFConfig);

            JObject htmlParserRules = htmlObj.parser as JObject;

            JObject sqlUserLexer = sqlObj.userLexer as JObject;
            JObject sqlParserRules = sqlObj.parser as JObject;

            JObject cssParserRules = cssObj.parser as JObject;

            JObject jsUserLexer = jsObj.userLexer as JObject;
            JObject jsParserRules = jsObj.parser as JObject;

            JObject ebnfUserLexer = ebnfObj.userLexer as JObject;
            JObject ebnfParserRules = ebnfObj.parser as JObject;

            Lexer htmlLexer = new Lexer(lexerRules, null);
            Lexer sqlLexer = new Lexer(lexerRules, sqlUserLexer);
            Lexer cssLexer = new Lexer(lexerRules, null);
            Lexer jsLexer = new Lexer(lexerRules, jsUserLexer);
            Lexer ebnfLexer = new Lexer(lexerRules, ebnfUserLexer);

            Parser htmlParser = new Parser(htmlLexer, htmlParserRules);
            Parser sqlParser = new Parser(sqlLexer, sqlParserRules);
            Parser cssParser = new Parser(cssLexer, cssParserRules);
            Parser jsParser = new Parser(jsLexer, jsParserRules);
            Parser ebnfParser = new Parser(ebnfLexer, ebnfParserRules);

            string inputHtml = "<html><head><title>Title</title></head><body><h2>Helloooo hi</h2><div>Here is <span>some</span> text</div></body></html>";
            string inputSql = "select column1, column2 as c2 from _yay";
            string inputCss = "body { background-color: #512fff; margin-left: auto; color: '#123456'; } h2 { color: 'blue'; }";
            string inputJs = "var i = \"Hello\"; var x=5;";
            string inputEBNF = "a=\"5\";";

            var htmlParserResult = htmlParser.Parse(inputHtml);
            var sqlParserResult = sqlParser.Parse(inputSql);
            var cssParserResult = cssParser.Parse(inputCss);
            var jsParserResult = jsParser.Parse(inputJs);

            var ebnfParserResult = ebnfParser.Parse(inputEBNF, "grammar");
            var exit = ebnfParser.CancelAllParsing;
        }
    }
}
