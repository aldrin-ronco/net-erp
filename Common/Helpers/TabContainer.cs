
using DevExpress.Mvvm;

namespace Common.Helpers
{
    public class TabContainer : BindableBase
    {
        private string _Header = string.Empty;
        public string Header
        {
            get { return _Header; }
            set { SetValue(ref _Header, value); }
        }

        private bool _AllowHide;
        public bool AllowHide
        {
            get { return _AllowHide; }
            set { SetValue(ref _AllowHide, value); }
        }

        private bool _IsSelected;
        public bool IsSelected
        {
            get { return _IsSelected; }
            set { SetValue(ref _IsSelected, value); }
        }

        private ViewModelBase _Content;
        public ViewModelBase Content
        {
            get { return _Content; }
            set { SetValue(ref _Content, value); }
        }
    }
}
