using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Sdk.Dotnet
{
    public interface LiveKitEvent { }

    public interface RoomEvent : LiveKitEvent
    {
    }

    public interface ParticipantEvent : LiveKitEvent
    {
    }

    public interface TrackEvent : LiveKitEvent
    {
    }

    public interface EngineEvent : LiveKitEvent
    {
    }

    public interface SignalEvent : LiveKitEvent
    {
    }

    public class RoomConnectedEvent : RoomEvent
    {

    }
}
