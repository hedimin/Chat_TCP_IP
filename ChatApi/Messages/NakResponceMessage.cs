using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatApi.Messages
{
    public sealed class NakResponceMessage:IMessage
    {
        public Guid RequestId { get; }

        public string Message { get; }

        public NakResponceMessage(Guid requestId, string message)
        {
            RequestId = requestId;
            Message = message;
        }
    }
}
