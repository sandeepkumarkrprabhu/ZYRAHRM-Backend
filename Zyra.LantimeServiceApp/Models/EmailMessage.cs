using System;
using System.Collections.Generic;
using System.Text;

namespace Zyra.LantimeServiceApp.Models
{
    public class EmailMessage
    {
        public string To { get; set; }
        public string bcc { get; set; }
        public string ToName { get; set; }
        public string Subject { get; set; }
        public string PlainTextBody { get; set; }
        public string HtmlBody { get; set; }
    }
}
