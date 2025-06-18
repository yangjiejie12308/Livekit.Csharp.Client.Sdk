using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Sdk.Dotnet.Internal
{
    internal class IceCandidate
    {
        public string candidate { get; set; }
        public string sdpMid { get; set; }
        public ushort sdpMLineIndex { get; set; }
    }
}
