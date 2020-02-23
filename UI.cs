// Decompiled with JetBrains decompiler
// Type: ChapterUnorderer.UI
// Assembly: ChapterUnorderer, Version=1.0.0.1, Culture=neutral, PublicKeyToken=null
// MVID: 106F3BD1-044C-4380-8F8D-D9E187CA45E3
// Assembly location: E:\mkv\ChapterUnorderer.exe

namespace ChapterUnorderer
{
    internal interface IUi
    {
        void WriteLine(string message);

        void WriteWarning(string message);

        void WriteError(string message);
    }
}
