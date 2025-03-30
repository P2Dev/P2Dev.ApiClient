using System.Threading.Tasks;

namespace P2Dev.ApiClient
{
    public class MockTokenProvider : ITokenProvider
    {
        public Token Tokens { get; set; } = null;
        public async Task<Token> GetTokens(string authCode)
        {
            return Tokens;
        }

        public async Task<Token> RefreshTokens()
        {
            return Tokens;
        }
    }
}