using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using FolderMerge.Properties;

namespace FolderMerge
{
    public class MergeWorker : BackgroundWorker
    {
        private readonly string _pathFolder1;
        private readonly string _pathFolder2;
        private readonly string _pathFolderCombo;
        private readonly int _comparePropertyIndex;

        private int _numOfFilesMerged;
        private readonly int _maxNumOfFiles;

        public MergeWorker(string pathFolder1, string pathFolder2, string pathFolderCombo, int maxNumOfFiles, int comparePropertyIndex = 0)
        {
            _pathFolder1 = pathFolder1;
            _pathFolder2 = pathFolder2;
            _pathFolderCombo = pathFolderCombo;
            _comparePropertyIndex = comparePropertyIndex;
            _numOfFilesMerged = 0;
            _maxNumOfFiles = maxNumOfFiles;
        }

        protected override void OnDoWork(DoWorkEventArgs e)
        {
            MergeFolders(_pathFolder1, _pathFolder2, _pathFolderCombo, _comparePropertyIndex);
        }


        public event OnProgressFileMerged FileMerged;

        public void OnFileMerged(ProgressEventArgs args)
        {
            OnProgressFileMerged Handler = FileMerged;
            if (Handler != null) Handler(this, args);
        }

        public delegate void OnProgressFileMerged(object sender, ProgressEventArgs args);

