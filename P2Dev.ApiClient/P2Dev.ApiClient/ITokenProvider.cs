using System;
using System.Threading.Tasks;

namespace P2Dev.ApiClient
{
	public interface ITokenProvider
	{
		Token Tokens { get; set; }

		Task<Token> RefreshTokens();
		Task<Token> GetTokens(string code);
	}
}

