// Decompiled with JetBrains decompiler
// Type: ChapterUnorderer.TUI
// Assembly: ChapterUnorderer, Version=1.0.0.1, Culture=neutral, PublicKeyToken=null
// MVID: 106F3BD1-044C-4380-8F8D-D9E187CA45E3
// Assembly location: E:\mkv\ChapterUnorderer.exe

using System;
using System.IO;

namespace ChapterUnorderer
{
    internal class Tui : IUi
    {
        private TextWriter _log = Console.Out;

        public static void Main(string[] args)
        {
            var _ = new Tui(args);
        }

        public Tui(string[] args)
        {
            if (args.Length < 1)
                WriteSyntax();
            var fullPath = args[0];
            var str = (string) null;
            var mode = new bool?();
            for (var index = 1; index < args.Length; ++index)
                try
                {
                    switch (args[index])
                    {
                        case "-o":
                            str = args[++index];
                            continue;
                        case "-f":
                            mode = true;
                            continue;
                        case "-s":
                            mode = false;
                            continue;
                        default:
                            WriteWarning(args[index] + " is not a valid switch");
                            continue;
                    }
                }
                catch (IndexOutOfRangeException ex)
                {
                    WriteError("Switch " + args[index - 1] + " requires an argument");
                }

            if (str == null)
                str = fullPath + Path.DirectorySeparatorChar +
                      new DirectoryInfo(fullPath).Name.Trim(Path.GetInvalidFileNameChars()) + "." +
                      (ChapterUnorderer.IsUnix ? "sh" : (object) "bat");
            try
            {
                fullPath = Path.GetFullPath(fullPath);
                str = Path.GetFullPath(str);
            }
            catch (ArgumentException ex)
            {
                WriteError(ex.ParamName + " is not a valid directory");
            }

            if (!Directory.Exists(fullPath))
                WriteError("Source directory " + fullPath + " does not exist");
            if (!Directory.Exists(Path.GetDirectoryName(str)))
                WriteError("Destination directory " + Path.GetDirectoryName(str) + " does not exist");
            WriteLine("Source directory: " + fullPath);
            WriteLine("Destination file: " + str);
            WriteLine(string.Empty);
            new ChapterUnorderer(this, fullPath, str, mode).Run();
        }

        public void WriteLine(string message)
        {
            _log.WriteLine(message);
        }

        public void WriteWarning(string message)
        {
            WriteLine("Warning: " + message);
        }

        public void WriteError(string message)
        {
            WriteLine("Error: " + message);
            Environment.Exit(1);
        }

        public void WriteSyntax()
        {
            WriteLine("Syntax: " + Environment.GetCommandLineArgs()[0] +
                      " <source directory> [-o <destination file>] [-f|-s]");
            WriteLine(string.Empty);
            WriteLine("<source directory>");
            WriteLine("    Directory containing input Matroska files.");
            WriteLine("-o <destination file>");
            WriteLine("    Filename of output script file.");
            WriteLine("    Default: Name of source directory plus appropriate extension.");
            WriteLine("-f|-s");
            WriteLine("    Force fast or slow mode. Slow mode handles complex cases better");
            WriteLine("    than fast mode, but may deliver worse results in other cases.");
            WriteLine("    Default: Guess the optimal mode for every file (inaccurate!).");
            Environment.Exit(0);
        }
    }
}