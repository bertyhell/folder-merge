using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System;
using System.IO;
using Ookii.Dialogs.Wpf;

namespace FolderMerge
{
    public partial class MainWindow : INotifyPropertyChanged
    {
        public MainWindow()
        {
            //load dll from resource
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                try
                {
                    string ResName = "FolderMerge.lib." + args.Name.Split(',')[0] + ".dll";
                    using (Stream Input = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResName))
                    {
                        return Input != null
                             ? Assembly.Load(StreamToBytes(Input))
                             : null;
                    }
                }
                catch (Exception Ex)
                {
                    Console.WriteLine(Properties.Resources.ErrorLoadingDll + args.Name + ": " + Ex.Message);
                    return null;
                }
            };
        }

        private static byte[] StreamToBytes(Stream input)
        {
            int Capacity = input.CanSeek ? (int)input.Length : 0;
            using (MemoryStream Output = new MemoryStream(Capacity))
            {
                int ReadLength;
                byte[] Buffer = new byte[4096];

                do
                {
                    ReadLength = input.Read(Buffer, 0, Buffer.Length);
                    Output.Write(Buffer, 0, ReadLength);
                }
                while (ReadLength != 0);

                return Output.ToArray();
            }
        }

        private ProgressbarWindow _progressWindow;

        private int _totalNumberOfFiles;

        private void CbbComparePropSelectionChanged(Object sender, SelectionChangedEventArgs e)
        {
            SetStatusMergeButton();
        }

        private void TxtPathFolder1TextChanged(Object sender, TextChangedEventArgs e)
        {
            SetStatusMergeButton();
        }

        private void TxtPathFolder2TextChanged(Object sender, TextChangedEventArgs e)
        {
            SetStatusMergeButton();
        }

        private void TxtPathFolderComboTextChanged(Object sender, TextChangedEventArgs e)
        {
            SetStatusMergeButton();
        }

        private void SetStatusMergeButton()
        {
            if (txtPathFolder1 != null &&
                txtPathFolder2 != null &&
                txtPathFolderCombo != null &&
                !string.IsNullOrEmpty(txtPathFolder1.Text) &&
                !string.IsNullOrEmpty(txtPathFolder2.Text) &&
                !string.IsNullOrEmpty(txtPathFolderCombo.Text))
            {
                btnMerge.IsEnabled = true;
            }
            else
            {
                btnMerge.IsEnabled = false;
            }
        }

        private void BtnBrowseFolder1Click(Object sender, RoutedEventArgs e)
        {
            var Fbd = new VistaFolderBrowserDialog { Description = Properties.Resources.PleaseSelectAFolder, UseDescriptionForTitle = true, SelectedPath = Environment.SpecialFolder.MyComputer.ToString() };
            // ReSharper disable PossibleInvalidOperationException
            if ((bool)Fbd.ShowDialog(this))
            // ReSharper restore PossibleInvalidOperationException
            {
                txtPathFolder1.Text = Fbd.SelectedPath;
                if (!string.IsNullOrEmpty(txtPathFolderCombo.Text))
                {
                    txtPathFolderCombo.Text = Fbd.SelectedPath;
                }
            }
            SetStatusMergeButton();
        }

        private void BtnBrowseFolder2Click(Object sender, RoutedEventArgs e)
        {
            var Fbd = new VistaFolderBrowserDialog { Description = Properties.Resources.PleaseSelectAFolder, UseDescriptionForTitle = true, SelectedPath = Environment.SpecialFolder.MyComputer.ToString() };
            // ReSharper disable PossibleInvalidOperationException
            if ((bool)Fbd.ShowDialog(this))
            // ReSharper restore PossibleInvalidOperationException
            {
                txtPathFolder2.Text = Fbd.SelectedPath;
                if (!string.IsNullOrEmpty(txtPathFolderCombo.Text))
                {
                    txtPathFolderCombo.Text = Fbd.SelectedPath;
                }
            }
            SetStatusMergeButton();
        }

        private void BtnBrowseFolderComboClick(Object sender, RoutedEventArgs e)
        {
            var Fbd = new VistaFolderBrowserDialog { Description = Properties.Resources.PleaseSelectAFolder, UseDescriptionForTitle = true, SelectedPath = Environment.SpecialFolder.MyComputer.ToString() };
            // ReSharper disable PossibleInvalidOperationException
            if ((bool)Fbd.ShowDialog(this))
            // ReSharper restore PossibleInvalidOperationException
            {
                txtPathFolderCombo.Text = Fbd.SelectedPath;
            }
            SetStatusMergeButton();
        }

        private void BtnMergeClick(Object sender, RoutedEventArgs e)
        {
            //merge folder 1 and folder2 in mergefolder
            //check if folder1 or folder2 = foldercombo
            var Folder1 = txtPathFolder1.Text;
            var Folder2 = txtPathFolder2.Text;
            var PathFolderCombo = txtPathFolderCombo.Text;

            if (Folder1 == txtPathFolder2.Text)
            {
                MessageBox.Show(Properties.Resources.ErrorEqualSourceFolders);
            }
            else
            {
                if (new DirectoryInfo(Folder1).Exists && new DirectoryInfo(Folder2).Exists &&
                    new DirectoryInfo(txtPathFolderCombo.Text).Exists)
                {
                    if (Folder1 == PathFolderCombo)
                    {
                        if (
                            MessageBox.Show(Properties.Resources.WarningMergeIntoFolder1, "Overwrite files",
                                            MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) == MessageBoxResult.Yes)
                        {
                            StartCalculateWork(Folder1, Folder2);
                        }
                    }
                    else if (Folder2 == PathFolderCombo)
                    {
                        if (
                            MessageBox.Show(Properties.Resources.WarningMergeIntoFolder2, "Overwrite files",
                                            MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) == MessageBoxResult.Yes)
                        {
                            StartCalculateWork(Folder1, Folder2);
                        }
                    }
                    else
                    {
                        StartCalculateWork(Folder1, txtPathFolder2.Text);

                    }
                }
                else
                {
                    if (!new DirectoryInfo(Folder1).Exists)
                    {
                        MessageBox.Show(Properties.Resources.ErrorSourceFolder1NotExisiting);
                    }
                    else if (!new DirectoryInfo(txtPathFolder2.Text).Exists)
                    {
                        MessageBox.Show(Properties.Resources.ErrorSourceFolder2NotExisiting);
                    }
                    else
                    {
                        MessageBox.Show(Properties.Resources.ErrorSourceFolderComboNotExisiting);
                    }
                }
            }
        }

        private void StartCalculateWork(string folder1, string folder2)
        {
            btnMerge.IsEnabled = false;
            try
            {
                //calculate work
                Message = Properties.Resources.FilesFound + ": 0";
                _progressWindow = new ProgressbarWindow(this) { Owner = this, IsIndeterminate = true, DataContext = this };
                var FileCounter = new FileCounter(folder1, folder2);
                FileCounter.FoundFile += OnProgressFileFind;
                FileCounter.CompletedFindingFiles += OnCompleteFileFind;
                FileCounter.RunWorkerAsync();
                _progressWindow.ShowDialog();

                //TODO 060 add cancel possibility
            }
            catch (IOException Ex)
            {
                MessageBox.Show(Properties.Resources.ErrorBtnMergeClick + ": " + Ex.Message);
            }
        }

        void OnProgressFileFind(object sender, ProgressEventArgs args)
        {
            Message = Properties.Resources.FilesFound + ": " + args.ProgressNumber;
        }

        private void HandleCompleteFileFind(ProgressEventArgs args)
        {
            _progressWindow.Close();
            _totalNumberOfFiles = args.MaxNumber;
            Message = Properties.Resources.MergingFiles + ": 0 / " + _totalNumberOfFiles;
            Value = 0;
            Maximum = _totalNumberOfFiles;
            _progressWindow = new ProgressbarWindow(this) { Owner = this, IsIndeterminate = false, DataContext = this };
            var MergeWorker = new MergeWorker(txtPathFolder1.Text, txtPathFolder2.Text, txtPathFolderCombo.Text, cbbCompareProp.SelectedIndex);
            MergeWorker.FileMerged += OnProgressFileMerge;
            MergeWorker.RunWorkerCompleted += OnCompleteFileMerge;
            MergeWorker.RunWorkerAsync();

            _progressWindow.ShowDialog();
        }

        private delegate void HandleCompleteFileFindDelegate(ProgressEventArgs args);

        void OnCompleteFileFind(object sender, ProgressEventArgs args)
        {
            _progressWindow.Dispatcher.Invoke(Delegate.CreateDelegate(typeof(HandleCompleteFileFindDelegate), this, "HandleCompleteFileFind"), new object[] { args });
        }

        void OnProgressFileMerge(object sender, ProgressEventArgs args)
        {
            Message = Properties.Resources.MergingFiles + ": " + args.ProgressNumber + " / " + _totalNumberOfFiles;
            Value = args.ProgressNumber;
        }

        private void HandleCompleteFileMerge()
        {
            _progressWindow.Close();
            MessageBox.Show(String.Format(Properties.Resources.FinishedMerging, _totalNumberOfFiles));
            btnMerge.IsEnabled = true;
        }

        private delegate void HandleCompleteFileMergeDelegate();

        void OnCompleteFileMerge(object sender, RunWorkerCompletedEventArgs e)
        {
            _progressWindow.Dispatcher.Invoke(Delegate.CreateDelegate(typeof(HandleCompleteFileMergeDelegate), this, "HandleCompleteFileMerge"));
        }

        private int _maximum;
        public int Maximum
        {
            get { return _maximum; }
            set
            {
                _maximum = value;
                PropChanged("Maximum");
            }
        }

        private int _value;
        public int Value
        {
            get { return _value; }
            set
            {
                _value = value;
                PropChanged("Value");
            }
        }

        private string _message;
        public string Message
        {
            get { return _message; }
            set
            {
                _message = value;
                PropChanged("Message");
            }
        }

        private void BtnMergeIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {

            if (IsLoaded)
            {
                if (btnMerge.IsEnabled)
                {
                    BitmapImage MergeIconSource = new BitmapImage();
                    MergeIconSource.BeginInit();
                    MergeIconSource.UriSource = new Uri("/img/merge.PNG", UriKind.Relative);
                    MergeIconSource.EndInit();
                    imgMerge.Source = MergeIconSource;
                }
                else
                {
                    BitmapImage MergeIconSource = new BitmapImage();
                    MergeIconSource.BeginInit();
                    MergeIconSource.UriSource = new Uri("/img/merge.PNG", UriKind.Relative);
                    MergeIconSource.EndInit();
                    imgMerge.Source = MergeIconSource;
                }
            }
        }

        private void TextBoxDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.All;
                e.Handled = true;
            }
        }

        private void TextBoxDragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] DroppedFilePaths = e.Data.GetData(DataFormats.FileDrop) as string[];

                if (DroppedFilePaths != null) ((TextBox)sender).Text = DroppedFilePaths[0];
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void PropChanged(string field)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(field));
            }
        }
    }
}
