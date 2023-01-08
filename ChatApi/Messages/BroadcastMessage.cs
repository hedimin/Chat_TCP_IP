using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatApi.Messages
{
    public class BroadcastMessage:IMessage
    {
        public string Text { get; init; }
        public string From { get; }

        public BroadcastMessage(string from, string text)
        {
            From = from;
            Text = text;
        }
    }
}
