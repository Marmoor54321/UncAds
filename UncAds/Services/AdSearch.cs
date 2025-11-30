using System;
using System.Collections.Generic;
using System.Linq;
using UncAds.Models;

namespace UncAds.Services
{
    public static class AdSearch
    {
        public static bool Matches(Ad ad, string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return true;

            var tokens = ParseQuery(query);
            var rpn = ToRPN(tokens);
            return EvaluateRPN(rpn, ad);
        }

        //  1. PARSOWANIE Z PREFIKSAMI 
        private static List<SearchToken> ParseQuery(string query)
        {
            var tokens = new List<SearchToken>();
            var parts = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            int i = 0;

            while (i < parts.Length)
            {
                var part = parts[i].Trim();
                if (string.IsNullOrEmpty(part)) { i++; continue; }

                if (part.StartsWith("\""))
                {
                    var phrase = new List<string>();
                    while (i < parts.Length && !part.EndsWith("\""))
                    {
                        phrase.Add(parts[i++].Trim('"'));
                        if (i < parts.Length) part = parts[i].Trim();
                    }
                    if (i < parts.Length && part.EndsWith("\""))
                        phrase.Add(parts[i++].Trim('"'));

                    tokens.Add(new SearchToken
                    {
                        Type = TokenType.Phrase,
                        Value = string.Join(" ", phrase).ToLower(),
                        Field = null
                    });
                    continue;
                }

              
                var colonIndex = part.IndexOf(':');
                if (colonIndex > 0 && colonIndex < part.Length - 1)
                {
                    var field = part[..colonIndex].ToLower();
                    var value = part[(colonIndex + 1)..].ToLower();

                    if (IsValidField(field))
                    {
                        tokens.Add(new SearchToken
                        {
                            Type = TokenType.Word,
                            Value = value,
                            Field = field
                        });
                        i++;
                        continue;
                    }
                }

                
                var lower = part.ToLower();
                if (lower is "and" or "or" or "not")
                {
                    tokens.Add(new SearchToken
                    {
                        Type = lower switch { "and" => TokenType.And, "or" => TokenType.Or, "not" => TokenType.Not, _ => TokenType.And },
                        Value = lower,
                        Field = null
                    });
                }
                else
                {
                    tokens.Add(new SearchToken
                    {
                        Type = TokenType.Word,
                        Value = lower,
                        Field = null 
                    });
                }
                i++;
            }

            return tokens;
        }

        private static bool IsValidField(string field) =>
            field is "title" or "description" or "category" or "attribute" or "value";

        //  2. RPN 
        private static List<SearchToken> ToRPN(List<SearchToken> tokens)
        {
            var output = new List<SearchToken>();
            var opStack = new Stack<SearchToken>();

            foreach (var token in tokens)
            {
                if (token.Type == TokenType.Word || token.Type == TokenType.Phrase)
                    output.Add(token);
                else if (token.Type == TokenType.And || token.Type == TokenType.Or || token.Type == TokenType.Not)
                {
                    while (opStack.Any() && HasHigherPrecedence(opStack.Peek(), token))
                        output.Add(opStack.Pop());
                    opStack.Push(token);
                }
            }

            while (opStack.Any())
                output.Add(opStack.Pop());

            return output;
        }

        private static bool HasHigherPrecedence(SearchToken top, SearchToken current)
        {
            int Prec(TokenType t) => t switch { TokenType.Not => 3, TokenType.And => 2, TokenType.Or => 1, _ => 0 };
            return Prec(top.Type) >= Prec(current.Type);
        }

        // 3. EWALUACJA RPN
        private static bool EvaluateRPN(List<SearchToken> rpn, Ad ad)
        {
            var stack = new Stack<bool>();

            foreach (var token in rpn)
            {
                if (token.Type == TokenType.Word || token.Type == TokenType.Phrase)
                    stack.Push(MatchToken(ad, token));
                else if (token.Type == TokenType.Not)
                {
                    if (stack.Count == 0) continue;
                    stack.Push(!stack.Pop());
                }
                else if (token.Type == TokenType.And || token.Type == TokenType.Or)
                {
                    if (stack.Count < 2) return false;
                    var right = stack.Pop();
                    var left = stack.Pop();
                    stack.Push(token.Type == TokenType.And ? left && right : left || right);
                }
            }

            return stack.Count > 0 && stack.Pop();
        }

        // 4. DOPASOWANIE Z POLEM 
        private static bool MatchToken(Ad ad, SearchToken token)
        {
            var search = token.Value.ToLower();

     
            if (string.IsNullOrEmpty(token.Field))
            {
                return MatchAnywhere(ad, search);
            }

            return token.Field switch
            {
                "title" => ad.Title?.ToLower().Contains(search) == true,
                "description" => ad.Description?.ToLower().Contains(search) == true,
                "category" => ad.AdCategories?.Any(ac =>
                    ac.Category?.Name?.ToLower().Contains(search) == true ||
                    ac.Category?.FullPath?.ToLower().Contains(search) == true
                ) == true,
                "attribute" => ad.AttributeValues?.Any(av =>
                    av.CategoryAttribute?.Name?.ToLower().Contains(search) == true
                ) == true,
                "value" => ad.AttributeValues?.Any(av =>
                    av.Value?.ToLower().Contains(search) == true
                ) == true,
                _ => false
            };
        }

        private static bool MatchAnywhere(Ad ad, string search)
        {
            return
                ad.Title?.ToLower().Contains(search) == true ||
                ad.Description?.ToLower().Contains(search) == true ||
                ad.AdCategories?.Any(ac =>
                    ac.Category?.Name?.ToLower().Contains(search) == true ||
                    ac.Category?.FullPath?.ToLower().Contains(search) == true
                ) == true ||
                ad.AttributeValues?.Any(av =>
                    av.CategoryAttribute?.Name?.ToLower().Contains(search) == true ||
                    av.Value?.ToLower().Contains(search) == true
                ) == true;
        }
    }

    enum TokenType { Word, Phrase, And, Or, Not }

    class SearchToken
    {
        public TokenType Type { get; set; }
        public string Value { get; set; } = "";
        public string? Field { get; set; } 
    }
}