using System;
namespace P2Dev.ApiClient
{
    public class RestException : Exception
    {
        public int Code { get; set; }
    }
}
