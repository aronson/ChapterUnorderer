// Decompiled with JetBrains decompiler
// Type: ChapterUnorderer.MatroskaException
// Assembly: ChapterUnorderer, Version=1.0.0.1, Culture=neutral, PublicKeyToken=null
// MVID: 106F3BD1-044C-4380-8F8D-D9E187CA45E3
// Assembly location: E:\mkv\ChapterUnorderer.exe

using System;

namespace ChapterUnorderer
{
    internal class MatroskaException : Exception
    {
        public MatroskaException(string message)
          : base(message)
        {
        }
    }
}
