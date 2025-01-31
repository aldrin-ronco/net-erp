using DevExpress.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Caliburn.Micro;
using DevExpress.Drawing.Internal.Fonts.Interop;

namespace NetErp.Global.Email.ViewModels
{
    public class EmailDetailViewModel: Screen
    {
        public EmailViewModel Context { get; set; }
        public EmailDetailViewModel(EmailViewModel context)
        {
            Context = context;
        }

        public async Task GoBack()
        {
            await Context.ActivateMasterView();
        }

        public ICommand _goBackCommand;

        public ICommand GoBackCommand
        {
            get
            {
                if (_goBackCommand is null) _goBackCommand = new AsyncCommand(GoBack);
                return _goBackCommand;
            }
        }

        private string _emailSmtp;

        public string EmailSmtp
        {
            get { return _emailSmtp; }
            set
            {
                if (_emailSmtp != value)
                {
                    _emailSmtp = value;
                    NotifyOfPropertyChange(nameof(EmailSmtp));
                }
            }
        }
        
        private string _emailDescription;

        public string EmailDescription
        {
            get { return _emailDescription; }
            set
            {
                if (_emailDescription != value)
                {
                    _emailDescription = value;
                    NotifyOfPropertyChange(nameof(EmailDescription));
                }
            }
        }

        private string _emailEmail;

        public string EmailEmail
        {
            get { return _emailEmail; }
            set
            {
                if (_emailEmail != value)
                {
                    _emailEmail = value;
                    NotifyOfPropertyChange(nameof(EmailEmail));
                }

            }
        }

        private string _emailPassword;

        public string EmailPassword
        {
            get { return _emailPassword; }
            set
            {
                if (_emailPassword != value)
                {
                    _emailPassword = value;
                    NotifyOfPropertyChange(nameof(EmailPassword));
                }
            }
        }


    }
}
