using System;
namespace P2Dev.ApiClient
{
    public class ServerSideException : Exception
    {
        public String Content { get; set; }
        public ServerSideException(string content)
        {
            this.Content = content;
        }
    }
}
