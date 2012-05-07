namespace FolderMerge
{
    /// <summary>
    /// Interaction logic for ProgressbarWindow.xaml
    /// </summary>
    public partial class ProgressbarWindow
    {
        public ProgressbarWindow(MainWindow context)
        {
            InitializeComponent();
            progressBarControl1.DataContext = context;
        }

        public bool IsIndeterminate
        {
            get { return progressBarControl1.IsIndeterminate; }
            set
            {
                progressBarControl1.IsIndeterminate = value;
            }
        }
    }
}

