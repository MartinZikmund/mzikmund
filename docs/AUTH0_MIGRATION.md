# Auth0 Migration Guide

This document provides instructions for configuring Auth0 authentication after migrating from Microsoft Entra ID (Azure AD).

## Overview

The application has been migrated from Microsoft Entra ID to Auth0 for authentication. This affects both:
- **Web Application** - ASP.NET Core API with JWT Bearer authentication
- **Uno Platform App** - Cross-platform mobile/desktop application

## Auth0 Setup

### 1. Create Auth0 Account and Tenant

1. Sign up for an Auth0 account at https://auth0.com
2. Create a new tenant or use an existing one
3. Note your tenant domain (e.g., `your-tenant.auth0.com`)

### 2. Configure API (Backend)

1. In Auth0 Dashboard, navigate to **Applications** → **APIs**
2. Click **Create API**
3. Configure:
   - **Name**: MZikmund API
   - **Identifier**: `https://api.mzikmund.dev` (or your API identifier)
   - **Signing Algorithm**: RS256
4. Save the **API Identifier** - you'll need it for configuration

### 3. Create Web Application (for testing)

1. Navigate to **Applications** → **Applications**
2. Click **Create Application**
3. Choose **Single Page Application** or **Machine to Machine** depending on your needs
4. Configure:
   - **Name**: MZikmund Web
   - **Allowed Callback URLs**: `https://localhost:5001/callback` (adjust for your environment)
   - **Allowed Logout URLs**: `https://localhost:5001`
   - **Allowed Web Origins**: `https://localhost:5001`

### 4. Create Native Application (for Uno Platform App)

1. Navigate to **Applications** → **Applications**
2. Click **Create Application**
3. Choose **Native**
4. Configure:
   - **Name**: MZikmund App
   - **Allowed Callback URLs**: 
     - `dev.mzikmund.MZikmund.App://callback` (Android/iOS)
     - `http://localhost/callback` (Desktop/WASM)
   - **Allowed Logout URLs**: 
     - `dev.mzikmund.MZikmund.App://logout`
     - `http://localhost/logout`
5. Note the **Client ID** - you'll need it for the app configuration

## Configuration

### Web Application

Update `appsettings.json` (or use User Secrets for development):

```json
{
  "Auth0": {
    "Domain": "your-tenant.auth0.com",
    "Audience": "https://api.mzikmund.dev"
  }
}
```

For production, set these as environment variables or in Azure App Configuration:
- `Auth0__Domain`
- `Auth0__Audience`

### Uno Platform App

Update `src/app/MZikmund.App.Core/Services/Account/AuthenticationConstants.cs`:

```csharp
public static class AuthenticationConstants
{
    public const string Domain = "your-tenant.auth0.com";
    public const string ClientId = "your-native-app-client-id";
    public const string Audience = "https://api.mzikmund.dev";
    
    public static string[] DefaultScopes { get; } = new string[]
    {
        "openid",
        "profile",
        "email",
        "offline_access"
    };
}
```

## Testing Authentication

### Web API

Test with a JWT token obtained from Auth0:

```bash
# Get a token (adjust for your Auth0 configuration)
curl --request POST \
  --url https://your-tenant.auth0.com/oauth/token \
  --header 'content-type: application/json' \
  --data '{
    "client_id":"your-client-id",
    "client_secret":"your-client-secret",
    "audience":"https://api.mzikmund.dev",
    "grant_type":"client_credentials"
  }'

# Test the API
curl --request GET \
  --url https://localhost:5001/api/v1/test \
  --header 'authorization: Bearer YOUR_ACCESS_TOKEN'
```

### Uno Platform App

1. Run the application
2. Navigate to a feature that requires authentication
3. The app should redirect to Auth0 login page
4. After successful authentication, you'll be redirected back to the app

## Platform-Specific Notes

### Android
- Ensure the redirect URI is properly configured in AndroidManifest.xml
- The format should be: `dev.mzikmund.MZikmund.App://callback`
- **Note**: The current `Auth0Browser` implementation is simplified. For production, implement proper deep linking to capture OAuth callbacks.

### iOS
- Add URL scheme to Info.plist
- The format should be: `dev.mzikmund.MZikmund.App`
- **Note**: The current `Auth0Browser` implementation is simplified. For production, implement proper deep linking to capture OAuth callbacks.

### Desktop/WASM
- Uses `http://localhost` for callback
- **Note**: The current `Auth0Browser` implementation is simplified. For production, implement a local HTTP listener to properly capture OAuth callbacks.

## Implementation Notes

### Browser Implementation

The current `Auth0Browser.cs` implementation is simplified for demonstration purposes. For production use, you should implement proper callback handling:

**Mobile (Android/iOS):**
1. Register custom URL schemes in platform-specific configuration
2. Implement deep link handlers in your app
3. Use `TaskCompletionSource` to wait for the callback
4. Extract authorization code from the callback URL
5. Return it to the Auth0Client

**Desktop/WASM:**
1. Start a local HTTP listener (e.g., `HttpListener` on a specific port)
2. Configure Auth0 callback URL to point to this listener
3. Wait for the HTTP request with the authorization code
4. Parse the authorization code from the query string
5. Return it to the Auth0Client

Example production implementation structure:
```csharp
public class ProductionAuth0Browser : IBrowser
{
    private TaskCompletionSource<string>? _callbackTcs;
    
    public async Task<BrowserResult> InvokeAsync(BrowserOptions options, CancellationToken ct)
    {
        _callbackTcs = new TaskCompletionSource<string>();
        
        // Start local listener or register deep link handler
        // Open browser
        // Wait for callback
        var callbackUrl = await _callbackTcs.Task;
        
        return new BrowserResult 
        { 
            ResultType = BrowserResultType.Success,
            Response = callbackUrl 
        };
    }
    
    // Method called by deep link handler or HTTP listener
    public void HandleCallback(string url)
    {
        _callbackTcs?.SetResult(url);
    }
}
```


## Troubleshooting

### Common Issues

1. **Invalid Audience Error**
   - Verify the Audience in your configuration matches the API Identifier in Auth0
   - Ensure the access token includes the correct audience claim

2. **Redirect URI Mismatch**
   - Check that all redirect URIs are properly configured in Auth0 Application settings
   - Ensure they match exactly (including trailing slashes)

3. **Token Validation Failed**
   - Verify the Domain is correct (should not include `https://`)
   - Check that the token is not expired
   - Ensure the signing algorithm matches (RS256)

4. **Browser Not Opening on Mobile**
   - Check that the platform-specific browser implementation is correct
   - Verify deep linking is properly configured

## Migration Checklist

- [ ] Create Auth0 account and tenant
- [ ] Configure Auth0 API
- [ ] Create Auth0 Applications (Web and Native)
- [ ] Update web application configuration
- [ ] Update Uno Platform app constants
- [ ] Configure callback URLs for all platforms
- [ ] Test authentication flow on web
- [ ] Test authentication flow on mobile platforms
- [ ] Test authentication flow on desktop
- [ ] Update deployment configurations with Auth0 settings

## Additional Resources

- [Auth0 Documentation](https://auth0.com/docs)
- [Auth0 ASP.NET Core SDK](https://auth0.com/docs/quickstart/backend/aspnet-core-webapi)
- [Auth0 Native Apps](https://auth0.com/docs/quickstart/native)
- [IdentityModel.OidcClient Documentation](https://identitymodel.readthedocs.io/en/latest/native/overview.html)
