using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SixtyOne.Text;

namespace SixtyOne.Tests
{
    /// <summary>strings_tr.csv must ship without placeholder text.</summary>
    public class LocalizationTests
    {
        [Test]
        public void StringsCsvHasNoTodo()
        {
            Assert.IsTrue(File.Exists(TextDB.DefaultCsvPath), "run from the project root: " + TextDB.DefaultCsvPath);
            string csv = File.ReadAllText(TextDB.DefaultCsvPath);
            List<string> rows = TextLint.TodoRows(csv);
            Assert.AreEqual(0, rows.Count, "rows with TODO:\n" + string.Join("\n", rows));
            Assert.Greater(CsvParser.Parse(csv).Count, 100, "the CSV was really parsed");
        }

        [Test]
        public void TodoScanIsCaseInsensitiveAndChecksEveryColumn()
        {
            Assert.AreEqual(1, TextLint.TodoRows("key,text,notes\na,todo: metin,\n").Count);
            Assert.AreEqual(1, TextLint.TodoRows("key,text,notes\na,metin,TODO - later\n").Count);
            Assert.AreEqual(0, TextLint.TodoRows("key,text,notes\na,Hazır metin,\n").Count);
        }
    }
}
