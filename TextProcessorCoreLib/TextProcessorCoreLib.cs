using System.Text.RegularExpressions;

namespace TextProcessorCoreLib
{
    public interface ITextFilter
    {
        string Filter(string from);
    }

    public interface ITextFilterChunk : ITextFilter
    {
        public bool IsGoodToken(string token);
        public string LastToken { get; }
    }

    public interface ITextProcessor
    {
        public void ProcessText(TextReader reader, TextWriter writer);
    }
    public class TextFilter(uint minLength, bool removePunctuation = false, bool removeWhitespaces = false) : ITextFilterChunk
    {
        public string LastToken { get; private set; } = string.Empty;

        //Good - means we leave it, bad - we filter it
        public bool IsGoodToken(string token)
        {
            return !((token.All(char.IsLetter) && token.Length < minLength) ||
                     removePunctuation && token.All(char.IsPunctuation) ||
                     removeWhitespaces && token.All(char.IsWhiteSpace));

        }

        public string Filter(string from)
        {
            if (from == string.Empty) { return from;}
            var tokens = Regex.Split(from, @"\b").Where(c => c != string.Empty).ToArray();

            //we do not where chunk border lies
            //if it happens to be on the letter - we risk misfiltering by length and can not know how to manage it  until we read  next chunk
            //To be sure - always do this, no matter what and the add "tokens" to next query for fool proof parsing
            LastToken = tokens.Last();

            var filteredTokens = tokens.SkipLast(1).Where(IsGoodToken);

            
            //var filteredTokens = tokens.SkipLast(1)
            //    .Where(token => token.Length >= minLength || (!token.All(char.IsLetter)))
            //    .Select(word =>
            //        removePunctuation
            //            ? new string(word.Where(c => !char.IsPunctuation(c)).ToArray())
            //            : word)
            //    .Select(token =>
            //        removeWhitespaces
            //            ? new string(token.Where(c => !char.IsWhiteSpace(c)).ToArray())
            //            : token);

            return string.Join("", filteredTokens);
        }
    }

    public class ChunkTextProcessor(ITextFilterChunk filter, uint chunkSize = 4096) : ITextProcessor
    {
        //possibly (i don't really know) its better to limit chunk size to min of 4096 because of default filesystem chunk size
        private readonly uint _chunkSize = uint.Max(chunkSize, 1);
        private ITextFilterChunk Filter { get; } = filter;

        public void ProcessText(TextReader reader, TextWriter writer)
        {
            var buffer = new char[_chunkSize];
            int bytesReadCount;
            while ((bytesReadCount = reader.ReadBlock(buffer, 0, (int)_chunkSize)) > 0 )
            {
                writer.Write(Filter.Filter(filter.LastToken+new string(buffer, 0, bytesReadCount)));
            }

            if (Filter.IsGoodToken(Filter.LastToken))
            {
                writer.Write(Filter.LastToken);
            }
        }
    }
}
