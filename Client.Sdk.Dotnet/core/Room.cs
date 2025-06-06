using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Client.Sdk.Dotnet.managers;

namespace Client.Sdk.Dotnet.core
{
    public class Room : EventsEmittableBase<RoomEvent>
    {
        public Engine Engine { get; private set; }
    }
}
