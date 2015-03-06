using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PSPassCustomTypes;
using System.Text.RegularExpressions;

namespace PSPassTestProject
{
    [TestClass]
    public class PathConverterUnitTest
    {
        [TestMethod]
        public void EncodeCharTestMethod()
        {
            char target = '\0';
            string expected = "_0x00_";
            string actual = PathConverter.EncodeChar(target);
            Assert.IsNotNull(actual);
            Assert.AreEqual(expected, actual);

            target = ' ';
            expected = "_0x20_";
            actual = PathConverter.EncodeChar(target);
            Assert.IsNotNull(actual);
            Assert.AreEqual(expected, actual);

            target = 'x';
            expected = "_0x78_";
            actual = PathConverter.EncodeChar(target);
            Assert.IsNotNull(actual);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void EscapeSequenceRegexTestMethod1()
        {
            string target = "";
            bool expected = false;
            bool actual = PathConverter.EscapeSequenceRegex.IsMatch(target);
            Assert.AreEqual(expected, actual);

            target = "_0x0g_";
            actual = PathConverter.EscapeSequenceRegex.IsMatch(target);
            Assert.AreEqual(expected, actual);

            target = "_0x00f_";
            actual = PathConverter.EscapeSequenceRegex.IsMatch(target);
            Assert.AreEqual(expected, actual);

            target = "_0xx0f_";
            actual = PathConverter.EscapeSequenceRegex.IsMatch(target);
            Assert.AreEqual(expected, actual);

            target = "_00x0f_";
            actual = PathConverter.EscapeSequenceRegex.IsMatch(target);
            Assert.AreEqual(expected, actual);

            target = "_0x0_";
            actual = PathConverter.EscapeSequenceRegex.IsMatch(target);
            Assert.AreEqual(expected, actual);

            expected = true;
            target = "_0x0f_";
            actual = PathConverter.EscapeSequenceRegex.IsMatch(target);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void EscapeSequenceRegexTestMethod2()
        {
            string target = "_0_0x78_0x0f_";
            int[] expected = new int[] { 2 };
            int[] actual = PathConverter.EscapeSequenceRegex.Matches(target).OfType<Match>().Select(m => m.Index).ToArray();
            Assert.AreEqual(expected.Length, actual.Length);
            for (int i = 0; i < expected.Length; i++)
                Assert.AreEqual(expected[i], actual[i]); target = "_0_0x78_0f_0x0f_";

            target = "_0_0x78_0f_0x0f_";
            expected = new int[] { 2, 10 };
            actual = PathConverter.EscapeSequenceRegex.Matches(target).OfType<Match>().Select(m => m.Index).ToArray();
            Assert.AreEqual(expected.Length, actual.Length);
            for (int i = 0; i < expected.Length; i++)
                Assert.AreEqual(expected[i], actual[i]);
        }

        public class WhiteSpaceProperties
        {
            public int Index { get; set; }

            public int Length { get; set; }

            public bool StartsWithWs { get; set; }

            public bool EndsWithWs { get; set; }

            public WhiteSpaceProperties() { }

            public WhiteSpaceProperties(Match m)
            {
                this.Index = m.Index;
                this.Length = m.Length;
                this.StartsWithWs = Char.IsWhiteSpace(m.Value[0]);
                this.EndsWithWs = Char.IsWhiteSpace(m.Value.Last());
            }

            public static WhiteSpaceProperties[] Create(MatchCollection matches)
            {
                return matches.OfType<Match>().Select(m => new WhiteSpaceProperties(m)).ToArray();
            }

            public void AssertAreEqual(WhiteSpaceProperties other)
            {
                Assert.AreEqual(this.Index, other.Index);
                Assert.AreEqual(this.Length, other.Length);
                Assert.AreEqual(this.StartsWithWs, other.StartsWithWs);
                Assert.AreEqual(this.EndsWithWs, other.EndsWithWs);
            }
        }

        [TestMethod]
        public void WhiteSpacePathRegexTestMethod()
        {
            string[] target = new string[] { "", " ", "\r\n", "\\ ", " / ", " / asdf / ", " \\/ "};
            foreach (string t in target)
            {
                bool actual = PathConverter.WhiteSpacePathRegex.IsMatch(t);
                Assert.IsFalse(actual);
            }

            var testData = new[]
            {
                new { Target = "/ /", Index = 0, Length = 2 }, 
                new { Target = " \\ \n/ ", Index = 1, Length = 3 },
                new { Target = "one / \r \\", Index = 4, Length = 4 }
            };
            foreach (var t in testData)
            {
                Match actual = PathConverter.WhiteSpacePathRegex.Match(t.Target);
                Assert.IsTrue(actual.Success);
                Assert.AreEqual(t.Index, actual.Index);
                Assert.AreEqual(t.Length, actual.Length);
            }
        }

        [TestMethod]
        public void NormalizeUserPathTestMethod()
        {
            string[] target = new string[] { null, "", " ", "\r\n" };
            string expected = "/";
            foreach (string t in target)
            {
                string actual = PathConverter.NormalizeUserPath(t);
                Assert.IsNotNull(actual);
                Assert.AreEqual(expected, actual);
            }

            expected = "/one";
            target = new string[] { "one", " one", "one  " };
            foreach (string t in target)
            {
                string actual = PathConverter.NormalizeUserPath(t);
                Assert.IsNotNull(actual);
                Assert.AreEqual(expected, actual);
            }

            expected = "/one two";
            target = new string[] { "one two", " one two", "one   two  ", "\tone   two  " };
            foreach (string t in target)
            {
                string actual = PathConverter.NormalizeUserPath(t);
                Assert.IsNotNull(actual);
                Assert.AreEqual(expected, actual);
            }

            expected = "/one/ /two";
            target = new string[] { "one// /two", " one/\\ /two", "one/ \r\n\t/two  ", "\tone/ /two  " };
            foreach (string t in target)
            {
                string actual = PathConverter.NormalizeUserPath(t);
                Assert.IsNotNull(actual);
                Assert.AreEqual(expected, actual);
            }

            target = new string[] { "one / / two", " one\n/\\ /\ttwo", "one\r/ \r\n\t/ two  ", "\tone / / two  " };
            foreach (string t in target)
            {
                string actual = PathConverter.NormalizeUserPath(t);
                Assert.IsNotNull(actual);
                Assert.AreEqual(expected, actual);
            }

            expected = "/one/two";
            target = new string[] { "one////two", " one\\/\\two", "one////\\\\//two  ", "\tone\\/\\/\\two  " };
            foreach (string t in target)
            {
                string actual = PathConverter.NormalizeUserPath(t);
                Assert.IsNotNull(actual);
                Assert.AreEqual(expected, actual);
            }

            target = new string[] { "/one////two", " \\one\\/\\two", "//\\one////\\\\//two  ", "\t//////one\\/\\/\\two  " };
            foreach (string t in target)
            {
                string actual = PathConverter.NormalizeUserPath(t);
                Assert.IsNotNull(actual);
                Assert.AreEqual(expected, actual);
            }

            target = new string[] { "one////two\\", " one\\/\\two\\\n", "one////\\\\//two/  ", "\tone\\/\\/\\two///\\\\\\  " };
            foreach (string t in target)
            {
                string actual = PathConverter.NormalizeUserPath(t);
                Assert.IsNotNull(actual);
                Assert.AreEqual(expected, actual);
            }
        }

        [TestMethod]
        public void ItemNameToFileNameTestMethod()
        {
            string[] target = new string[] { null, "", " ", "\n " };
            string expected = "_0x20_.xml";
            foreach (string t in target)
            {
                string actual = PathConverter.ItemNameToFileName(t);
                Assert.IsNotNull(actual);
                Assert.AreEqual(expected, actual);
            }

            target = new string[] { "test", " test", "test ", "\n test\t " };
            expected = "test.xml";
            foreach (string t in target)
            {
                string actual = PathConverter.ItemNameToFileName(t);
                Assert.IsNotNull(actual);
                Assert.AreEqual(expected, actual);
            }

            target = new string[] { "test\\", " test\\", "test\\ ", "\n test\\\t " };
            expected = "test_0x5c_.xml";
            foreach (string t in target)
            {
                string actual = PathConverter.ItemNameToFileName(t);
                Assert.IsNotNull(actual);
                Assert.AreEqual(expected, actual);
            }

            target = new string[] { "/test", " /test", "/test ", "\n /test\t " };
            expected = "_0x2f_test.xml";
            foreach (string t in target)
            {
                string actual = PathConverter.ItemNameToFileName(t);
                Assert.IsNotNull(actual);
                Assert.AreEqual(expected, actual);
            }

            target = new string[] { "test \\", " test\r\n\\", "test \\ ", "\n test \\\t " };
            expected = "test _0x5c_.xml";
            foreach (string t in target)
            {
                string actual = PathConverter.ItemNameToFileName(t);
                Assert.IsNotNull(actual);
                Assert.AreEqual(expected, actual);
            }

            target = new string[] { "test _0x78_\\\\", " test\r\n_0x78_\\\\", "test _0x78_\\\\ ", "\n test _0x78_\\\\\t " };
            expected = "test _0_0x78_78__0x5c__0x5c_.xml";
            foreach (string t in target)
            {
                string actual = PathConverter.ItemNameToFileName(t);
                Assert.IsNotNull(actual);
                Assert.AreEqual(expected, actual);
            }
        }

        [TestMethod]
        public void FileNameToItemNameTestMethod()
        {
            string target = "_0x20_.xml";
            string expected = "";
            string actual = PathConverter.FileNameToItemName(target);
            Assert.IsNotNull(actual);
            Assert.AreEqual(expected, actual);

            target = "test.xml";
            expected = "test";
            actual = PathConverter.FileNameToItemName(target);
            Assert.IsNotNull(actual);
            Assert.AreEqual(expected, actual);

            target = "_0x5c_test.xml";
            expected = "\\test";
            actual = PathConverter.FileNameToItemName(target);
            Assert.IsNotNull(actual);
            Assert.AreEqual(expected, actual);

            target = "test_0x2f_.xml";
            expected = "test/";
            actual = PathConverter.FileNameToItemName(target);
            Assert.IsNotNull(actual);
            Assert.AreEqual(expected, actual);

            target = "_0x5c_ test.xml";
            expected = "\\ test";
            actual = PathConverter.FileNameToItemName(target);
            Assert.IsNotNull(actual);
            Assert.AreEqual(expected, actual);

            target = "test _0x2f_.xml";
            expected = "test /";
            actual = PathConverter.FileNameToItemName(target);
            Assert.IsNotNull(actual);
            Assert.AreEqual(expected, actual);

            target = "test _0_0x78_78__0x5c_.xml";
            expected = "test _0x78_\\";
            actual = PathConverter.FileNameToItemName(target);
            Assert.IsNotNull(actual);
            Assert.AreEqual(expected, actual);

            target = "test _0_0x78_78__0x5c__0x5c_.xml";
            expected = "test _0x78_\\\\";
            actual = PathConverter.FileNameToItemName(target);
            Assert.IsNotNull(actual);
            Assert.AreEqual(expected, actual);

            target = "test _0_0x78_78__0x5c_0x2f_.xml";
            expected = "test _0x78_\\0x2f_";
            actual = PathConverter.FileNameToItemName(target);
            Assert.IsNotNull(actual);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void FolderPathToDirectoryNameTestMethod()
        {
            string[] target = new string[] { null, "", " ", "\\", "/", " \\ ", " /// " };
            string expected = "/";
            foreach (string t in target)
            {
                string actual = PathConverter.FolderPathToDirectoryName(t);
                Assert.IsNotNull(actual);
                Assert.AreEqual(expected, actual);
            }

            target = new string[] { "<one/|", "<one/| ", "<one/\\|", "/<one/|", " \\<one\\| ", " //<one/|/ " };
            expected = "/_0x3c_one/_0x7c_";
            foreach (string t in target)
            {
                string actual = PathConverter.FolderPathToDirectoryName(t);
                Assert.IsNotNull(actual);
                Assert.AreEqual(expected, actual);
            }

            target = new string[] { "<one/|/_0_0x7_0_0x788_0x78__0x78", "<one/| /_0_0x7_0_0x788_0x78__0x78\\", "<one/\\|/_0_0x7_0_0x788_0x78__0x78", "/<one/|/_0_0x7_0_0x788_0x78__0x78 /// ", " \\<one\\| /_0_0x7_0_0x788_0x78__0x78", " //<one/|/ /_0_0x7_0_0x788_0x78__0x78" };
            expected = "/_0x3c_one/_0x7c_/_0_0x7_0_0x788_0_0x78_78__0x78";
            foreach (string t in target)
            {
                string actual = PathConverter.FolderPathToDirectoryName(t);
                Assert.IsNotNull(actual);
                Assert.AreEqual(expected, actual);
            }
        }

        [TestMethod]
        public void DirectoryNameToFolderPathTestMethod()
        {
            string target = "\\";
            string expected = "\\";
            string actual = PathConverter.DirectoryNameToFolderPath(target);
            Assert.IsNotNull(actual);
            Assert.AreEqual(expected, actual);

            target = "test";
            expected = "\\test";
            actual = PathConverter.DirectoryNameToFolderPath(target);
            Assert.IsNotNull(actual);
            Assert.AreEqual(expected, actual);

            target = "\\test";
            expected = "\\test";
            actual = PathConverter.DirectoryNameToFolderPath(target);
            Assert.IsNotNull(actual);
            Assert.AreEqual(expected, actual);

            target = "\\_0x3c_one\\_0x7c_";
            expected = "\\<one\\|";
            actual = PathConverter.DirectoryNameToFolderPath(target);
            Assert.IsNotNull(actual);
            Assert.AreEqual(expected, actual);

            target = "_0x3c_one\\_0x7c_";
            expected = "\\<one\\|";
            actual = PathConverter.DirectoryNameToFolderPath(target);
            Assert.IsNotNull(actual);
            Assert.AreEqual(expected, actual);

            target = "\\_0x3c_one\\_0x7c_\\_0_0x7_0_0x788_0_0x78_78__0x78";
            expected = "\\<one\\|\\_0_0x7_0_0x788_0x78__0x78";
            actual = PathConverter.DirectoryNameToFolderPath(target);
            Assert.IsNotNull(actual);
            Assert.AreEqual(expected, actual);

            target = "_0x3c_one\\_0x7c_\\_0_0x7_0_0x788_0_0x78_78__0x78\\";
            expected = "\\<one\\|\\_0_0x7_0_0x788_0x78__0x78";
            actual = PathConverter.DirectoryNameToFolderPath(target);
            Assert.IsNotNull(actual);
            Assert.AreEqual(expected, actual);
        }
    }
}
