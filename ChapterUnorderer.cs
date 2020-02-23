// Decompiled with JetBrains decompiler
// Type: ChapterUnorderer
// Assembly: ChapterUnorderer, Version=1.0.0.1, Culture=neutral, PublicKeyToken=null
// MVID: 106F3BD1-044C-4380-8F8D-D9E187CA45E3
// Assembly location: E:\mkv\ChapterUnorderer.exe

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Xml;

namespace ChapterUnorderer
{
    internal class ChapterUnorderer
    {
        public static readonly bool IsUnix = false;
        private readonly string[] _dependencies = { "mkvinfo", "mkvextract" };
        private readonly string _delApp = IsUnix ? "rm" : "del";
        private readonly IUi _ui;
        private TextWriter _dst;
        private readonly string _sourceDir;
        private readonly string _dstFile;
        private readonly bool? _mode;

        public ChapterUnorderer(IUi ui, string sourceDir, string dstFile, bool? mode)
        {
            _ui = ui;
            _sourceDir = sourceDir;
            _dstFile = dstFile;
            _mode = mode;
        }

        public void Run()
        {
            var files = new List<MatroskaFile>();
            foreach (var dependency in _dependencies)
            {
                try
                {
                    Process.Start(dependency);
                }
                catch (Win32Exception ex)
                {
                    _ui.WriteError("Could not find " + dependency + " in path");
                }
            }
            foreach (var file in Directory.GetFiles(_sourceDir))
            {
                try
                {
                    var matroskaFile = AnalyzeFile(file);
                    files.Add(matroskaFile);
                }
                catch (MatroskaException ex)
                {
                    _ui.WriteLine(" " + ex.Message);
                    _ui.WriteLine(string.Empty);
                }
            }
            try
            {
                using (_dst = new StreamWriter(_dstFile))
                {
                    _ui.WriteLine("Created: " + _dstFile);
                    _ui.WriteLine(string.Empty);
                    for (var idx = 0; idx < files.Count; ++idx)
                        ProcessFile(files, idx);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                _ui.WriteError("Could not write to " + _dstFile);
            }
        }

        private MatroskaFile AnalyzeFile(string sourceFile)
        {
            _ui.WriteLine("Analyzing: " + Path.GetFileName(sourceFile));
            var suid = ExtractSuid(sourceFile);
            _ui.WriteLine(" SUID: " + suid);
            var chapters = ExtractChapters(sourceFile);
            _ui.WriteLine(" Chapters: " + (chapters.HasChildNodes ? "Yes, " + (IsFlagSet(chapters.GetElementsByTagName("EditionFlagOrdered")) ? "un" : string.Empty) + "ordered" : "No"));
            _ui.WriteLine(string.Empty);
            return new MatroskaFile(sourceFile, suid, chapters);
        }

        private void ProcessFile(List<MatroskaFile> files, int idx)
        {
            var chapters = files[idx].Chapters;
            if (!chapters.HasChildNodes)
                return;
            var xmlNodeList = chapters.SelectSingleNode("Chapters").SelectNodes("EditionEntry");
            var mode = _mode;
            var flag = mode.HasValue ? mode.GetValueOrDefault() : xmlNodeList.Count == 1;
            _ui.WriteLine("Processing: " + Path.GetFileName(files[idx].FileName));
            _ui.WriteLine(" Mode: " + (flag ? "Fast" : "Slow"));
            _ui.WriteLine(string.Empty);
            for (var index = 0; index < xmlNodeList.Count; ++index)
            {
                // TODO: Flag appears to be an invalid XML node in some split ordering encodes
                //if (IsFlagSet(xmlNodeList[index].SelectNodes("EditionFlagOrdered"))) continue;
                var nodes = xmlNodeList[index].SelectNodes("ChapterAtom");
                var muxCmd =
                    $"mkvmerge --no-chapters -o \"(New){(xmlNodeList.Count > 1 ? "(" + (index + 1) + ")" : string.Empty)}{Path.GetFileName(files[idx].FileName)}\" ";
                var empty = string.Empty;
                if (flag)
                    ProcessEditionFast(files, idx, nodes, ref muxCmd, ref empty);
                else
                    ProcessEditionSlow(files, idx, nodes, ref muxCmd, ref empty);
                _dst.WriteLine(muxCmd.TrimEnd(' ', '+'));
                if (empty.Length > 0)
                    _dst.WriteLine(_delApp + " " + empty.TrimEnd());
                _dst.WriteLine();
            }
        }

        private void ProcessEditionFast(List<MatroskaFile> files, int idx, XmlNodeList nodes, ref string muxCmd, ref string delCmd)
        {
            var stringCollection = new StringCollection();
            var nameValueCollection = new NameValueCollection();
            var str1 = (string)null;
            foreach (XmlNode node in nodes)
            {
                var xmlNode = node.SelectSingleNode("ChapterSegmentUID");
                var suid = xmlNode != null ? xmlNode.InnerText.Trim() : files[idx].Suid;
                var fileName = files.Find(m => m.Suid == suid).FileName;
                if (fileName == null)
                    _ui.WriteWarning("Unable to find SUID \"" + suid + "\"");
                else
                {
                    var str2 = node.SelectSingleNode("ChapterTimeStart").InnerText.Trim();
                    if (fileName.Equals(str1)) continue;
                    stringCollection.Add(str1 = fileName);
                    if (str2.Trim(':', '.', '0').Length > 0)
                        nameValueCollection.Add(fileName, str2);
                }
            }
            var sortedList1 = new SortedList<string, int>();
            foreach (var allKey in nameValueCollection.AllKeys)
            {
                sortedList1.Add(allKey, 0);
                _dst.WriteLine("mkvmerge --no-chapters --split timecodes:{0} -o \"{1}\" \"{2}\"", nameValueCollection[allKey], "(Tmp)" + Path.GetFileName(allKey), allKey);
            }
            foreach (var index1 in stringCollection)
            {
                if (sortedList1.ContainsKey(index1))
                {
                    SortedList<string, int> sortedList2;
                    string index2;
                    (sortedList2 = sortedList1)[index2 = index1] = sortedList2[index2] + 1;
                    var str2 = "(Tmp)" + Path.GetFileNameWithoutExtension(index1) + "-" + sortedList1[index1].ToString("000") + Path.GetExtension(index1);
                    ref var local1 = ref muxCmd;
                    local1 = local1 + "\"" + str2 + "\" +";
                    ref var local2 = ref delCmd;
                    local2 = local2 + "\"" + str2 + "\" ";
                }
                else
                {
                    ref var local = ref muxCmd;
                    local = local + "\"" + index1 + "\" +";
                }
            }
        }

        private void ProcessEditionSlow(List<MatroskaFile> files, int idx, IEnumerable nodes, ref string muxCmd, ref string delCmd)
        {
            var fileName1 = files[idx].FileName;
            var str1 = "(Tmp)" + Path.GetFileNameWithoutExtension(fileName1);
            var num1 = -1;
            foreach (XmlNode node in nodes)
            {
                var xmlNode = node.SelectSingleNode("ChapterSegmentUID");
                var suid = xmlNode != null ? xmlNode.InnerText.Trim() : files[idx].Suid;
                var fileName2 = files.Find(m => m.Suid == suid).FileName;
                if (fileName2 == null)
                {
                    _ui.WriteWarning("Unable to find SUID \"" + suid + "\"");
                }
                else
                {
                    _dst.WriteLine("mkvmerge --no-chapters --split timecodes:{0} -o \"{1}\" \"{2}\"",
                        node.SelectSingleNode("ChapterTimeStart").InnerText.Trim() + "," +
                        node.SelectSingleNode("ChapterTimeEnd").InnerText.Trim(),
                        str1 + "-" + ++num1 + Path.GetExtension(fileName1), fileName2);
                    var str2 = string.Empty;
                    var numArray = new int[2] { 1, 3 };
                    foreach (var num2 in numArray)
                        str2 = str2 + "\"" + str1 + "-" + num1 + "-" + num2.ToString("000") + Path.GetExtension(fileName1) + "\" ";
                    _dst.WriteLine(_delApp + " " + str2.Trim());
                }
            }
            for (var index = 0; index <= num1; ++index)
            {
                var str2 = str1 + "-" + index + "-002" + Path.GetExtension(fileName1);
                ref var local1 = ref muxCmd;
                local1 = local1 + "\"" + str2 + "\" +";
                ref var local2 = ref delCmd;
                local2 = local2 + "\"" + str2 + "\" ";
            }
        }

        private string ExtractSuid(string sourceFile)
        {
            var process = new Process
            {
                StartInfo =
                {
                    UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, FileName = "mkvinfo",
                    Arguments = "\"" + sourceFile + "\""
                }
            };
            process.Start();
            var end = process.StandardOutput.ReadToEnd();
            var num = end.IndexOf("Segment UID:");
            if (num == -1)
                throw new MatroskaException("Not a Matroska file");
            return end.Substring(num + 13, 79).Replace(" 0x", string.Empty).Replace("0x", string.Empty);
        }

        private XmlDocument ExtractChapters(string sourceFile)
        {
            var process = new Process
            {
                StartInfo =
                {
                    UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true,
                    FileName = "mkvextract", Arguments = "chapters \"" + sourceFile + "\""
                }
            };
            process.Start();
            var end = process.StandardOutput.ReadToEnd();
            var xmlDocument = new XmlDocument();
            if (!end.Equals(string.Empty))
                xmlDocument.LoadXml(end);
            return xmlDocument;
        }

        private bool IsFlagSet(XmlNodeList flag)
        {
            return flag.Count == 0 || !flag.Item(0).InnerText.Equals("1");
        }

        private struct MatroskaFile
        {
            public readonly XmlDocument Chapters;
            public readonly string FileName;
            public readonly string Suid;

            public MatroskaFile(string fileName, string suid, XmlDocument chapters)
            {
                FileName = fileName;
                Suid = suid;
                Chapters = chapters;
            }
        }
    }
}
