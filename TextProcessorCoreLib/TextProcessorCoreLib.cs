using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Controls;

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

    public interface ITextProcessor
    {
        public void ProcessText(TextReader reader, TextWriter writer);
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

        private StringBuilder _stringBuilder = new();
        private StringBuilder _longWordBuilder = new();
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
            if (isWord.IsMatch(token))
            {
                return token.Length >= minLength;
            }
            if (removePunctuation && isPunctuation.IsMatch(token)) { return false; }
            if (removeWhitespaces && isWhitespace.IsMatch(token)) { return false; }
            return true;
        }
    }

    public class ChunkTextProcessor(IChunkTextFilter filter, uint chunkSize = 3) : ITextProcessor
    {
        //possibly (i don't really know) its better to limit chunk size to min of 4096 because of default filesystem chunk size
        private readonly uint _chunkSize = uint.Max(chunkSize, 1);
        private IChunkTextFilter Filter { get; } = filter;

        public void ProcessText(TextReader reader, TextWriter writer)
        {
            var buffer = new char[_chunkSize];
            int bytesReadCount;
            while ((bytesReadCount = reader.ReadBlock(buffer, 0, (int)_chunkSize)) > 0 )
            {
                writer.Write(Filter.Filter(new string(buffer, 0, bytesReadCount)));
            }

            if (Filter.IsGoodToken(Filter.LastToken))
            {
                writer.Write(Filter.LastToken);
            }
        }
    }
}
