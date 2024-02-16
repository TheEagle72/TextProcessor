using System.Text.RegularExpressions;
using TextProcessorCoreLib;

namespace TextProcessorTest
{
    public class TestChunkTextFilter
    {
        [TestCase(".", true)]
        [TestCase("...", true)]
        [TestCase(".,:", true)]
        [TestCase(" ", false)]
        [TestCase("abcd", false)]
        [TestCase("abcd ", false)]
        [TestCase("ab,.,cd", false)]
        public void isGoodTokenTruePunctuation(string token, bool isGood)
        {
            ChunkTextFilter filter = new ChunkTextFilter((uint)(token.Length+1), removePunctuation: false, removeWhitespaces: true);
            Assert.AreEqual(isGood, filter.IsGoodToken(token));
        }
        
        [TestCase(".", false)]
        [TestCase("...", false)]
        [TestCase(".,:", false)]
        [TestCase(" ", true)]
        [TestCase("\n", true)]
        [TestCase("  \n  ", true)]
        [TestCase(" \n \n  ", true)]
        [TestCase("abcd", false)]
        [TestCase("abcd ", false)]
        [TestCase("ab,.,cd", false)]
        [TestCase("ab,  \n.  ,cd", false)]
        public void isGoodTokenWhitespace(string token, bool isGood)
        {
            ChunkTextFilter filter = new ChunkTextFilter((uint)(token.Length+1), removePunctuation: true, removeWhitespaces: false);
            Assert.AreEqual(isGood, filter.IsGoodToken(token));
        }

        [TestCase(".", false)]
        [TestCase("...", false)]
        [TestCase(".,:", false)]
        [TestCase(" ", false)]
        [TestCase("\n", false)]
        [TestCase("  \n  ", false)]
        [TestCase(" \n \n  ", false)]
        [TestCase("abcd", false)]
        [TestCase("abcd ", false)]
        [TestCase("ab,.,cd", false)]
        [TestCase("ab,  \n.  ,cd", false)]
        public void isGoodTokenWordIsTooShort(string token, bool isGood)
        {
            ChunkTextFilter filter = new ChunkTextFilter((uint)(token.Length+1), removePunctuation: true, removeWhitespaces: true);
            Assert.AreEqual(isGood, filter.IsGoodToken(token));
        }
        
        [TestCase(".", false)]
        [TestCase("...", false)]
        [TestCase(".,:", false)]
        [TestCase(" ", false)]
        [TestCase("\n", false)]
        [TestCase("  \n  ", false)]
        [TestCase(" \n \n  ", false)]
        [TestCase("abcd", true)]
        [TestCase("abcd ", true)] // not a 'clean' token - bad, but we do not filter it by definition, depends on specifics
        [TestCase("ab,.,cd", true)]
        public void isGoodTokenWordIsLongEnough(string token, bool isGood)
        {
            ChunkTextFilter filter = new ChunkTextFilter((uint)token.Length, removePunctuation: true, removeWhitespaces: true);
            Assert.AreEqual(isGood, filter.IsGoodToken(token));
        }


        [TestCase("")]
        [TestCase("OneWord")]
        [TestCase("Two words")]
        [TestCase("Three words case")]
        [TestCase("Multirow\ntest\ncase")]
        public void filterNothing(string str)
        {
            ChunkTextFilter filter = new ChunkTextFilter(0, removePunctuation: false, removeWhitespaces: false);
            Assert.AreEqual(str, filter.Filter(str) + filter.LastToken);
        }

        [TestCase("")]
        [TestCase("OneWord")]
        [TestCase("Two words")]
        [TestCase("Three words case")]
        [TestCase("Multirow\ntest\ncase")]
        public void filterByWordlengthRemoveAll(string str)
        {
            ChunkTextFilter filter =
                new ChunkTextFilter((uint)str.Length, removePunctuation: false, removeWhitespaces: false);
            var filtered = string.Join("", Regex.Split(str, @"\b").Where(word => filter.IsGoodToken(word)));
            Assert.AreEqual(filtered, filter.Filter(str) +
                                      ((filter.IsGoodToken(filter.LastToken) ? filter.LastToken : "")));

        }

        [TestCase("")]
        [TestCase("OneWord")]
        [TestCase("Two words")]
        [TestCase("Three words case")]
        public void filterByWordlengthRemoveSome(string str)
        {
            var length = 4u;
            ChunkTextFilter filter = new ChunkTextFilter(length, removePunctuation: false, removeWhitespaces: false);
            var filtered = string.Join("", Regex.Split(str, @"\b").Where(word => filter.IsGoodToken(word)));
            Assert.AreEqual(filtered, filter.Filter(str) +
                                      ((filter.IsGoodToken(filter.LastToken) ? filter.LastToken : "")));
        }

