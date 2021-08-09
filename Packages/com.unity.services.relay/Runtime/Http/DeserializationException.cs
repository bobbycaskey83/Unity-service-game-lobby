using System;

namespace Unity.Services.Relay.Http
{
    [Serializable]
    public class DeserializationException : Exception
    {
        public DeserializationException() : base()
        {
        }

        public DeserializationException(string message) : base(message)
        {
        }

        DeserializationException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
