using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace LevelDB.NET.Tests
{
    [TestFixture]
    public class LevelDBTests
    {
        DB Database { get; set; }
        string DatabasePath { get; set; }

        [SetUp]
        public void SetUp()
        {
            var tempPath = Path.GetTempPath();
            var randName = Path.GetRandomFileName();
            DatabasePath = Path.Combine(tempPath, randName);
            var options = new Options()
            {
                CreateIfMissing = true
            };
            Database = new DB(DatabasePath, options);
        }

        [TearDown]
        public void TearDown()
        {
            // some test-cases tear-down them self
            if (Database != null)
            {
                Database.Dispose();
            }
            if (Directory.Exists(DatabasePath))
            {
                Directory.Delete(DatabasePath, true);
            }
        }


        [Test]
        public void Close()
        {
            // test double close
            Database.Dispose();
        }

        [Test]
        public void DisposeChecks()
        {
            Database.Dispose();
            Assert.That(() => Database.Get("key1"), Throws.TypeOf<ObjectDisposedException>());
        }

        [Test]
        public void TestCRUD()
        {
            Database.Put("Tampa", "green");
            Database.Put("London", "red");
            Database.Put("New York", "blue");

            Assert.AreEqual(Database.Get("Tampa"), "green");
            Assert.AreEqual(Database.Get("London"), "red");
            Assert.AreEqual(Database.Get("New York"), "blue");

            Database.Delete("New York");

            Assert.IsNull(Database.Get("New York"));

            Database.Delete("New York");
        }

        [Test]
        public void TestRepair()
        {
            TestCRUD();
            DB.Repair(DatabasePath, new Options());
        }

        [Test]
        public void TestIterator()
        {
            Database.Put("Tampa", "green");
            Database.Put("London", "red");
            Database.Put("New York", "blue");

            var expected = new[] { "London", "New York", "Tampa" };
            
            var actual = new List<string>();
            using (var iterator = Database.CreateIterator(new ReadOptions()))
            {
                iterator.SeekToFirst();
                while (iterator.Valid())
                {
                    var key = iterator.StringKey();
                    actual.Add(key);
                    iterator.Next();
                }
            }

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void TestEnumerable()
        {
            Database.Put("Tampa", "green");
            Database.Put("London", "red");
            Database.Put("New York", "blue");

            var expected = new[] { "London", "New York", "Tampa" };
            var actual = from kv in Database as IEnumerable<KeyValuePair<string, string>>
                           select kv.Key;

            CollectionAssert.AreEqual(expected, actual.ToArray());
        }

        [Test]
        public void TestSnapshot()
        {
            Database.Put("Tampa", "green");
            Database.Put("London", "red");
            Database.Delete("New York");

            using (var snapShot = Database.CreateSnapshot())
            {
                var readOptions = new ReadOptions { Snapshot = snapShot };

            Database.Put("New York", "blue");

                Assert.AreEqual(Database.Get("Tampa", readOptions), "green");
                Assert.AreEqual(Database.Get("London", readOptions), "red");

                // Snapshot taken before key was updates
                Assert.IsNull(Database.Get("New York", readOptions));
            }

            // can see the change now
            Assert.AreEqual(Database.Get("New York"), "blue");
        }

        [Test]
        public void TestGetProperty()
        {
            var r = new Random(0);
            var data = "";
            for (var i = 0; i < 1024; i++)
            {
                data += 'a' + r.Next(26);
            }

            for (int i = 0; i < 5 * 1024; i++)
            {
                Database.Put(string.Format("row{0}", i), data);
            }

            var stats = Database.PropertyValue("leveldb.stats");

            Console.WriteLine(stats);

            Assert.IsNotNull(stats);
            Assert.IsTrue(stats.Contains("Compactions"));
        }

        [Test]
        public void TestWriteBatch()
        {
            Database.Put("NA", "Na");

            using (var batch = new WriteBatch())
            {
                batch.Delete("NA")
                     .Put("Tampa", "Green")
                     .Put("London", "red")
                     .Put("New York", "blue");
                Database.Write(batch);
            }

            var expected = new[] { "London", "New York", "Tampa" };
            var actual = from kv in Database as IEnumerable<KeyValuePair<string, string>>
                         select kv.Key;

            CollectionAssert.AreEqual(expected, actual.ToArray());
        }

        [Test]
        public void IsValid()
        {
            Database.Put("key1", "value1");

            var iter = Database.CreateIterator();
            iter.SeekToLast();
            Assert.IsTrue(iter.IsValid);

            iter.Next();
            Assert.IsFalse(iter.IsValid);
        }

    }
}