        [TestCase("")]
        [TestCase("OneWord")]
        [TestCase("Two words")]
        [TestCase("Three words case")]
        [TestCase("Multirow\ntest\ncase")]
        public void filterPunctuationAndWhitespaces(string str)
        {
            ChunkTextFilter filter = new ChunkTextFilter(0, removePunctuation: true, removeWhitespaces: true);
            var filtered = Regex.Replace(str, @"\s|\p{P}", "");
            Assert.AreEqual(filtered, filter.Filter(str) +
                                      ((filter.IsGoodToken(filter.LastToken) ? filter.LastToken : "")));
        }

        [TestCase("")]
        [TestCase("aVeryLongWord")]
        [TestCase("Two")]
        [TestCase("Three words case")]
        [TestCase("Multirow\ntest\ncase")]
        public void filterByWordlengthAndSpaces(string str)
        {
            var length = 4u;
            ChunkTextFilter filter = new ChunkTextFilter(length, removePunctuation: false, removeWhitespaces: true);
            var filtered =
                Regex.Replace(string.Join("", Regex.Split(str, @"\b").Where(word => filter.IsGoodToken(word))),
                    @"\p{P}", "");

            Assert.AreEqual(filtered, filter.Filter(str) +
                                      ((filter.IsGoodToken(filter.LastToken) ? filter.LastToken : "")));
        }

        [TestCase("")]
        [TestCase("aVeryLongWord")]
        [TestCase("Two")]
        [TestCase("Three words case")]
        [TestCase("Multirow\ntest\ncase")]
        public void filterByWordlengthAndPunctuation(string str)
        {
            var length = 4u;
            ChunkTextFilter filter = new ChunkTextFilter(length, removePunctuation: false, removeWhitespaces: true);
            var filtered =
                Regex.Replace(string.Join("", Regex.Split(str, @"\b").Where(word => filter.IsGoodToken(word))),
                    @"\p{P}", "");
            Assert.AreEqual(filtered, filter.Filter(str) +
                                      ((filter.IsGoodToken(filter.LastToken) ? filter.LastToken : "")));
        }



    }

    class TestChunkTextProcessor
    {
        [TestCase("")]
        [TestCase("aVeryLongWord")]
        [TestCase("Two")]
        [TestCase("Three words case")]
        [TestCase("Multirow\ntest\ncase")]
        public void textProcessorFilterDefaultChunkSizeNothing(string str)
        {
            using StringReader SR = new StringReader(str);
            using StringWriter SW = new StringWriter();

            var minLength = 4u;
            var removePunctuation = false;
            var removeWhitespaces = false;

            var textProcessor =
                new ChunkTextProcessor(new ChunkTextFilter(minLength, removePunctuation, removeWhitespaces));
            textProcessor.ProcessText(SR, SW);
            var filteredResult = SW.ToString();
            var filter = new ChunkTextFilter(minLength, false, false);

            var filtered =
                Regex.Replace(string.Join("", Regex.Split(str, @"\b").Where(word => filter.IsGoodToken(word))),
                    @"\p{P}", "");
            Assert.AreEqual(filtered, filteredResult);
        }

        [TestCase("")]
        [TestCase("aVeryLongWord")]
        [TestCase("Two")]
        [TestCase("Three words case")]
        [TestCase("Multirow\ntest\ncase")]
        public void textProcessorFilterVerySmallChunk(string str)
        {
            using StringReader SR = new StringReader(str);
            using StringWriter SW = new StringWriter();

            var minLength = 4u;
            var removePunctuatuion = false;
            var removeWhitespaces = false;
            var chunkSize = 1u;


            var textProcessor = new ChunkTextProcessor(
                new ChunkTextFilter(minLength, removePunctuatuion, removeWhitespaces),
                chunkSize);
            textProcessor.ProcessText(SR, SW);
            var filteredResult = SW.ToString();
            var filter = new ChunkTextFilter(minLength, false, false);

            var filtered =
                Regex.Replace(string.Join("", Regex.Split(str, @"\b").Where(word => filter.IsGoodToken(word))),
                    @"\p{P}", "");
            Assert.AreEqual(filtered, filteredResult);
        }
    }
}