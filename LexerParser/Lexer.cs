using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                string StringToken { get; }
                bool Evaluate(string input);
                bool Evaluate(char input);
                bool Evaluate(char[] input);
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
                public string StringToken { get { return Token.ToString(); } }
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
                public bool Evaluate(char[] input)
                {
                    if (input.Length == 0 || input.Length > 1) return false;
                    return input[0].Equals(Token);
                }
                public override bool Equals(object obj)
                {
                    var o = (CharLexerRule)obj;
                    if (o.RuleName == this.RuleName) return true;
                    return false;
                }
                public override int GetHashCode()
                {
                    return this.RuleName.GetHashCode();
                }
            }
            public class CharInLexerRule : ILexerRule
            {
                public int Ordinal { get; set; } = 2;
                public string RuleName { get; set; }
                public string RuleType { get; set; } = "CharIn";
                public char[] Tokens { get; set; }
                public string StringToken { get { return new string(Tokens); } }
                public bool Evaluate(string input)
                {
                    if (input.Length == 0 || input.Length > 1) return false;
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
                public bool Evaluate(char[] input)
                {
                    if (input.Length == 0 || input.Length > 1) return false;
                    return Tokens.Contains(input[0]);
                }
                public override bool Equals(object obj)
                {
                    var o = (CharInLexerRule)obj;
                    if (o.RuleName == this.RuleName) return true;
                    return false;
                }
                public override int GetHashCode()
                {
                    return this.RuleName.GetHashCode();
                }

            }
            public class CaseInsensitiveStringLexerRule : ILexerRule
            {
                public int Ordinal { get; set; } = 6;
                public string RuleName { get; set; }
                public string RuleType { get; set; } = "CaseInsensitiveString";
                public string Token { get; set; }
                public string StringToken { get { return Token; } }
                char[] _TokenCharArray { get; set; }
                public bool Evaluate(char[] input)
                {
                    return Token.Equals(new string(input), StringComparison.OrdinalIgnoreCase);
                }
                public bool Evaluate(string input)
                {
                    //return string.CompareOrdinal(input, Token) == 0;
                    return input.Equals(Token, StringComparison.OrdinalIgnoreCase);
                    //return input.ToLower() == Token.ToLower();
                }
                public CaseInsensitiveStringLexerRule(string ruleName, string input)
                {
                    RuleName = ruleName;
                    Token = input;
                    _TokenCharArray = Token.ToCharArray();
                }
                public override string ToString()
                {
                    return $"[{RuleName}, CaseInsensitive String:{Token}]";
                }
                public bool Evaluate(char input)            
                {
                    return input.ToString().Equals(Token, StringComparison.OrdinalIgnoreCase);
                }
                public override bool Equals(object obj)
                {
                    var o = (CaseInsensitiveStringLexerRule)obj;
                    if (o.RuleName == this.RuleName) return true;
                    return false;
                }
                public override int GetHashCode()
                {
                    return this.RuleName.GetHashCode();
                }

            }
            public class StringLexerRule : ILexerRule
            {
                public int Ordinal { get; set; } = 7;
                public string RuleName { get; set; }
                public string RuleType { get; set; } = "String";
                public string Token { get; set; }
                public string StringToken { get { return Token; } }
                char[] _TokenCharArray { get; set; }
                public bool Evaluate(char[] input)
                {
                    //var correct = input.Equals(_TokenCharArray);
                    //return _TokenCharArray == input;
                    //return Token.Equals(new string(input), StringComparison.Ordinal);

                    if (input.Length != _TokenCharArray.Length) { return false; }
                    for (int i = 0; i < input.Length; i++)
                    {
                        if (input[i] != _TokenCharArray[i]) { return false; }
                    }
                    return true;
                }
                public bool Evaluate(string input)
                {
                    //return input.Equals(Token, StringComparison.Ordinal);
                    return input.Equals(Token);
                }
                public StringLexerRule(string ruleName, string input)
                {
                    RuleName = ruleName;
                    Token = input;
                    _TokenCharArray = Token.ToCharArray();
                }
                public override string ToString()
                {
                    return $"[{RuleName}, String:{Token}]";
                }
                public bool Evaluate(char input)
                {
                    return input.ToString().Equals(Token);
                    //return input.ToString().Equals(Token, StringComparison.Ordinal);
                }
                public override bool Equals(object obj)
                {
                    var o = (StringLexerRule)obj;
                    if (o.RuleName == this.RuleName) return true;
                    return false;
                }
                public override int GetHashCode()
                {
                    return this.RuleName.GetHashCode();
                }

            }
            public class RuleLookupLexerRule : ILexerRule, ILexerLookup
            {
                public int Ordinal { get; set; } = 3;
                public string RuleName { get; set; }
                public string RuleLookupName { get; set; }
                public string RuleType { get; set; } = "RuleLookup";
                public string StringToken { get { return null; } }
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
                public bool Evaluate(char[] input)
                {
                    if (LookedUpRule != null)
                    {
                        return LookedUpRule.Evaluate(input);
                    }
                    return false;
                }
                public override bool Equals(object obj)
                {
                    var o = (RuleLookupLexerRule)obj;
                    if (o.RuleName == this.RuleName) return true;
                    return false;
                }
                public override int GetHashCode()
                {
                    return this.RuleName.GetHashCode();
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
                public string StringToken { get { return null; } }
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
                public bool Evaluate(char[] input)
                {
                    foreach (var rule in Rules)
                    {
                        if (rule.Evaluate(input)) return true;
                    }
                    return false;
                }
                public override bool Equals(object obj)
                {
                    var o = (RuleCollectionRule)obj;
                    if (o.RuleName == this.RuleName) return true;
                    return false;
                }
                public override int GetHashCode()
                {
                    return this.RuleName.GetHashCode();
                }

            }
            public class RepeatRuleLexerRule : ILexerRule, ILexerLookup
            {
                public int Ordinal { get; set; } = 5;
                public string RuleName { get; set; }
                public string RepeatRuleName { get; set; }
                public string RuleType { get; set; } = "RepeatRule";
                public string StringToken { get { return null; } }
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
                public bool Evaluate(char[] input)
                {
                    foreach (char c in input)
                    {
                        if (!Rule.Evaluate(c)) { return false; }
                        //if (!Rule.Evaluate(new string(c,1))) { return false; }
                    }
                    return true;
                }
                public override bool Equals(object obj)
                {
                    var o = (RepeatRuleLexerRule)obj;
                    if (o.RuleName == this.RuleName) return true;
                    return false;
                }
                public override int GetHashCode()
                {
                    return this.RuleName.GetHashCode();
                }
            }
        }
        public class Span : ICloneable
        {
            public int Start { get; set; }
            public int End { get { return Start + Length; } }
            public int Length { get; set; }
            string _Text;
            public string Text { get { return _Text; } set { _Text = value; Length = _Text.Length; } }
            public LexerRules.ILexerRule Rule { get; set; }
            public List<Span> InnerSpans { get; set; }
            public bool IsBetween(int index)
            {
                return Start <= index && index < End;
            }
            public object Clone()
            {
                var span = new Span() { Text = this.Text, Start = this.Start, Rule = this.Rule };
                span.InnerSpans = new List<Span>();
                if (this.InnerSpans != null)
                {
                    foreach (var span1 in this.InnerSpans)
                    {
                        span.InnerSpans.Add(span1.Clone() as Span);
                    }
                }
                return span;
            }
            public override bool Equals(object obj)
            {
                var o = (Span)obj;
                if (o.Start == this.Start && o.Text == this.Text && o.Rule == this.Rule && o.InnerSpans.Count == this.InnerSpans.Count) return true;
                return false;
            }
            public override int GetHashCode()
            {
                return (this.Start, this.Text, this.Rule).GetHashCode();
            }
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
            public Task<List<Span>> OrganizableSpans { get; set; }
            public List<LexerResult> CollectionInnerResults { get; set; } = new List<LexerResult>();
            public List<(int Line, int Index, Span Data)> CollectionInnerSpans { get; set; } = new List<(int, int, Span)>();
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
        List<LexerRules.ILexerRule> ProcessRules(char[] input)
        {
            return Rules.Where(x => x.Evaluate(input)).ToList();
        }
        public LexerResult GetSpans(string input, int maxSlidingWindow = -1)
        {
            var spans1 = ProcessTextNew(input, removeInnerDupes: false, maxSlidingWindow: maxSlidingWindow);
            //var spans2 = ProcessText(input, removeInnerDupes: true);
            //var organized1 = OrganizeSpansNew(spans1.Item1);
            var organized1 = spans1.Item1;
            //var organized2 = OrganizeSpans(organized1);
            
            //var organized1 = OrganizeSpans(spans1.Item1);

            List<Span> newSpans = new List<Span>();
            foreach(var item in organized1)
            {
                var node = item.Clone() as Span;
                foreach (var item1 in node.InnerSpans)
                {
                    //item1.InnerSpans = new List<Span>();
                    item1.InnerSpans.Clear();

                    //var nodeInner = item1.Clone() as Span;
                    //nodeInner.InnerSpans = new List<Span>();
                    //newSpans.Add(nodeInner);
                }
                //node.InnerSpans.Clear();
                newSpans.Add(node);
                //item.InnerSpans = new List<Span>();
            }

            var organized2 = OrganizeSpans(newSpans);

            //var organized2 = OrganizeSpans(spans1.Item1);
            //if (organized1.Count != organized2.Count)
            //{
            //}

            //if (spans.Item1.Count != spans2.Item1.Count)
            //{
            //    List<Span> spans3 = new List<Span>();
            //    foreach(var s1 in spans.Item1)
            //    {
            //        bool found = false;
            //        foreach(var s2 in spans2.Item1)
            //        {
            //            if (s2.Start == s1.Start && s2.Text == s1.Text && s1.End == s2.End && s2.Rule.RuleName == s1.Rule.RuleName)
            //            {
            //                found = true;
            //                break;
            //            }
            //        }
            //        if (!found) { spans3.Add(s1); }
            //    }
            //}
            var result = new LexerResult()
            {
                RawSpans = spans1.Item1,
                //OrganizedSpans = OrganizeSpans(spans.Item1),
                OrganizedSpans = organized1,
                SingularSpans = null,
                Parsed1 = newSpans,
                ParsedOrganized = organized2,
                OrganizableSpans = new Task<List<Span>>(() => { return OrganizeSpans(newSpans); })
            };
            result.OrganizableSpans.Start();
            //var result = new LexerResult()
            //{
            //    RawSpans = spans1.Item1,
            //    //OrganizedSpans = OrganizeSpans(spans.Item1),
            //    OrganizedSpans = organized1,
            //    SingularSpans = null,
            //    Parsed1 = spans2.Item1,
            //    ParsedOrganized = organized2
            //};
            return result;
        }
        public LexerResult GetSpans(string[] input, int maxSlidingWindow = -1)
        {
            LexerResult lexerResult = new LexerResult();
            List<LexerResult> results = new List<LexerResult>();
            foreach(string line in input)
            {
                var spans1 = ProcessTextNew(line, removeInnerDupes: false, maxSlidingWindow: maxSlidingWindow);
                var organized1 = spans1.Item1;
                var org1a = OrganizeSpans(organized1);
                //List<Span> newSpans = new List<Span>();
                //foreach (var item in organized1)
                //{
                //    var node = item.Clone() as Span;
                //    foreach (var item1 in node.InnerSpans)
                //    {
                //        item1.InnerSpans.Clear();
                //    }
                //    newSpans.Add(node);
                //}
                //var organized2 = OrganizeSpans(newSpans);
                var result = new LexerResult()
                {
                    RawSpans = organized1,
                    OrganizedSpans = org1a,
                    SingularSpans = null,
                    //Parsed1 = newSpans,
                    //ParsedOrganized = organized2,
                    //OrganizableSpans = new Task<List<Span>>(() => { return OrganizeSpans(newSpans); })
                };
                //result.OrganizableSpans.Start();
                results.Add(result);
            }
            List<(int Line, int Index, Span Data)> spans = new List<(int, int, Span)>();
            int Line = 0;
            foreach(var item in results)
            {
                int Index = 0;
                foreach(var item1 in item.OrganizedSpans)
                {
                    spans.Add((Line, Index, item1));
                    Index++;
                }
                Line++;
            }
            lexerResult.CollectionInnerResults = results;
            lexerResult.CollectionInnerSpans = spans;
            return lexerResult;
        }
        public (List<Span>, List<Span>, List<Span>) ProcessTextNew(string input, bool removeInnerDupes = false, int maxSlidingWindow = -1)
        {
            var charRules = Rules.Where(x => new[] { "Char", "CharIn" }.Contains(x.RuleType)).ToArray();
            var stringRules = Rules.Where(x => new[] { "CaseInsensitiveString", "String" }.Contains(x.RuleType)).ToArray();
            var lookupRules = Rules.Where(x => x.RuleType == "RuleLookup").ToArray();
            var ruleCollections = Rules.Where(x => x.RuleType == "RuleCollection").ToArray();
            var repeatRules = Rules.Where(x => x.RuleType == "RepeatRule").ToArray();

            List<Span> spans = new List<Span>();
            Func<string, List<Span>> checkSingularRules1 = new Func<string, List<Span>>((input1) =>
            {
                //int idx = 0;
                List<Span> spans1 = new List<Span>();
                //foreach (var c in input1)
                //{
                //    foreach (var r in charRules)
                //    {
                //        if (r.Evaluate(c)) { spans1.Add(new Span() { Start = idx, Rule = r, Text = c.ToString(), InnerSpans = new List<Span>() }); }
                //    }
                //    idx++;
                //}
                for (int idx = 0; idx < input.Length; idx++)
                {
                    char c = input[idx];
                    for (int ii = 0; ii < charRules.Length; ii++)
                    {
                        Lexer.LexerRules.ILexerRule r = charRules[ii];
                        if (r.Evaluate(c)) { spans1.Add(new Span() { Start = idx, Rule = r, Text = c.ToString(), InnerSpans = new List<Span>() }); }
                    }
                }
                return spans1;
            });
            Func<string, List<Span>> checkSingularRules2 = new Func<string, List<Span>>((input1) =>
            {
                List<Span> spans1 = new List<Span>();
                foreach (var r in stringRules.Where(x => x.RuleType == "String"))
                {
                    if (input1.IndexOf(r.StringToken) > -1)
                    {
                        int idx1 = 0;
                        while (idx1 != -1)
                        {
                            idx1 = input1.IndexOf(r.StringToken, idx1);
                            if (idx1 != -1)
                            {
                                int len = r.StringToken.Length;
                                spans1.Add(new Span() { Start = idx1, Rule = r, InnerSpans = new List<Span>(), Text = input1.Substring(idx1, len) });
                                idx1++;
                            }
                        }
                    }
                }
                string input1Lower = input1.ToLower();
                foreach (var r in stringRules.Where(x => x.RuleType == "CaseInsensitiveString"))
                {
                    //if (input1Lower.Contains(r.StringToken.ToLower()))
                    if (input1.IndexOf(r.StringToken, StringComparison.OrdinalIgnoreCase) > -1)
                    {
                        int idx1 = 0;
                        while (idx1 != -1)
                        {
                            idx1 = input1Lower.IndexOf(r.StringToken, idx1, StringComparison.OrdinalIgnoreCase);
                            if (idx1 != -1)
                            {
                                int len = r.StringToken.Length;
                                spans1.Add(new Span() { Start = idx1, Rule = r, InnerSpans = new List<Span>(), Text = input1.Substring(idx1, len) });
                                idx1++;
                            }
                        }
                    }
                }
                return spans1;
            });
            Span[] charSpans = checkSingularRules1(input).OrderBy(x => x.Start).ToArray();
            Span[] stringSpans = checkSingularRules2(input).OrderByDescending(x => x.Length).ToArray();

            Func<string, int, List<Span>> checkLookupRules = new Func<string, int, List<Span>>((input1, idx1) => {
                List<Span> spans1 = new List<Span>();
                foreach(var r in lookupRules)
                {
                    if ((r as Lexer.LexerRules.RuleLookupLexerRule).LookedUpRule.Evaluate(input1))
                    {
                        spans1.Add(new Span() { InnerSpans = new List<Span>(), Start = idx1, Text = input1, Rule = r });
                    }
                }
                return spans1;
            });
            List<Span> lookupSpans = new List<Span>();
            for(int i=0;i<input.Length;i++)
            {
                lookupSpans.AddRange(checkLookupRules(input[i].ToString(), i));
            }
            Func<string, int, List<Span>> ruleCollectionRules = new Func<string, int, List<Span>>((input1, idx1) =>
            {
                List<Span> spans1 = new List<Span>();
                foreach(var r in ruleCollections)
                {
                    var r1 = r as Lexer.LexerRules.RuleCollectionRule;
                    if (r1.Evaluate(input1))
                    {
                        spans1.Add(new Span() { Text = input1, InnerSpans = new List<Span>(), Start = idx1, Rule = r });
                    }
                }
                return spans1;
            });
            List<Span> collectionSpans = new List<Span>();
            for (int i = 0; i < input.Length; i++)
            {
                collectionSpans.AddRange(ruleCollectionRules(input[i].ToString(), i));
            }
            List<Span> repeatSpans = new List<Span>();
            Func<string, int, List<Span>> checkRepeatRules = new Func<string, int, List<Span>>((input1, idx1) =>
            {
                List<Span> spans1 = new List<Span>();
                foreach (var r in repeatRules)
                {
                    var r1 = r as Lexer.LexerRules.RepeatRuleLexerRule;
                    if (r1.Evaluate(input1))
                    {
                        spans1.Add(new Span() { Text = input1, InnerSpans = new List<Span>(), Start = idx1, Rule = r });
                    }
                }
                return spans1;
            });
            for (int i = 0; i < input.Length; i++)
            {
                repeatSpans.AddRange(checkRepeatRules(input[i].ToString(), i));
            }
            repeatSpans = repeatSpans.OrderBy(x => x.Rule.RuleName).ThenBy(x => x.Start).ToList();
            List<Span> contiguousSpans = new List<Span>();
            List<List<Span>> contiguousStep = new List<List<Span>>();
            for(int i=0; i<repeatSpans.Count; i++)
            {
                int curIdx = i;
                var first = repeatSpans[i];
                List<Span> spansContig1 = new List<Span>();
                spansContig1.Add(first);
                int firstStart = first.Start;
                int lastStart = firstStart;
                for(int ii=i+1; ii < repeatSpans.Count; ii++)
                {
                    var current = repeatSpans[ii];
                    int curStart = current.Start;
                    if (curStart != lastStart + 1) {
                        i = curIdx;
                        break; 
                    }
                    if (current.Rule.RuleName != first.Rule.RuleName)
                    {
                        i = curIdx;
                        break;
                    }
                    spansContig1.Add(current);
                    lastStart = curStart;
                    curIdx++;
                }
                contiguousStep.Add(spansContig1);
            }
            foreach(var contig in contiguousStep.Where(x => x.Count > 1))
            {
                int min = contig.Min(x => x.Start);
                StringBuilder sb = new StringBuilder();
                foreach(var c in contig)
                {
                    sb.Append(c.Text);
                }
                contiguousSpans.Add(new Span()
                {
                    InnerSpans = new List<Span>(),
                    Rule = contig.First().Rule,
                    Start = min,
                    Text = sb.ToString()
                });
            }

            //List<Span> allSpans = new List<Span>();
            spans.AddRange(charSpans);
            spans.AddRange(lookupSpans);
            spans.AddRange(collectionSpans);

            spans.AddRange(repeatSpans);

            //foreach(var c in repeatSpans)
            //{
            //    List<Span> removeList = new List<Span>();
            //    foreach (var cc in repeatSpans)
            //    {
            //        if (cc.Start >= c.Start && cc.End <= c.End && c != cc)
            //        {
            //            //if (cc.InnerSpans.Count > 0)
            //            //{
            //            //    foreach (var ccc in cc.InnerSpans)
            //            //    {
            //            //        c.InnerSpans.Add(ccc.Clone() as Lexer.Span);
            //            //    }
            //            //    c.InnerSpans.AddRange(cc.InnerSpans);
            //            //}
            //            c.InnerSpans.Add(cc);
            //            removeList.Add(cc);
            //        }
            //    }
            //    repeatSpans = repeatSpans.Except(removeList).ToList();
            //}

            //spans = OrganizeSpans(spans);

            spans.AddRange(stringSpans);

            //foreach (var c in contiguousSpans)
            //{
            //    List<Span> removeList = new List<Span>();
            //    foreach (var cc in spans)
            //    {
            //        if (cc.Start >= c.Start && cc.End <= c.End)
            //        {
            //            //if (cc.InnerSpans.Count > 0)
            //            //{
            //            //    foreach (var ccc in cc.InnerSpans)
            //            //    {
            //            //        c.InnerSpans.Add(ccc.Clone() as Lexer.Span);
            //            //    }
            //            //    c.InnerSpans.AddRange(cc.InnerSpans);
            //            //}
            //            c.InnerSpans.Add(cc);
            //            removeList.Add(cc);
            //        }
            //    }
            //    spans = spans.Except(removeList).ToList();
            //}

            //spans.AddRange(allSpans);
            spans.AddRange(contiguousSpans);

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
                            //current.x.Rule.RuleName == x.x.Rule.RuleName
                            current.x.Rule.RuleName.Equals(x.x.Rule.RuleName));
                            //current.x.Rule.RuleName.Equals(x.x.Rule.RuleName, StringComparison.Ordinal)
                            //);
                            //current.x.Rule.RuleName.Equals(x.x.Rule.RuleName));
                        repeat_idx++;
                    }
                }
                res1.AddRange(repeat_list.Select(x => x.x).ToArray());
                res1.AddRange(spans.Where(x => x.Rule.RuleType != "RepeatRule").ToArray());
                res1 = res1.OrderBy(x => x.Start).ThenByDescending(x => x.Rule.Ordinal).ToList();
            }
            return (spans, spans, spans);
        }
        public (List<Span>, List<Span>, List<Span>) ProcessText(string input, bool removeInnerDupes = false, int maxSlidingWindow = -1)
        {
            List<Span> spans = new List<Span>();
            int len1 = 1;
            int inLen = input.Length;
            List<(int start, int len)> allIntsInner = new List<(int, int)>();
            //ReadOnlySpan<char> someChars = new ReadOnlySpan<char>(input.ToCharArray());
            for (int i1 = 0; i1 < inLen; i1++)
            {
                for (int i2 = 0; i2 < inLen; i2++)
                {
                    if ((i2 + len1) > inLen)
                    {
                        break;
                    }
                    allIntsInner.Add((i2, len1));
                }
                len1++;
                if (maxSlidingWindow > 0 && maxSlidingWindow <= len1)
                {
                    break;
                }
            }
            var allChars = allIntsInner.Where(x => x.len == 1).ToArray();
            var allIntsArray = allIntsInner.Where(x => x.len > 1).ToArray();
            foreach (var item in allIntsArray)
            {
                string substr = input.Substring(item.start, item.len);
                var procRules = ProcessRules(substr).ToArray();
                foreach (var p in procRules)
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
            spans = spans.Distinct().ToList();
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
                            current.x.Rule.RuleName.Equals(x.x.Rule.RuleName)
                            //current.x.Rule.RuleName.Equals(x.x.Rule.RuleName)
                            );
                        //repeat_list.RemoveAll(x => x != current && 
                        //    x.start >= current.start && 
                        //    x.end <= current.end && 
                        //    current.x.Rule.RuleName == x.x.Rule.RuleName);
                        repeat_idx++;
                    }
                }
                res1.AddRange(repeat_list.Select(x => x.x).ToArray());
                res1.AddRange(spans.Where(x => x.Rule.RuleType != "RepeatRule").ToArray());
                res1 = res1.OrderBy(x => x.Start).ThenByDescending(x => x.Rule.Ordinal).ToList();
            }
            List<Span> singularSpans = spans.Where(x => x.Length == 1).OrderBy(x => x.Start).ThenByDescending(x => x.Rule.Ordinal).ToList();
            return (spans, singularSpans, res1);
        }
        public (List<Span>, List<Span>, List<Span>) ProcessTextCharArray(string input, bool removeInnerDupes = false)
        {
            List<Span> spans = new List<Span>();
            List<Span> spans2 = new List<Span>();
            ReadOnlySpan<char> inputChars = new ReadOnlySpan<char>(input.ToCharArray());
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
            //allInts = allInts.GroupBy(x => new { x.start, x.len }).Select(x => (x.Key.start, x.Key.len)).ToList();
            //foreach (var item in allInts)
            //{
            //    string substr = input.Substring(item.start, item.len);
            //    foreach (var p in ProcessRules(substr))
            //    {
            //        spans.Add(new Span() { Rule = p, Start = item.start, Text = substr });
            //    }
            //}

            var allChars = allInts.Where(x => x.len == 1).ToArray();
            //allInts = allInts.Where(x => x.len > 1).ToList();
            var allIntsArray = allInts.Where(x => x.len > 1).ToArray();
            //foreach (var item in allInts)
            foreach (var item in allIntsArray)
            {
                //string substr = input.Substring(item.start, item.len);
                //char[] chars = new char[item.len];
                //inputChars.Slice()
                //Array.Copy(inputChars, item.start, chars, 0, item.len);
                //string newStr = new string(chars);

                //string newStr = new string(inputChars.Slice(item.start, item.len).ToArray());
                //var chars1 = inputChars.Slice(item.start, item.len).ToArray();
                var charsSlice = inputChars.Slice(item.start, item.len);
                var chars1 = charsSlice.ToArray();
                var procRules = ProcessRules(chars1).ToArray();
                foreach (var p in procRules)
                {
                    spans.Add(new Span() { Rule = p, Start = item.start, Text = new string(chars1) });
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
            //spans = spans.OrderByDescending(x => x.Length).ThenBy(x => x.Start).ThenByDescending(x => x.Rule.Ordinal).ToList();
            spans = spans.OrderByDescending(x => x.Length).ThenByDescending(x => x.Rule.Ordinal).ThenBy(x => x.Start).ToList();
            bool done = false;
            int idx = 0;
            List<Span> results = new List<Span>();
            while (!done)
            {
                Span span1 = spans[idx];

                int idx2 = idx + 1;
                span1.InnerSpans = new List<Span>();
                List<int> removeIndexes = new List<int>();
                //List<Span> removeSpans = new List<Span>();
                //int i1 = idx2;
                //var spansA = spans.Skip(i1).Take(spans.Count - i1).Where(x => x.Start >= span1.Start && x.End <= span1.End);
                //foreach(var c in spansA)
                //{
                //    span1.InnerSpans.Add(c);
                //    removeIndexes.Add(i1);
                //    i1++;
                //}
                for (int i = idx2; i < spans.Count; i++)
                {
                    Span span2 = spans[i];
                    if (span2.Start >= span1.Start && span2.End <= span1.End)
                    {
                        span1.InnerSpans.Add(span2);
                        if (span2.InnerSpans != null)
                        {
                            span1.InnerSpans.AddRange(span2.InnerSpans);
                        }
                        removeIndexes.Add(i);
                        //removeSpans.Add(span2);
                    }
                }
                results.Add(span1);
                //spans = spans.Except(removeSpans).ToList();
                int[] removeIndexes1 = removeIndexes.OrderByDescending(x => x).ToArray();
                for (int i=0;i<removeIndexes1.Length;i++)
                {
                    spans.RemoveAt(removeIndexes1[i]);
                }
                //foreach (var i in removeIndexes.OrderByDescending(x => x).ToArray())
                //{
                //    spans.RemoveAt(i);
                //}
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
        public List<Span> OrganizeSpansNew(List<Span> spans)
        {
            spans = spans.OrderByDescending(x => x.Length).ThenByDescending(x => x.Rule.Ordinal).ThenBy(x => x.Start).ToList();
            int idx_a = 0;
            List<(int Index, Span Item)> spansIdx = spans.Select(x => (idx_a++, x)).ToList();
            bool done = false;
            int idx = 0;
            List<Span> results = new List<Span>();
            var enumerator = spansIdx.GetEnumerator();
            enumerator.MoveNext();
            while (!done)
            {
                //Span span1 = spans[idx];
                (int Index, Span Item) span1 = spansIdx.Where(x => x.Index >= idx).OrderBy(x => x.Index).FirstOrDefault();
                if (span1.Item == null) { done = true; break; }
                span1.Item.InnerSpans = new List<Span>();
                var array = spansIdx.Where(x => x.Item != span1.Item &&
                    x.Item.Start >= span1.Item.Start && x.Item.End <= span1.Item.End)
                    .Select(x => { span1.Item.InnerSpans.Add(x.Item); return x; }).ToArray();
                //span1.Item.InnerSpans.AddRange(spansIdx.Where())
                spansIdx.RemoveAll(x => x.Item != span1.Item &&
                    x.Item.Start >= span1.Item.Start && x.Item.End <= span1.Item.End);
                if (spansIdx.Count > 25000) { Console.WriteLine("Spans left to process: " + spansIdx.Count); }
                //enumerator.MoveNext();
                idx++;
                //if (idx >= spans.Count) { done = true; }
            }
            spans = spansIdx.Select(x => x.Item).OrderBy(x => x.Start).ToList();
            foreach (var span in spans)
            {
                if (span.InnerSpans != null)
                {
                    span.InnerSpans = span.InnerSpans.OrderBy(x => x.Start).ToList();
                }
            }
            return spans;
        }
        public List<Span> TransposeSpans(List<(int Line, int Index, Span Data)> input)
        {
            List<Span> output = new List<Span>();
            int line = 0;
            int spanEnd = 0;
            Span lastSpan = null;
            foreach(var data in input)
            {
                if (data.Line != line)
                {
                    //Span spanCR = new Span() { InnerSpans = new List<Span>(), Start = spanEnd, Text = "\r", Rule = new Lexer.LexerRules.CharLexerRule("carriageReturn", '\r') { Token = '\r', RuleName = "carriageReturn" } };
                    //spanCR.InnerSpans.Add(new Span() { InnerSpans = new List<Span>(), Start = spanEnd, Text = "\r", Rule = new Lexer.LexerRules.RuleLookupLexerRule("whitespace", "carriageReturn") });
                    //spanCR.InnerSpans.Add(new Span() { InnerSpans = new List<Span>(), Start = spanEnd, Text = "\r", Rule = new Lexer.LexerRules.RepeatRuleLexerRule("whitespaces", "whitespace") });
                    //output.Add(spanCR);
                    //spanEnd++;
                    //Span spanLF = new Span() { InnerSpans = new List<Span>(), Start = spanEnd, Text = "\n", Rule = new Lexer.LexerRules.CharLexerRule("lineFeed", '\n') { Token = '\n', RuleName = "lineFeed" } };
                    //spanLF.InnerSpans.Add(new Span() { InnerSpans = new List<Span>(), Start = spanEnd, Text = "\n", Rule = new Lexer.LexerRules.RuleLookupLexerRule("whitespace", "lineFeed") });
                    //spanLF.InnerSpans.Add(new Span() { InnerSpans = new List<Span>(), Start = spanEnd, Text = "\n", Rule = new Lexer.LexerRules.RepeatRuleLexerRule("whitespaces", "whitespace") });
                    //output.Add(spanLF);
                    //spanEnd++;
                    line = data.Line;
                }
                //data.Data.Start += spanEnd;
                //lastSpan = data.Data;

                int curStart = data.Data.Start;
                data.Data.Start = spanEnd;
                foreach(var item in data.Data.InnerSpans)
                {
                    int min = item.Start - curStart;
                    item.Start = spanEnd + min;
                }
                spanEnd += data.Data.Length;
                output.Add(data.Data);
            }
            return output;
        }
    }
}
