﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace LevelDB
{
    /// <summary>
    /// A DB is a persistent ordered map from keys to values.
    /// A DB is safe for concurrent access from multiple threads without any external synchronization.
    /// </summary>
    public class DB : LevelDBHandle, IEnumerable<KeyValuePair<string, string>>, IEnumerable<KeyValuePair<byte[], byte[]>>
    {
        Options Options;

        private readonly WriteOptions _writeOptions = new WriteOptions();
        private readonly ReadOptions  _readOptions  = new ReadOptions();

        /// <summary>
        /// Open the database with the specified "name".
        /// </summary>
        public DB(string name)
            : this(name, new Options())
        {
        }

        /// <summary>
        /// Open the database with the specified "name".
        /// Options should not be modified after calling this method.
        /// </summary>
        public DB(string name, Options options)
        {
            Options = options ?? new Options();
            IntPtr error;
            Handle = LevelDBInterop.leveldb_open(Options.Handle, name, out error);
            LevelDBException.Check(error);
            GC.KeepAlive(Options);
        }

        /// <summary>
        /// If a DB cannot be opened, you may attempt to call this method to
        /// resurrect as much of the contents of the database as possible.
        /// Some data may be lost, so be careful when calling this function
        /// on a database that contains important information.
        /// </summary>
        public static void Repair(string name)
        {
            Repair(name, new Options());
        }

        /// <summary>
        /// If a DB cannot be opened, you may attempt to call this method to
        /// resurrect as much of the contents of the database as possible.
        /// Some data may be lost, so be careful when calling this function
        /// on a database that contains important information.
        /// Options should not be modified after calling this method.
        /// </summary>
        public static void Repair(string name, Options options)
        {
            IntPtr error;
            LevelDBInterop.leveldb_repair_db(options.Handle, name, out error);
            LevelDBException.Check(error);
            GC.KeepAlive(options);
        }

        /// <summary>
        /// Destroy the contents of the specified database.
        /// Be very careful using this method.
        /// </summary>
        public static void Destroy(string name)
        {
            Destroy(name, new Options());
        }

        /// <summary>
        /// Destroy the contents of the specified database.
        /// Be very careful using this method.
        /// Options should not be modified after calling this method.
        /// </summary>
        public static void Destroy(string name, Options options)
        {
            IntPtr error;
            LevelDBInterop.leveldb_destroy_db(options.Handle, name, out error);
            LevelDBException.Check(error);
            GC.KeepAlive(options);
        }

        /// <summary>
        /// Set the database entry for "key" to "value".  
        /// </summary>
        public void Put(string key, string value)
        {
            Put(key, value, _writeOptions);
        }

        /// <summary>
        /// Set the database entry for "key" to "value".  
        /// </summary>
        public void Put(string key, string value, WriteOptions options)
        {
            Put(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(value), options);
        }

        /// <summary>
        /// Set the database entry for "key" to "value".  
        /// </summary>
        public void Put(byte[] key, byte[] value)
        {
            Put(key, value, _writeOptions);
        }

        /// <summary>
        /// Set the database entry for "key" to "value".  
        /// </summary>
        public void Put(byte[] key, byte[] value, WriteOptions options)
        {
            CheckDisposed();
            IntPtr error;
            LevelDBInterop.leveldb_put(this.Handle, options.Handle, key, (IntPtr)key.Length, value, (IntPtr)value.LongLength, out error);
            LevelDBException.Check(error);
            GC.KeepAlive(options);
            GC.KeepAlive(this);
        }

        /// <summary>
        /// Remove the database entry (if any) for "key".  
        /// It is not an error if "key" did not exist in the database.
        /// </summary>
        public void Delete(string key)
        {
            Delete(key, _writeOptions);
        }

        /// <summary>
        /// Remove the database entry (if any) for "key".  
        /// It is not an error if "key" did not exist in the database.
        /// </summary>
        public void Delete(string key, WriteOptions options)
        {
            Delete(Encoding.UTF8.GetBytes(key), options);
        }

        /// <summary>
        /// Remove the database entry (if any) for "key".  
        /// It is not an error if "key" did not exist in the database.
        /// </summary>
        public void Delete(byte[] key)
        {
            Delete(key, _writeOptions);
        }

        /// <summary>
        /// Remove the database entry (if any) for "key".  
        /// It is not an error if "key" did not exist in the database.
        /// </summary>
        public void Delete(byte[] key, WriteOptions options)
        {
            CheckDisposed();
            IntPtr error;
            LevelDBInterop.leveldb_delete(this.Handle, options.Handle, key, (IntPtr)key.Length, out error);
            LevelDBException.Check(error);
            GC.KeepAlive(options);
            GC.KeepAlive(this);
        }

        public void Write(WriteBatch batch)
        {
            Write(batch, _writeOptions);
        }

        public void Write(WriteBatch batch, WriteOptions options)
        {
            CheckDisposed();
            IntPtr error;
            LevelDBInterop.leveldb_write(this.Handle, options.Handle, batch.Handle, out error);
            LevelDBException.Check(error);
            GC.KeepAlive(batch);
            GC.KeepAlive(options);
            GC.KeepAlive(this);
        }

        /// <summary>
        /// If the database contains an entry for "key" return the value,
        /// otherwise return null.
        /// </summary>
        public string Get(string key)
        {
            return Get(key, _readOptions);
        }

        /// <summary>
        /// If the database contains an entry for "key" return the value,
        /// otherwise return null.
        /// </summary>
        public string Get(string key, ReadOptions options)
        {
            var value = Get(Encoding.UTF8.GetBytes(key), options);
            return value != null ? Encoding.UTF8.GetString(value) : null;
        }

        /// <summary>
        /// If the database contains an entry for "key" return the value,
        /// otherwise return null.
        /// </summary>
        public byte[] Get(byte[] key)
        {
            return Get(key, _readOptions);
        }

        /// <summary>
        /// If the database contains an entry for "key" return the value,
        /// otherwise return null.
        /// </summary>
        public unsafe byte[] Get(byte[] key, ReadOptions options)
        {
            CheckDisposed();
            IntPtr error;
            IntPtr lengthPtr;
            var valuePtr = LevelDBInterop.leveldb_get(this.Handle, options.Handle, key, (IntPtr)key.Length, out lengthPtr, out error);
            LevelDBException.Check(error);
            if (valuePtr == IntPtr.Zero)
                return null;
            try
            {
                var length = (long)lengthPtr;
                var value = new byte[length];
                var valueNative = (byte*)valuePtr.ToPointer();
                for (long i = 0; i < length; ++i)
                    value[i] = valueNative[i];
                return value;
            }
            finally
            {
                LevelDBInterop.leveldb_free(valuePtr);
                GC.KeepAlive(options);
                GC.KeepAlive(this);
            }
        }

        /// <summary>
        /// Return an iterator over the contents of the database.
        /// The result of CreateIterator is initially invalid (caller must
        /// call one of the Seek methods on the iterator before using it).
        /// </summary>
        public Iterator CreateIterator()
        {
            return this.CreateIterator(_readOptions);
        }

        /// <summary>
        /// Return an iterator over the contents of the database.
        /// The result of CreateIterator is initially invalid (caller must
        /// call one of the Seek methods on the iterator before using it).
        /// </summary>
        public Iterator CreateIterator(ReadOptions options)
        {
            CheckDisposed();
            var result = new Iterator(LevelDBInterop.leveldb_create_iterator(this.Handle, options.Handle));
            GC.KeepAlive(options);
            GC.KeepAlive(this);
            return result;
        }

        /// <summary>
        /// Return a handle to the current DB state.  
        /// Iterators and Gets created with this handle will all observe a stable snapshot of the current DB state.  
        /// </summary>
        public SnapShot CreateSnapshot()
        {
            CheckDisposed();
            var result = new SnapShot(LevelDBInterop.leveldb_create_snapshot(this.Handle), this);
            GC.KeepAlive(this);
            return result;
        }

        /// <summary>
        /// DB implementations can export properties about their state
        /// via this method.  If "property" is a valid property understood by this
        /// DB implementation, fills "*value" with its current value and returns
        /// true.  Otherwise returns false.
        ///
        /// Valid property names include:
        ///
        ///  "leveldb.num-files-at-level<N>" - return the number of files at level <N>,
        ///     where <N> is an ASCII representation of a level number (e.g. "0").
        ///  "leveldb.stats" - returns a multi-line string that describes statistics
        ///     about the internal operation of the DB.
        /// </summary>
        public string PropertyValue(string name)
        {
            CheckDisposed();
            var ptr = LevelDBInterop.leveldb_property_value(this.Handle, name);
            if (ptr == IntPtr.Zero)
                return null;
            try
            {
                return Marshal.PtrToStringAnsi(ptr);
            }
            finally
            {
                LevelDBInterop.leveldb_free(ptr);
                GC.KeepAlive(this);
            }
        }

        /// <summary>
        /// Compact the underlying storage.
        /// In particular, deleted and overwritten versions are discarded,
        /// and the data is rearranged to reduce the cost of operations
        /// needed to access the data.  This operation should typically only
        /// be invoked by users who understand the underlying implementation.
        /// </summary>
        /// <param name="startKey"></param>
        /// <param name="limitKey"></param>
        public void Compact()
        {
            byte[] startKey = null;
            byte[] limitKey = null;
            CompactRange(startKey, limitKey);
        }

        /// <summary>
        /// Compact the underlying storage for the key range [*begin,*end].
        /// In particular, deleted and overwritten versions are discarded,
        /// and the data is rearranged to reduce the cost of operations
        /// needed to access the data.  This operation should typically only
        /// be invoked by users who understand the underlying implementation.
        /// </summary>
        /// <param name="startKey"></param>
        /// <param name="limitKey"></param>
        public void CompactRange(string startKey, string limitKey)
        {
            CompactRange(Encoding.UTF8.GetBytes(startKey), Encoding.UTF8.GetBytes(limitKey));
        }

        /// <summary>
        /// Compact the underlying storage for the key range [*begin,*end].
        /// In particular, deleted and overwritten versions are discarded,
        /// and the data is rearranged to reduce the cost of operations
        /// needed to access the data.  This operation should typically only
        /// be invoked by users who understand the underlying implementation.
        /// </summary>
        /// <param name="startKey"></param>
        /// <param name="limitKey"></param>
        public void CompactRange(byte[] startKey, byte[] limitKey)
        {
            CheckDisposed();
            LevelDBInterop.leveldb_compact_range(Handle, startKey, LevelDBInterop.MarshalSize(startKey),
                limitKey, LevelDBInterop.MarshalSize(limitKey));
            GC.KeepAlive(this);
        }

        /// <summary>
        /// Returns the approximate file system space used by keys in "[start .. limit)".
        ///
        /// Note that the returned sizes measure file system space usage, so
        /// if the user data compresses by a factor of ten, the returned
        /// sizes will be one-tenth the size of the corresponding user data size.
        ///
        /// The results may not include the sizes of recently written data.
        /// </summary>
        /// <param name="startKey"></param>
        /// <param name="limitKey"></param>
        /// <returns></returns>
        public unsafe long GetApproximateSize(string startKey, string limitKey)
        {
            return GetApproximateSize(Encoding.UTF8.GetBytes(startKey), Encoding.UTF8.GetBytes(limitKey));
        }

        /// <summary>
        /// Returns the approximate file system space used by keys in "[start .. limit)".
        ///
        /// Note that the returned sizes measure file system space usage, so
        /// if the user data compresses by a factor of ten, the returned
        /// sizes will be one-tenth the size of the corresponding user data size.
        ///
        /// The results may not include the sizes of recently written data.
        /// </summary>
        /// <param name="startKey"></param>
        /// <param name="limitKey"></param>
        /// <returns></returns>
        public unsafe long GetApproximateSize(byte[] startKey, byte[] limitKey)
        {
            CheckDisposed();
            IntPtr l1 = (IntPtr)startKey.Length;
            IntPtr l2 = (IntPtr)limitKey.Length;
            long[] sizes = new long[1];

            LevelDBInterop.leveldb_approximate_sizes(Handle, 1, new byte[][] { startKey }, new IntPtr[] { l1 }, new byte[][] { limitKey }, new IntPtr[] { l2 }, sizes);
            GC.KeepAlive(this);

            return sizes[0];
        }

        private IntPtr MarshalArray(byte[] ar)
        {
            var p = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(byte)) * ar.Length);
            Marshal.Copy(ar, 0, p, ar.Length);
            return p;
        }

        protected override void FreeUnManagedObjects()
        {
            if (this.Handle != default(IntPtr))
                LevelDBInterop.leveldb_close(this.Handle);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        IEnumerator<KeyValuePair<string, string>> IEnumerable<KeyValuePair<string, string>>.GetEnumerator()
        {
            CheckDisposed();
            using (var sn = this.CreateSnapshot())
            using (var iterator = this.CreateIterator(new ReadOptions { Snapshot = sn }))
            {
                iterator.SeekToFirst();
                while (iterator.Valid())
                {
                    yield return new KeyValuePair<string, string>(iterator.StringKey(), iterator.StringValue());
                    iterator.Next();
                }
            }
        }

        public IEnumerator<KeyValuePair<byte[], byte[]>> GetEnumerator()
        {
            CheckDisposed();
            using (var sn = this.CreateSnapshot())
            using (var iterator = this.CreateIterator(new ReadOptions { Snapshot = sn }))
            {
                iterator.SeekToFirst();
                while (iterator.Valid())
                {
                    yield return new KeyValuePair<byte[], byte[]>(iterator.Key(), iterator.Value());
                    iterator.Next();
                }
            }
        }

        private void CheckDisposed()
        {
            if (!Disposed)
            {
                return;
            }
            throw new ObjectDisposedException(this.GetType().Name);
        }
    }
}
