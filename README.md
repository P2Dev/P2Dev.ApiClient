# P2Dev.ApiClient

The ApiClient package provides a streamlined, easy-to-use HTTP client for interacting with RESTful APIs following the P2.Dev API Style Specification. It simplifies the process of making API calls, handling authentication via JSON Web Tokens (JWT), refreshing tokens, and pre-/post-processing of HTTP requests and responses.

## Features
- **Automatic JWT Authentication:** Automatically adds JWTs to requests when provided.
- **Token Refreshing:** Seamlessly refreshes tokens when necessary.
- **Error Handling:** Provides custom exceptions for various HTTP status codes, facilitating better error management.

## Usage

### Setting Up

#### Token Provider: AWS Cognito User Pools
Setting Up with AWS Cognito The `P2Dev.ApiClient` package includes a default token provider for AWS Cognito. To utilize this feature, you will need to configure the `CognitoTokenProvider` with the appropriate details from your AWS Cognito setup.

##### Required Information
To set up the `CognitoTokenProvider`, you will need the following information: 
- `ClientId`: The client ID of your app client in the Cognito user pool. 
- `RedirectUri`: The URI where the OAuth response can be sent and received by your app. 
- `PoolName`: The name of your Cognito user pool. 
- `Region`: The AWS region where your Cognito user pool is located. 
- `PoolId`: The ID of your Cognito user pool. 

##### Configuration Example 
Here is a basic example of how to configure and use the `CognitoTokenProvider` within your application: 
```csharp 
using P2Dev.ApiClient.Cognito; 
// Configure the Cognito Token Provider 
	CognitoTokenProvider tokenProvider = new CognitoTokenProvider { 
	ClientId = "YOUR_CLIENT_ID", 
	RedirectUri = "YOUR_REDIRECT_URI", 
	PoolName = "YOUR_POOL_NAME", 
	Region = "YOUR_COGNITO_REGION", 
	PoolId = "YOUR_POOL_ID" 
}; 

// Initialize the ApiClient with the token provider
ApiClientBase apiClient = new ApiClientBase { 
	TokenProvider = tokenProvider, 
	BaseURL = "YOUR_API_BASE_URL" 
}; 
// Now you can use the apiClient instance to make authenticated requests
```

#### Token Provider: Implement Your Own
First, implement the `ITokenProvider` interface to handle token acquisition and refreshing in your application:

```csharp
public class MyTokenProvider : ITokenProvider {
// Implementation details here 
}
```

Initialize the `ApiClientBase` with your `ITokenProvider` and base URL:

```csharp
var apiClient = new ApiClientBase {
    TokenProvider = new MyTokenProvider(),
    BaseURL = "https://api.example.com/" 
};
```

### Making Requests

Use the `SendAsync<T>` method to make API calls. Specify the HTTP method, endpoint, and optionally, a request body and whether to retry on authentication failure:

```csharp
var result = await apiClient.SendAsync<MyResponseType>(HttpMethod.Get, apiClient.GenURL("/my-endpoint"));`
```

### Handling Tokens and Refreshing

The ApiClient handles adding the JWT to requests and refreshing tokens as needed. Implement the `GetTokens` and `RefreshTokens` methods in your `ITokenProvider` to integrate with your token service.