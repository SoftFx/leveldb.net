[![MSBuild](https://github.com/SoftFx/leveldb.net/actions/workflows/msbuild.yml/badge.svg)](https://github.com/SoftFx/leveldb.net/actions/workflows/msbuild.yml)
[![.NET](https://github.com/SoftFx/leveldb.net/actions/workflows/dotnet.yml/badge.svg)](https://github.com/SoftFx/leveldb.net/actions/workflows/dotnet.yml)
[![NuGet](https://img.shields.io/nuget/v/SoftFX.LevelDB.Standard.svg)](https://www.nuget.org/packages/SoftFX.LevelDB.Standard)
# leveldb.net
Leveldb is a fast key-value storage library written at Google that provides an ordered mapping from string keys to string values.

This project is .net wrapper around windows port of leveldb taken from https://bitbucket.org/robertvazan/leveldb.net/src

# Installation #
LevelDB.Standard is available as a NuGet package:

```
PM> Install-Package SoftFx.LevelDB.Standard
```

# Getting Started #

Here's how you can get started with leveldb and .NET.

## Opening A Database ##

A Leveldb database has a name which corresponds to a directory on the system.  This then stores all files in this particular folder.  In this example, you can create a new database (if missing) in the C:\temp\tempdb directory.

```csharp
// Open a connection to a new DB and create if not found
var options = new Options { CreateIfMissing = true };
var db = new DB(options, @"C:\temp\tempdb");
```

## Closing a Database ##

When you are finished, you can close the database by calling the Dispose method.

```csharp
// Close the connection
db.Dispose();
```

The DB class also implements the IDisposable interface which allows you to use the using block:

```csharp
var options = new Options { CreateIfMissing = true };
using (var db = new DB(options, @"C:\temp\tempdb")) 
{
    // Use leveldb
}
```

## Reads and Writes ##

leveldb provides the Get, Put and Delete methods to query, update and delete database objects.

```csharp
const string key = "New York";

// Put in the key value
keyValue.Put(key, "blue");

// Print out the value
var keyValue = db.Get(key);
Console.WriteLine(keyValue); 

// Delete the key
db.Delete(key);
```

## Atomic Updates ##

leveldb also supports atomic updates through the WriteBatch class and the Write method on the DB.  This ensures atomic updates should a process exit abnormally.

```csharp
var options = new Options { CreateIfMissing = true };
using (var db = new DB(path, options))
{
    db.Put("NA", "Na");

    using(var batch = new WriteBatch())
    {
        batch.Delete("NA")
             .Put("Tampa", "Green")
             .Put("London", "red")
             .Put("New York", "blue");
        db.Write(batch);
    }
}
```

## Synchronous Writes ##

For performance reasons, by default, every write to leveldb is asynchronous.  This behavior can be changed by providing a WriteOptions class with the Sync flag set to true to a Put method call on the DB instance.

```csharp
// Synchronously write
var writeOptions = new WriteOptions { Sync = true };
db.Put("New York", "blue");
```

The downside of this is that due to a process crash, these updates may be lost.  

As an alternative, atomic updates can be used as a safer alternative with a synchronous write which the cost will be amortized across all of the writes in the batch.

```csharp
var options = new Options { CreateIfMissing = true };
using (var db = new DB(options, path))
{
	db.Put("New York", "blue");

	// Create a batch to set key2 and delete key1
	using (var batch = new WriteBatch())
	{
		var keyValue = db.Get("New York");
		batch.Put("Tampa", keyValue);
		batch.Delete("New York");
		
		// Write the batch
		var writeOptions = new WriteOptions { Sync = true; }
		db.Write(batch, writeOptions);
	}
}
```

## Iteration ##

The leveldb bindings also supports iteration using the standard GetEnumerator pattern.  In this example, we can select all keys in a LINQ expression and then iterate the results, printing out each key.

```csharp
var keys = 
    from kv in db as IEnumerable<KeyValuePair<string, string>>
    select kv.Key;

foreach (var key in keys) 
{
	Console.WriteLine("Key: {0}", key);
}
```

The following example shows how you can iterate all the keys as strings.

```csharp
// Create new iterator
using (var iterator = db.CreateIterator())
{
	// Iterate to print the keys as strings
	for (it.SeekToFirst(); it.IsValid; it.Next()) 
	{
	    Console.WriteLine("Key as string: {0}", it.KeyAsString());
	}
}
```

The next example shows how you can iterate all the values in the leveldb instance in reverse.

```csharp
// Create new iterator
using (var iterator = db.CreateIterator())
{
	// Iterate in reverse to print the values as strings
	for (it.SeekToLast(); it.Valid(); it.Prev()) 
	{
	    Console.WriteLine("Value as string: {0}", it.ValueAsString());
	}
}
```

## Snapshots ##

Snapshots in leveldb provide a consistent read-only view of the entire state of the current key-value store.  Note that the Snapshot implements IDisposable and should be disposed to allow leveldb to get rid of state that was being maintained just to support reading as of that snapshot. 

```csharp
var options = new Options { CreateIfMissing = true }
using (var db = new Db(path, options))
{
    db.Put("Tampa", "green");
    db.Put("London", "red");
    db.Delete("New York");

	using (var snapshot = db.CreateSnapshot()) 
	{
		var readOptions = new ReadOptions {Snapshot = snapShot};

		db.Put("New York", "blue");

		// Will return null as the snapshot created before
		// the updates happened
		Console.WriteLine(db.Get("New York", readOptions)); 
	}
}
```

