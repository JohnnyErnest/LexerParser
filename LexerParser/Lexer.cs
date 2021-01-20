using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LexerParser
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
                bool Evaluate(char input);
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
                    //if (input.Length == 0 || input.Length > 1) return false;
                    //return input[0].Equals(Token);
                    return input.Equals(Token);
                    //return string.CompareOrdinal(Token.ToString(), input) == 0;
                }
                public override string ToString()
                {
                    return $"[{RuleName}, Char:{Token}]";
                }
                public bool Evaluate(char input)
                {
                    return input.Equals(Token);
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

                public bool Evaluate(char input)
                {
                    return Tokens.Contains(input);
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
                    return input.Equals(Token, StringComparison.OrdinalIgnoreCase);
                    //return input.ToLower() == Token.ToLower();
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

                public bool Evaluate(char input)
                {
                    return input.ToString().Equals(Token, StringComparison.OrdinalIgnoreCase);
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
                    return input.Equals(Token, StringComparison.Ordinal);
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

                public bool Evaluate(char input)
                {
                    return input.ToString().Equals(Token, StringComparison.Ordinal);
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
                public bool Evaluate(char input)
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
                public bool Evaluate(char input)
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
                    foreach (char c in input)
                    {
                        if (!Rule.Evaluate(c)) { return false; }
                        //if (!Rule.Evaluate(new string(c,1))) { return false; }
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
                public bool Evaluate(char input)
                {
                    return Rule.Evaluate(input);
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
                return $"[Start:{Start} Text:{Text}, Rule:{Rule.RuleName}/{Rule.RuleType}, End/Length:{End}/{Length}]";
            }
        }
        public class LexerResult
        {
            public List<Span> RawSpans { get; set; }
            public List<Span> OrganizedSpans { get; set; }
            public List<Span> SingularSpans { get; set; }
            public List<Span> Parsed1 { get; set; }
            public List<Span> ParsedOrganized { get; set; }
        }
        public List<LexerRules.ILexerRule> Rules { get; set; }
        LexerRules.ILexerRule GetRule(string key, JToken token)
        {
            JToken value = token;
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
                    throw new Exception("Error determining rule: " + val);
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
                JToken value = l.Value;
                if (value.Type == JTokenType.String)
                {
                    rules.Add(GetRule(key, value));
                }
                else if (value.Type == JTokenType.Array)
                {
                    List<LexerRules.ILexerRule> r1 = new List<LexerRules.ILexerRule>();
                    foreach (var v1 in value)
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
            //dynamic defaultObj = JsonConvert.DeserializeObject<dynamic>(defaultConfig);
            //JObject lexerRules = (JObject)defaultObj.lexer;

            string userConfig = File.ReadAllText(userConfigurationFile);
            //dynamic userObj = JsonConvert.DeserializeObject<dynamic>(userConfig);
            //JObject lexerUserRules = userObj.userLexer as JObject;

            JObject lexerRules = (JObject)JObject.Parse(defaultConfig)["lexer"];
            JObject lexerUserRules = (JObject)JObject.Parse(userConfig)["userLexer"];

            Init(lexerRules, lexerUserRules);
        }
        public Lexer(JObject lexer, JObject userLexer)
        {
            Init(lexer, userLexer);
        }
        List<LexerRules.ILexerRule> ProcessRules(char input)
        {
            return Rules.Where(x => x.Evaluate(input)).ToList();
        }

        List<LexerRules.ILexerRule> ProcessRules(string input)
        {
            return Rules.Where(x => x.Evaluate(input)).ToList();
        }
        public LexerResult GetSpans(string input)
        {
            var spans = ProcessText(input, removeInnerDupes:true);
            var result = new LexerResult()
            {
                RawSpans = spans.Item1,
                //OrganizedSpans = OrganizeSpans(spans.Item1),
                OrganizedSpans = OrganizeSpans(spans.Item3),
                SingularSpans = spans.Item2,
                Parsed1 = spans.Item3,
                ParsedOrganized = OrganizeSpans(spans.Item3)
            };
            return result;
        }
        public (List<Span>, List<Span>, List<Span>) ProcessText(string input, bool removeInnerDupes = false)
        {
            List<Span> spans = new List<Span>();
            List<Span> spans2 = new List<Span>();
            int len1 = 1;
            int inLen = input.Length;
            List<(int start, int len)> allInts = new List<(int, int)>();
            for (int i1 = 0; i1 < inLen; i1++)
            {
                for (int i2 = 0; i2 < inLen; i2++)
                {
                    if ((i2 + len1) > inLen)
                    {
                        break;
                    }
                    allInts.Add((i2, len1));
                }
                len1++;
            }
            allInts = allInts.GroupBy(x => new { x.start, x.len }).Select(x => (x.Key.start, x.Key.len)).ToList();
            //foreach (var item in allInts)
            //{
            //    string substr = input.Substring(item.start, item.len);
            //    foreach (var p in ProcessRules(substr))
            //    {
            //        spans.Add(new Span() { Rule = p, Start = item.start, Text = substr });
            //    }
            //}

            var allChars = allInts.Where(x => x.len == 1).ToArray();
            allInts = allInts.Where(x => x.len > 1).ToList();
            foreach (var item in allInts)
            {
                string substr = input.Substring(item.start, item.len);
                foreach (var p in ProcessRules(substr))
                {
                    spans.Add(new Span() { Rule = p, Start = item.start, Text = substr });
                }
            }
            foreach (var item in allChars)
            {
                char c = input[item.start];
                foreach (var p in ProcessRules(c))
                {
                    spans.Add(new Span() { Rule = p, Start = item.start, Text = c.ToString() });
                }
            }

            //spans = spans2.ToList();
            //spans2 = spans2.OrderByDescending(x => x.Length).OrderBy(x => x.Start).ThenByDescending(x => x.Rule.Ordinal).ToList();
            spans = spans.OrderByDescending(x => x.Length).OrderBy(x => x.Start).ThenByDescending(x => x.Rule.Ordinal).ToList();

            List<Span> res1 = new List<Span>();
            if (removeInnerDupes)
            {
                int repeat_idx = 0;
                var repeat_list = spans.Where(x => x.Rule.RuleType == "RepeatRule").Select(x => new { idx = repeat_idx++, start = x.Start, end = x.End, len = x.Length, x }).ToList();
                bool done_repeats = false;
                repeat_idx = 0;
                while (!done_repeats)
                {
                    if (repeat_idx >= repeat_list.Count) { done_repeats = true; }
                    else
                    {
                        var current = repeat_list[repeat_idx];
                        repeat_list.RemoveAll(x => x != current && 
                            x.start >= current.start && 
                            x.end <= current.end && 
                            current.x.Rule.RuleName == x.x.Rule.RuleName);
                        repeat_idx++;
                    }
                }
                res1.AddRange(repeat_list.Select(x => x.x).ToArray());
                res1.AddRange(spans.Where(x => x.Rule.RuleType != "RepeatRule").ToArray());
                res1 = res1.OrderBy(x => x.Start).ThenByDescending(x => x.Rule.Ordinal).ToList();
                //var res2 = res1.ToArray();
                //res1 = new List<Span>();
                //foreach(var a in res2)
                //{
                //    res1.Add(new Span() { Start = a.Start, Rule = a.Rule, Text = a.Text, InnerSpans = a.InnerSpans });
                //}
            }
            List<Span> singularSpans = spans.Where(x => x.Length == 1).OrderBy(x => x.Start).ThenByDescending(x => x.Rule.Ordinal).ToList();
            return (spans, singularSpans, res1);
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
            while (!done)
            {
                Span span1 = spans[idx];
                int idx2 = idx + 1;
                span1.InnerSpans = new List<Span>();
                List<int> removeIndexes = new List<int>();
                for (int i = idx2; i < spans.Count; i++)
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
            foreach (var span in spans)
            {
                if (span.InnerSpans != null)
                {
                    span.InnerSpans = span.InnerSpans.OrderBy(x => x.Start).ToList();
                }
            }
            return spans;
        }
    }
}
