using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using FolderMerge.Properties;

namespace FolderMerge
{
    internal class FileCounter : BackgroundWorker
    {
        private readonly string _folder1;
        private readonly string _folder2;
        private int _numOfFiles;

        public FileCounter(string folder1, string folder2)
        {
            _folder1 = folder1;
            _folder2 = folder2;
        }

        public event OnProgressFileFound FoundFile;
        internal delegate void OnProgressFileFound(object sender, ProgressEventArgs args);
        public void OnFoundFile(ProgressEventArgs args)
        {
            var Handler = FoundFile;
            if (Handler != null) Handler(this, args);
        }

        public event OnCompleteFindingFiles CompletedFindingFiles;
        internal delegate void OnCompleteFindingFiles(object sender, ProgressEventArgs args);
        public void OnCompletedFindingFiles(ProgressEventArgs args)
        {
            OnCompleteFindingFiles Handler = CompletedFindingFiles;
            if (Handler != null) Handler(this, args);
        }

        protected override void OnDoWork(DoWorkEventArgs e)
        {
            _numOfFiles = 0;
            CountFilesInDirectory(_folder1);
            CountFilesInDirectory(_folder2);
            OnCompletedFindingFiles(new ProgressEventArgs{MaxNumber = _numOfFiles});
        }
        
        private void CountFilesInDirectory(string initialDirectory)
        {
            //TODO 020 fix duplicate code

            // This stack stores the directories to process.
            Stack<string> Stack = new Stack<string>();

            // Add the initial directory
            Stack.Push(initialDirectory);

            // Continue processing for each stacked directory
            while ((Stack.Count > 0))
            {
                // Get top directory string
                string Dir = Stack.Pop();
                try
                {
                    //count files

                    _numOfFiles += Directory.GetFiles(Dir).Length;
                    OnFoundFile(new ProgressEventArgs { ProgressNumber = _numOfFiles });
                    //result.AddRange(Directory.GetFiles(dir, "*.*"))

                    // Loop through all subdirectories and add them to the stack.
                    foreach (string TempDir in Directory.GetDirectories(Dir))
                    {
                        string DirectoryName = TempDir;
                        Stack.Push(DirectoryName);
                    }
                }
                catch (Exception Ex)
                {
                    Console.WriteLine(Resources.ErrorGetFilesRecursive + Ex.Message);
                }
            }
        }
    }

}
