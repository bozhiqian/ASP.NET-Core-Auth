using System;
using System.Collections.Generic;
using System.Text;

namespace TodoXamarin.Events
{
    public class AlertEventArgs : EventArgs
    {
        public AlertEventArgs()
        {

        }

        public AlertEventArgs(string message) : this()
        {
            Message = message;
        }
        public string Message { get; set; }
        public string Title { get; set; }
        public string Accept { get; set; }
        public string Cancel { get; set; }

    }
}
