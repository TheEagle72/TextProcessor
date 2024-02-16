using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TextProcessorCoreLib
{
    public interface ITextFilter
    {
        string Filter(string str);
    }

    public interface IChunkTextFilter : ITextFilter
    {
        public bool IsGoodToken(string token);
        public string LastToken { get; }
    }

    public class ChunkTextFilter(uint minLength, bool removePunctuation = false, bool removeWhitespaces = false) : IChunkTextFilter
    {
        internal static Regex isWord = new(@"\w+");
        internal static Regex isWhitespace = new(@"\s+");
        internal static Regex isPunctuation = new(@"\p{P}+");
        internal static Regex splitter = new(@"(\w+|\s+|\p{P})");

        public string LastToken => _longWordBuilder.ToString();

        private bool longWordProcessing = false;
        private bool longWordIsGood = false;

        private readonly StringBuilder _stringBuilder = new();
        private readonly StringBuilder _longWordBuilder = new();
        public string Filter(string str)
        {
            _stringBuilder.Clear();

            if (str == string.Empty) { return str; }

            var matches = splitter.Matches(str);
            switch (matches.Count)
            {
                case 0:
                    _stringBuilder.Append(str);
                    break;

                case 1:
                    ProcessLastToken(matches.Last().ValueSpan);
                    break;

                case 2:
                    ProcessFirstToken(matches.First().ValueSpan);
                    ProcessLastToken(matches.Last().ValueSpan);
                    break;

                default:
                    ProcessFirstToken(matches.First().ValueSpan);
                    foreach (var token in matches.Skip(1).SkipLast(1).Where(t => IsGoodToken(str.AsSpan(t.Index, t.Length))))
                    {
                        _stringBuilder.Append(token.Value);
                    }
                    ProcessLastToken(matches.Last().ValueSpan);
                    break;
            }

            return _stringBuilder.ToString();
        }

        private void ProcessFirstToken(ReadOnlySpan<char> token)
        {
            if (longWordProcessing)
            {
                if (isWord.IsMatch(token))
                {
                    if (longWordIsGood)
                    {
                        _stringBuilder.Append(token);
                    }
                    else
                    {
                        _longWordBuilder.Append(token);
                        if (_longWordBuilder.Length >= minLength)
                        {
                            _stringBuilder.Append(_longWordBuilder.ToString());
                            _longWordBuilder.Clear();
                        }
                    }
                }
            }
            else
            {
                if (IsGoodToken(token))
                {
                    _stringBuilder.Append(token);
                }
            }
            longWordProcessing = false;
            longWordIsGood = false;
        }

        private void ProcessLastToken(ReadOnlySpan<char> token)
        {
            if (isWord.IsMatch(token))
            {
                longWordProcessing = true;
                if (longWordIsGood)
                {
                    _stringBuilder.Append(_longWordBuilder.ToString());
                    _stringBuilder.Append(token);
                }
                else
                {
                    _longWordBuilder.Append(token);
                    if (_longWordBuilder.Length >= minLength)
                    {
                        longWordIsGood = true;
                        _stringBuilder.Append(_longWordBuilder.ToString());
                        _longWordBuilder.Clear();
                    }
                }
            }
            else
            {
                if (IsGoodToken(token))
                {
                    _stringBuilder.Append(token);
                }
            }
        }

        public bool IsGoodToken(string token)
        {
            return IsGoodToken(token.AsSpan());
        }

        public bool IsGoodToken(ReadOnlySpan<char> token)
        {
            //this method depends on tokens being 'clean', only words or punctuation or whitespace, not at the same time...
            //not very secure. oh well... ¯\_(ツ)_/¯ need to think, how to handle its better

            var isCurrentTokenWord = isWord.IsMatch(token);
            var isCurrentTokenWhitespace = isWhitespace.IsMatch(token);
            var isCurrentTokenPunctuation = isPunctuation.IsMatch(token);

            //use xor? 
            //if (!(isCurrentTokenWord ^ isCurrentTokenWhitespace ^ isCurrentTokenPunctuation))
            //{
            //    return false;
            //}

            if (isCurrentTokenWord)
            {
                return token.Length >= minLength;
            }

            if (removePunctuation && isCurrentTokenPunctuation) { return false; }

            if (removeWhitespaces && isCurrentTokenWhitespace) { return false; }

            //if token is not 'clean' - we filter it, so 'false'
            //this behavior is debatable, depends on what we really want from filtered text
            // ¯\_(ツ)_/¯
            
            return true;
        }
    }

}
