using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatApi.Messages
{
    public sealed class AckResponceMessage : IMessage
    {
        public Guid RequestId { get; }

        public AckResponceMessage(Guid requestId)
        {
            RequestId = requestId;
        }
    }
}
