using System.Text.RegularExpressions;
using TextProcessorCoreLib;

namespace TextProcessorTest
{
    public class Tests
    {
        [TestCase("")]
        [TestCase("OneWord")]
        [TestCase("Two words")]
        [TestCase("Three words case")]
        [TestCase("Multirow\ntest\ncase")]
        public void filterNothing(string str)
        {
            TextFilter filter = new TextFilter(0, removePunctuation: false, removeWhitespaces: false);
            Assert.AreEqual(str, filter.Filter(str)+filter.LastToken);
        }
        
        [TestCase("")]
        [TestCase("OneWord")]
        [TestCase("Two words")]
        [TestCase("Three words case")]
        [TestCase("Multirow\ntest\ncase")]
        public void filterByWordlengthRemoveAll(string str)
        {
            TextFilter filter = new TextFilter((uint)str.Length, removePunctuation: false, removeWhitespaces: false);
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
            TextFilter filter = new TextFilter(length, removePunctuation: false, removeWhitespaces: false);
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
            TextFilter filter = new TextFilter(0, removePunctuation: true, removeWhitespaces: true);
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
            TextFilter filter = new TextFilter(length, removePunctuation: false, removeWhitespaces: true);
            var filtered =
                Regex.Replace(string.Join("", Regex.Split(str, @"\b").Where(word => filter.IsGoodToken(word))),
                    @"\p{P}", "");
            
            Assert.AreEqual(filtered, filter.Filter(str)+
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
            TextFilter filter = new TextFilter(length, removePunctuation: false, removeWhitespaces: true);
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
        public void textProcessorFilterDefaultChunkSizeNothing(string str)
        {
            using StringReader SR = new StringReader(str);
            using StringWriter SW = new StringWriter();

            var minLength = 4u;
            var removePunctuatuion = false;
            var removeWhitespaces = false;
            
            var textProcessor = new ChunkTextProcessor(new TextFilter(minLength, removePunctuatuion, removeWhitespaces));
            textProcessor.ProcessText(SR, SW);
            var filteredResult = SW.ToString();
            var filter = new TextFilter(minLength, false, false);

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


            var textProcessor = new ChunkTextProcessor(new TextFilter(minLength, removePunctuatuion, removeWhitespaces),
                chunkSize);
            textProcessor.ProcessText(SR, SW);
            var filteredResult = SW.ToString();
            var filter = new TextFilter(minLength, false, false);

            var filtered =
                Regex.Replace(string.Join("", Regex.Split(str, @"\b").Where(word => filter.IsGoodToken(word))),
                    @"\p{P}", "");
            Assert.AreEqual(filtered, filteredResult);
        }
    }
}