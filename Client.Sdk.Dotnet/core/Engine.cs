using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Client.Sdk.Dotnet.managers;

namespace Client.Sdk.Dotnet.core
{
    public class Engine : EventsEmittableBase<EngineEvent>, IDisposable
    {
        private const int maxRetryDelay = 7000;

        private static readonly int[] defaultRetryDelaysInMs =
        {
           0,
           300,
           2 * 2 * 300,
           3 * 3 * 300,
           4 * 4 * 300,
           maxRetryDelay,
           maxRetryDelay,
           maxRetryDelay,
           maxRetryDelay,
           maxRetryDelay,
       };

        private string _lossyDCLabel = "_lossy";

        private string _reliableDCLabel = "_reliable";

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
