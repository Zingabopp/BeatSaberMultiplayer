using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatSaberMultiplayerLite
{
    public class ServerHubException : Exception
    {
        public ServerHubExceptionType ServerHubExceptionType { get; private set; }
        public ServerHubException()
        {

        }
        public ServerHubException(string message, ServerHubExceptionType serverHubExceptionType)
            : base(message)
        {
            ServerHubExceptionType = serverHubExceptionType;
        }
        public ServerHubException(string message, Exception innerException, ServerHubExceptionType serverHubExceptionType)
            : base(message, innerException)
        {
            ServerHubExceptionType = serverHubExceptionType;
        }
    }

    public enum ServerHubExceptionType
    {
        None = 0,
        VersionMismatch = 1,
        MalformedPacket = 2,
        PlayerNotWhitelisted = 3,
        PlayerBanned = 4
    }
}
