namespace TextProcessorCoreLib
{
    public interface ITextProcessor
    {
        public void ProcessText(TextReader reader, TextWriter writer);
    }

    public class ChunkTextProcessor(IChunkTextFilter filter, uint chunkSize = 4096) : ITextProcessor
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
