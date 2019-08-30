using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MailKit_MailEntegrasyon.Models
{
    public class MailAccount
    {
        public MailAccount()
        {
            ImapSettings = new MailServer();
            SmtpSettings = new MailServer();
        }
        public string Email { get; set; }
        public string Password { get; set; }
        public MailServer ImapSettings { get; set; }
        public MailServer SmtpSettings { get; set; }
    }

    public class MailServer
    {
        public MailServer()
        {
            Ssl = true;
        }
        public string Host { get; set; }
        public int Port { get; set; }
        public bool Ssl { get; set; }
    }
    
}