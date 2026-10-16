using System.Collections.Generic;
using NUnit.Framework;
using SixtyOne.Text;

namespace SixtyOne.Tests
{
    public class CsvParserTests
    {
        private static List<List<string>> Parse(string csv) => CsvParser.Parse(csv);

        [Test]
        public void SimpleRows()
        {
            var rows = Parse("key,text,notes\na,b,c\n");
            Assert.AreEqual(2, rows.Count);
            CollectionAssert.AreEqual(new[] { "a", "b", "c" }, rows[1]);
        }

        [Test]
        public void QuotedCommaStaysInField()
        {
            var rows = Parse("k,\"Merhaba, dünya\",n");
            CollectionAssert.AreEqual(new[] { "k", "Merhaba, dünya", "n" }, rows[0]);
        }

        [Test]
        public void EscapedQuotes()
        {
            var rows = Parse("k,\"O \"\"dedi\"\" ki\",");
            Assert.AreEqual("O \"dedi\" ki", rows[0][1]);
            Assert.AreEqual(string.Empty, rows[0][2]);
        }

        [Test]
        public void NewlineInsideQuotes()
        {
            var rows = Parse("k,\"satır 1\nsatır 2\",x\nk2,y,z");
            Assert.AreEqual(2, rows.Count);
            Assert.AreEqual("satır 1\nsatır 2", rows[0][1]);
            Assert.AreEqual("k2", rows[1][0]);
        }

        [Test]
        public void CrLfLineEndings()
        {
            var rows = Parse("a,b\r\nc,d\r\n");
            Assert.AreEqual(2, rows.Count);
            CollectionAssert.AreEqual(new[] { "c", "d" }, rows[1]);
        }

        [Test]
        public void Utf8BomIsIgnored()
        {
            var rows = Parse("﻿key,text\nç,ğüşıöİ");
            Assert.AreEqual("key", rows[0][0]);
            Assert.AreEqual("ğüşıöİ", rows[1][1]);
        }

        [Test]
        public void BlankLinesAreSkipped()
        {
            var rows = Parse("a,b\n\n\nc,d");
            Assert.AreEqual(2, rows.Count);
        }

        [Test]
        public void EmptyQuotedFieldIsKept()
        {
            var rows = Parse("\"\"");
            Assert.AreEqual(1, rows.Count);
            Assert.AreEqual(string.Empty, rows[0][0]);
        }

        [Test]
        public void NoTrailingNewline()
        {
            var rows = Parse("a,b");
            Assert.AreEqual(1, rows.Count);
            CollectionAssert.AreEqual(new[] { "a", "b" }, rows[0]);
        }

        [Test]
        public void EmptyInput()
        {
            Assert.AreEqual(0, Parse(string.Empty).Count);
            Assert.AreEqual(0, Parse(null).Count);
        }

        [Test]
        public void EscapeRoundTrips()
        {
            string value = "a, \"b\"\nc";
            var rows = Parse("k," + CsvParser.Escape(value));
            Assert.AreEqual(value, rows[0][1]);
            Assert.AreEqual("plain", CsvParser.Escape("plain"));
        }

        [Test]
        public void TextDbMissingKeyRendersBrackets()
        {
            try
            {
                CheckTextDb();
            }
            finally
            {
                // Restore the real CSV so editor previews are not left with test data.
                if (System.IO.File.Exists(TextDB.DefaultCsvPath))
                {
                    TextDB.LoadFromCsv(System.IO.File.ReadAllText(TextDB.DefaultCsvPath));
                }
            }
        }

        private static void CheckTextDb()
        {
            TextDB.LoadFromCsv("key,text,notes\nhello,Merhaba,\nempty,,");
            Assert.AreEqual("Merhaba", TextDB.Get("hello"));
            Assert.AreEqual("[[nope]]", TextDB.Get("nope"));
            Assert.AreEqual("[[empty]]", TextDB.Get("empty"));
            Assert.IsTrue(TextDB.HasKey("empty"));
            StringAssert.Contains("FF00FF", TextDB.GetDisplay("nope"));
            TextDB.LoadFromCsv("key,text,notes\nfmt,LOOP #{0},");
            Assert.AreEqual("LOOP #3", TextDB.Format("fmt", 3));
        }
    }
}