        private void MergeFolders(string pathFolder1, string pathFolder2, string pathFolderCombo, int comparePropertyIndex = 0)
        {

            //scan all files and folders in folder(1,2) and copy newest(compare selection) items to foldercombo
            var Folder1 = new DirectoryInfo(pathFolder1);
            var Folder2 = new DirectoryInfo(pathFolder2);
            var FilesFolder1 = Folder1.GetFiles();
            var FilesFolder2 = Folder2.GetFiles();
            var IndexFolder1 = 0;
            var IndexFolder2 = 0;

            var MergeInFolder1 = pathFolder1 == pathFolderCombo;
            var MergeInFolder2 = pathFolder2 == pathFolderCombo;

            //scan files
            while (IndexFolder1 < FilesFolder1.Length && IndexFolder2 < FilesFolder2.Length)
            {
                var Comp = String.Compare(FilesFolder1[IndexFolder1].Name, FilesFolder2[IndexFolder2].Name, StringComparison.Ordinal);
                if (Comp == 0)
                {
                    //equal files check for property to compare(last edit)
                    bool CopyFileFromFolder1;
                    switch (comparePropertyIndex)
                    {
                        case 1:
                            //filesize
                            CopyFileFromFolder1 = FilesFolder1[IndexFolder1].Length >
                                                  FilesFolder2[IndexFolder2].Length;
                            break;
                        case 2:
                            //last edit date
                            CopyFileFromFolder1 = FilesFolder1[IndexFolder1].LastWriteTimeUtc >
                                                  FilesFolder2[IndexFolder2].LastWriteTimeUtc;
                            break;
                        case 3:
                            //last access date
                            CopyFileFromFolder1 = FilesFolder1[IndexFolder1].LastAccessTimeUtc >
                                                  FilesFolder2[IndexFolder2].LastAccessTimeUtc;
                            break;
                        default:
                            //creation date (0)
                            CopyFileFromFolder1 = FilesFolder1[IndexFolder1].CreationTimeUtc >
                                                  FilesFolder2[IndexFolder2].CreationTimeUtc;
                            break;
                    }

                    if (CopyFileFromFolder1)
                    {
                        var From = FilesFolder1[IndexFolder1].FullName;
                        var To = Path.Combine(pathFolderCombo, FilesFolder1[IndexFolder1].Name);
                        if(From != To)
                        {
                            File.Copy(From, To, true);
                        }
                    }
                    else
                    {
                        var From = FilesFolder2[IndexFolder2].FullName;
                        var To = Path.Combine(pathFolderCombo, FilesFolder2[IndexFolder2].Name);
                        if (From != To)
                        {
                            File.Copy(From, To, true);
                        }
                    }
                    IndexFolder2++;
                    IndexFolder1++;
                    _numOfFilesMerged += 2;
                    OnFileMerged(new ProgressEventArgs { MaxNumber = _maxNumOfFiles, ProgressNumber = _numOfFilesMerged });
                }
                else if (Comp > 0)
                {
                    //filename from list folder1 is larger then file from folder2
                    //   copy file folder2 to merge
                    //   increase indexFolder2
                    var From = FilesFolder2[IndexFolder2].FullName;
                    var To = Path.Combine(pathFolderCombo, FilesFolder2[IndexFolder2].Name);
                    if (From != To)
                    {
                        File.Copy(From, To, true);
                    }
                    IndexFolder2 += 1;
                    _numOfFilesMerged++;
                    OnFileMerged(new ProgressEventArgs { MaxNumber = _maxNumOfFiles, ProgressNumber = _numOfFilesMerged });
                }
                else
                {
                    //filename from list folder1 is smaller then file from folder2
                    //   copy file folder1 to merge
                    //   increase indexFolder1
                    var From = FilesFolder1[IndexFolder1].FullName;
                    var To = Path.Combine(pathFolderCombo, FilesFolder1[IndexFolder1].Name);
                    if(From != To)
                    {
                        File.Copy(From, To, true);                        
                    }
                    IndexFolder1 += 1;
                    _numOfFilesMerged++;
                    OnFileMerged(new ProgressEventArgs { MaxNumber = _maxNumOfFiles, ProgressNumber = _numOfFilesMerged });
                }
            }
            //copy rest of files in either folder1 or folder2 to foldercombo
            if (IndexFolder1 == FilesFolder1.Length && pathFolder1 != pathFolderCombo)
            {
                //continue with folder 2
                while (IndexFolder2 < FilesFolder2.Length)
                {
                    var From = FilesFolder2[IndexFolder2].FullName;
                    var To = Path.Combine(pathFolderCombo, FilesFolder2[IndexFolder2].Name);
                    if (From != To)
                    {
                        File.Copy(From, To, true);
                    }
                    IndexFolder2++;
                    _numOfFilesMerged++;
                    OnFileMerged(new ProgressEventArgs { MaxNumber = _maxNumOfFiles, ProgressNumber = _numOfFilesMerged });
                }
            }
            else if(pathFolder2 != pathFolderCombo)
            {
                //continue with folder 1
                while (IndexFolder1 < FilesFolder1.Length)
                {
                    var From = FilesFolder1[IndexFolder1].FullName;
                    var To = Path.Combine(pathFolderCombo, FilesFolder1[IndexFolder1].Name);
                    if (From != To)
                    {
                        File.Copy(From, To, true);
                    }
                    IndexFolder1++;
                    _numOfFilesMerged++;
                    OnFileMerged(new ProgressEventArgs { MaxNumber = _maxNumOfFiles, ProgressNumber = _numOfFilesMerged });
                }
            }

            //handle directories by caling a recursive method
            var FoldersFolder1 = Folder1.GetDirectories();
            var FoldersFolder2 = Folder2.GetDirectories();
            IndexFolder1 = 0;
            IndexFolder2 = 0;
            while (IndexFolder1 < FoldersFolder1.Length && IndexFolder2 < FoldersFolder2.Length)
            {
                int Comp = String.Compare(FoldersFolder1[IndexFolder1].Name, FoldersFolder2[IndexFolder2].Name, StringComparison.Ordinal);
                if (Comp == 0)
                {
                    //folder exists in folder(1,2) -> create and call recursuve
                    if(pathFolder1 != pathFolderCombo && pathFolder2 != pathFolderCombo)
                    {
                        Directory.CreateDirectory(Path.Combine(pathFolderCombo, FoldersFolder1[IndexFolder1].Name));
                    }
                    MergeFolders(FoldersFolder1[IndexFolder1].FullName, FoldersFolder2[IndexFolder2].FullName,
                                 Path.Combine(pathFolderCombo, FoldersFolder1[IndexFolder1].Name),
                                 comparePropertyIndex);
                    IndexFolder2++;
                    IndexFolder1++;
                    _numOfFilesMerged += 2;
                    OnFileMerged(new ProgressEventArgs { MaxNumber = _maxNumOfFiles, ProgressNumber = _numOfFilesMerged });

                }
                else if (Comp > 0)
                {
                    //foldername from list folder1 is larger then folder from folder2
                    //   copy folder folder2 to merge
                    //   increase indexFolder2

                    var From = FoldersFolder2[IndexFolder2];
                    var To = Path.Combine(pathFolderCombo, FoldersFolder2[IndexFolder2].Name);
                    if(From.FullName != To)
                    {
                        CopyDirectory(From, new DirectoryInfo(To));
                    }

                    IndexFolder2 ++;
                    _numOfFilesMerged += CountFilesInDirectory(FoldersFolder2[IndexFolder2].FullName);
                    OnFileMerged(new ProgressEventArgs { MaxNumber = _maxNumOfFiles, ProgressNumber = _numOfFilesMerged });

                    _numOfFilesMerged += CountFilesInDirectory(FoldersFolder2[IndexFolder2].FullName);
                    OnFileMerged(new ProgressEventArgs { MaxNumber = _maxNumOfFiles, ProgressNumber = _numOfFilesMerged });
                }
                else
                {
                    //foldername from list folder2 is larger then folder from folder1
                    //   copy folder folder1 to merge
                    //   increase indexFolder1

                    var From = FoldersFolder1[IndexFolder1];
                    var To = Path.Combine(pathFolderCombo, FoldersFolder1[IndexFolder1].Name);
                    if (From.FullName != To)
                    {
                        CopyDirectory(From, new DirectoryInfo(To));
                    }

                    IndexFolder1 ++;
                    _numOfFilesMerged += CountFilesInDirectory(FoldersFolder1[IndexFolder1].FullName);
                    OnFileMerged(new ProgressEventArgs { MaxNumber = _maxNumOfFiles, ProgressNumber = _numOfFilesMerged });

                    _numOfFilesMerged += CountFilesInDirectory(FoldersFolder1.ElementAt(IndexFolder1).FullName);
                    OnFileMerged(new ProgressEventArgs { MaxNumber = _maxNumOfFiles, ProgressNumber = _numOfFilesMerged });
                }
            }
        }

