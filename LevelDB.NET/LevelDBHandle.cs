﻿using System;
using System.Collections.Generic;
using System.Text;

namespace LevelDB
{
    /// <summary>
    /// Base class for all LevelDB objects
    /// Implement IDisposable as prescribed by http://msdn.microsoft.com/en-us/library/b1yfkh5e.aspx by overriding the two additional virtual methods
    /// </summary>
    public abstract class LevelDBHandle : IDisposable
    {
        protected bool Disposed;

        public IntPtr Handle { protected set; get; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void FreeManagedObjects()
        {
        }

        protected virtual void FreeUnManagedObjects()
        {
        }

        void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    FreeManagedObjects();
                }
                if (Handle != IntPtr.Zero)
                {
                    FreeUnManagedObjects();
                    Handle = IntPtr.Zero;
                }
                Disposed = true;
            }
        }

        ~LevelDBHandle()
        {
            Dispose(false);
        }
    }
}
