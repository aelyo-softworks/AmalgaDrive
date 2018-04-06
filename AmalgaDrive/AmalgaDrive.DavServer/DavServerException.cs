using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace AmalgaDrive.DavServer
{
    [Serializable]
    public class DavServerException : Exception
    {
        public const string Prefix = "DAV";

        public DavServerException()
            : base(Prefix + "0001: Dav Server exception.")
        {
        }

        public DavServerException(string message)
            : base(message)
        {
        }

        public DavServerException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public DavServerException(Exception innerException)
            : base(null, innerException)
        {
        }

        protected DavServerException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public static int GetCode(string message)
        {
            if (message == null)
                return -1;

            if (!message.StartsWith(Prefix, StringComparison.Ordinal))
                return -1;

            int pos = message.IndexOf(':', Prefix.Length);
            if (pos < 0)
                return -1;

            if (int.TryParse(message.Substring(Prefix.Length, pos - Prefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out int i))
                return i;

            return -1;
        }

        public int Code => GetCode(Message);
    }
}
