using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatApi.Messages
{
    public class SetNickNameMessage:IMessage
    {
        public Guid RequestId { get; }
        public string NickName { get; }

        public SetNickNameMessage(Guid requestId, string nickName)
        {
            RequestId = requestId;
            NickName = nickName;
        }
    }
}
