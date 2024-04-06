using Models.Global;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.DTO.Global
{
    public class EmailDTO : EmailGraphQLModel
    {
        public string UUID { get; set; } = System.Guid.NewGuid().ToString();
        public bool Edited { get; set; } = false;
        public bool Deleted { get; set; } = false;
        public bool Saved { get; set; } = true;
        public string ServerType
        {
            get
            {
                return this.Smtp == null ? "AWS SES" : "SMTP";
            }
        }
        public string ServerName => Smtp != null ? Smtp.Name : AwsSes != null ? AwsSes.Name : "";
        private bool _isChecked;
        public bool IsChecked
        {
            get { return _isChecked; }
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                }
            }
        }
    }
}
