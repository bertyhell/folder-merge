namespace FolderMerge
{
    /// <summary>
    /// Interaction logic for ProgressBarControl.xaml
    /// </summary>
    public partial class ProgressBarControl
    {
        public ProgressBarControl()
        {
            InitializeComponent();
        }

        public bool IsIndeterminate
        {
            get { return progressbar.IsIndeterminate; }
            set
            {
                progressbar.IsIndeterminate = value;
            }
        }
    }
}
