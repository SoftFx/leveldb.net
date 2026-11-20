using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LevelDB.NET.Tests
{
    // The Linux library shipped up to 1.2.1 was built without Snappy and silently ignored
    // CompressionLevel.SnappyCompression. These tests pin that compression works on every
    // platform the tests run on, and that a library still opens the databases 1.2.1 wrote:
    // uncompressed ones (Linux) and Snappy ones (Windows).
    [TestFixture]
    public class CompressionTests
    {
        const int FixtureRows = 2000;

        string DatabasePath { get; set; }

        static string FixturesPath => Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures");

        [SetUp]
        public void SetUp()
        {
            DatabasePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(DatabasePath))
                Directory.Delete(DatabasePath, true);
        }

        [Test]
        public void SnappyCompressionShrinksCompressibleData()
        {
            const long raw = 4096 * (sizeof(int) + 1024);

            var size = WriteZeros(DatabasePath, CompressionLevel.SnappyCompression);

            Assert.That(size, Is.LessThan(raw / 4), "Snappy was requested, but the table files were not compressed");
        }

        [Test]
        public void NoCompressionKeepsDataUncompressed()
        {
            const long raw = 4096 * (sizeof(int) + 1024);

            var size = WriteZeros(DatabasePath, CompressionLevel.NoCompression);

            Assert.That(size, Is.GreaterThanOrEqualTo(raw));
        }

        // Written on Linux by 1.2.1 (LevelDB 1.22 without Snappy): every block is uncompressed.
        // Written on Windows by 1.2.1 (LevelDB 1.23 with Snappy): blocks are Snappy-compressed,
        // which the old Linux library could not read ("corrupted compressed block contents").
        [TestCase("uncompressed-1.22-linux")]
        [TestCase("snappy-1.23-windows")]
        public void ReadsDatabaseWrittenByVersion121(string fixture)
        {
            CopyDirectory(Path.Combine(FixturesPath, fixture), DatabasePath);

            var rows = 0;
            using (var options = new Options())
            using (var db = new DB(DatabasePath, options))
            {
                foreach (var row in (IEnumerable<KeyValuePair<string, string>>)db)
                {
                    Assert.AreEqual(FixtureKey(rows), row.Key);
                    Assert.AreEqual(FixtureValue(rows), row.Value);
                    rows++;
                }
            }

            Assert.AreEqual(FixtureRows, rows);
        }

        // Guards the fixtures themselves: if the Snappy one were regenerated without compression,
        // the read test above would no longer cover compressed blocks.
        [Test]
        public void FixturesCoverBothBlockKinds()
        {
            var snappy = TableBytes(Path.Combine(FixturesPath, "snappy-1.23-windows"));
            var uncompressed = TableBytes(Path.Combine(FixturesPath, "uncompressed-1.22-linux"));

            Assert.That(snappy * 2, Is.LessThan(uncompressed));
        }

        // The rows the fixtures were written with.
        static string FixtureKey(int i) => $"key{i:D5}";

        static string FixtureValue(int i) => $"value {i % 7} " + new string((char)('a' + i % 26), 100) + $" {i}";

        static long WriteZeros(string path, CompressionLevel level)
        {
            using (var options = new Options { CreateIfMissing = true, CompressionLevel = level })
            using (var db = new DB(path, options))
            {
                var value = new byte[1024];
                for (var i = 0; i < 4096; i++)
                    db.Put(BitConverter.GetBytes(i), value);

                db.Compact();
            }

            return TableBytes(path);
        }

        static long TableBytes(string path) =>
            Directory.GetFiles(path, "*.ldb").Sum(f => new FileInfo(f).Length);

        static void CopyDirectory(string source, string target)
        {
            Directory.CreateDirectory(target);
            foreach (var file in Directory.GetFiles(source))
                File.Copy(file, Path.Combine(target, Path.GetFileName(file)));
        }
    }
}
