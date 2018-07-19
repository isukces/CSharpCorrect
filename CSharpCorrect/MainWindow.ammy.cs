using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using Microsoft.CodeAnalysis.CSharp;

namespace CSharpCorrect
{
    public partial class MainWindow
    {
        public MainWindow()
        {
            CorrectionCodeText = "ExceptionReporter.Report({0});";
            InitializeComponent();
        }

        private static void DependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
        }

        private static DirectoryInfo[] GetDirectories(DirectoryInfo directoryInfo)
        {
            try
            {
                return directoryInfo.GetDirectories();
            }
            catch
            {
                return new DirectoryInfo[0];
            }
        }

        private static FileInfo[] GetFiles(DirectoryInfo directoryInfo)
        {
            try
            {
                return directoryInfo.GetFiles();
            }
            catch
            {
                return new FileInfo[0];
            }
        }

        private static IEnumerable<FileInfo> ProcessDroppedFiles(DragEventArgs e)
        {
            IEnumerable<FileInfo> Scan(DirectoryInfo d)
            {
                foreach (var i in GetFiles(d))
                    yield return i;

                foreach (var dd in GetDirectories(d))
                {
                    if (string.Equals(dd.Name, "bin", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(dd.Name, "obj", StringComparison.OrdinalIgnoreCase))
                        continue;
                    var enu = Scan(dd).ToList();
                    foreach (var i in enu)
                        yield return i;
                }
            }

            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) yield break;
            if (!(e.Data.GetData(DataFormats.FileDrop) is string[] files)) yield break;
            foreach (var file in files)
            {
                if (File.Exists(file))
                    yield return new FileInfo(file);
                if (Directory.Exists(file))
                {
                    var d = new DirectoryInfo(file);
                    var enu = Scan(new DirectoryInfo(file)).ToList();
                    foreach (var i in enu)
                        yield return i;
                }

                e.Handled = true;
            }
        }

        private async void DropCorrect(object sender, DragEventArgs e)
        {
            var files = ProcessDroppedFiles(e)
                .Where(a => string.Equals(a.Extension, ".cs", StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (files.Count == 0)
                return;
            TotalFilesCount = files.Count;
            CorrectedFilesCount = 0;
            CorrectedStatementsCount=0;

            var done = 0;
            var modifiedFiles = 0;
            foreach (var file in files)
            {
                var fileCode = File.ReadAllText(file.FullName);
                var tree = CSharpSyntaxTree.ParseText(fileCode);
                var analizer = new CatchCorrector
                {
                    DoCorrection = true,
                    CorrectionStatement = CorrectionCodeText
                };
                await analizer.ProcessAsync(tree, CancellationToken.None);
                done++;
                CorrectedProgressBar.Value = done;
                if (!analizer.WasModified) continue;
                modifiedFiles++;
                CorrectedFilesCount      =  modifiedFiles;
                CorrectedStatementsCount += analizer.ModifiedStatements;
                if (SaveModifiedFiles)
                    File.WriteAllText(file.FullName, analizer.ModifiedCode);
            }

            CorrectedProgressBar.Value = 0;
        }


        private void DropPreview(object sender, DragEventArgs e)
        {
            e.Handled = true;
        }
        
        public int CorrectedFilesCount
        {
            get { return (int)GetValue(CorrectedFilesCountProperty); }
            set { SetValue(CorrectedFilesCountProperty, value); }
        }

        public string CorrectionCodeText
        {
            get { return (string)GetValue(CorrectionCodeTextProperty); }
            set { SetValue(CorrectionCodeTextProperty, value); }
        }

        public int TotalFilesCount
        {
            get { return (int)GetValue(TotalFilesCountProperty); }
            set { SetValue(TotalFilesCountProperty, value); }
        }

        public static readonly DependencyProperty CorrectedFilesCountProperty =
            DependencyProperty.Register(nameof(CorrectedFilesCount), typeof(int), typeof(MainWindow),
                new PropertyMetadata(DependencyPropertyChanged));

        public static readonly DependencyProperty CorrectionCodeTextProperty =
            DependencyProperty.Register(nameof(CorrectionCodeText), typeof(string), typeof(MainWindow),
                new PropertyMetadata(DependencyPropertyChanged));

        public static readonly DependencyProperty TotalFilesCountProperty =
            DependencyProperty.Register(nameof(TotalFilesCount), typeof(int), typeof(MainWindow),
                new PropertyMetadata(DependencyPropertyChanged));
        
        
        public int CorrectedStatementsCount
        {
            get { return (int)GetValue(CorrectedStatementsCountProperty); }
            set { SetValue(CorrectedStatementsCountProperty, value); }
        }

        public static readonly DependencyProperty CorrectedStatementsCountProperty =
            DependencyProperty.Register(nameof(CorrectedStatementsCount), typeof(int), typeof(MainWindow),
                new PropertyMetadata(DependencyPropertyChanged));
        
        
        
        public bool SaveModifiedFiles
        {
            get { return (bool)GetValue(SaveModifiedFilesProperty); }
            set { SetValue(SaveModifiedFilesProperty, value); }
        }

        public static readonly DependencyProperty SaveModifiedFilesProperty =
            DependencyProperty.Register(nameof(SaveModifiedFiles), typeof(bool), typeof(MainWindow),
                new PropertyMetadata(DependencyPropertyChanged));
    }
}