        void CopyDirectory(DirectoryInfo source, DirectoryInfo destination)
        {
            // Check if the target directory exists, if not, create it.
            if (Directory.Exists(destination.FullName) == false)
            {
                Directory.CreateDirectory(destination.FullName);
            }

            // Copy each file into it’s new directory.
            foreach (FileInfo Fi in source.GetFiles())
            {
                Fi.CopyTo(Path.Combine(destination.FullName, Fi.Name), true);
            }

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo DiSourceSubDir in source.GetDirectories())
            {
                DirectoryInfo NextTargetSubDir = destination.CreateSubdirectory(DiSourceSubDir.Name);
                CopyDirectory(DiSourceSubDir, NextTargetSubDir);
            }
        }




        public event OnProgressFileFound FoundFile;

        public delegate void OnProgressFileFound(object sender, ProgressEventArgs args);

        private void OnFoundFile(ProgressEventArgs args)
        {
            OnProgressFileFound Handler = FoundFile;
            if (Handler != null) Handler(this, args);
        }

        private int CountFilesInDirectory(string initialDirectory)
        {
            //TODO 020 fix duplicate code
            int NumOfFiles = 0;

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
                    NumOfFiles += 1;
                    OnFoundFile(new ProgressEventArgs { ProgressNumber = NumOfFiles });
                    //result.AddRange(Directory.GetFiles(dir, "*.*"))

                    // Loop through all subdirectories and add them to the stack.
                    foreach (string DirectoryNameLoopVariable in Directory.GetDirectories(Dir))
                    {
                        string DirectoryName = DirectoryNameLoopVariable;
                        Stack.Push(DirectoryName);
                    }
                }
                catch (Exception Ex)
                {
                    Console.WriteLine(Resources.ErrorGetFilesRecursive + ": " + Ex.Message);
                }
            }

            // Return the list
            return NumOfFiles;
        }
    }
}
