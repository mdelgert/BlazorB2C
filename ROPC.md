# Troubleshooting Azure AD B2C ROPC "non_parsable_oauth_error"

You are now using the correct authority format for Azure AD B2C ROPC, but you are still getting a `non_parsable_oauth_error`. This means the B2C endpoint is returning an error that MSAL.NET cannot parse, usually because of one of the following:

## 1. The ROPC user flow is not enabled or not supported
- ROPC is only supported for local accounts (not social or federated accounts).
- ROPC user flow must be created in the B2C portal and must be of type "Resource owner password credentials".
- Check that the policy name is correct and matches the one in the portal.

## 2. The application is not enabled for ROPC
- In the Azure portal, go to your B2C tenant → User flows → Select your ROPC flow (e.g., `B2C_1_ROPC`).
- Make sure the flow is enabled and configured.

## 3. The user is not a local account
- ROPC will fail for users created via Google, Facebook, Microsoft, etc.
- Only users created directly in the B2C directory (local accounts) can use ROPC.

## 4. The app registration is missing required API permissions
- Go to Azure AD B2C → App registrations → Your app.
- Under "API permissions", ensure you have:
  - `openid`
  - `offline_access`
  - `https://graph.microsoft.com/User.Read`
- Click "Grant admin consent" after adding.

## 5. The app registration is missing redirect URIs or is not a public client
- Under "Authentication", ensure "Allow public client flows" is enabled.
- You do not need a redirect URI for ROPC, but the app must be marked as a public client.

## 6. The user has MFA enabled
- ROPC does not support MFA. If the user is required to do MFA, ROPC will fail.

## 7. Check the error details
- The actual error is in the HTTP response body, but MSAL.NET cannot parse it.
- Catch the exception and log `ex.Message`, `ex.InnerException`, and (if available) `ex.ResponseBody` or `ex.RawResponse`.

---

### What to do next
- Double-check the user flow type and name in the Azure portal.
- Ensure the user is a local account and does not have MFA enabled.
- Check your app registration for the correct permissions and public client setting.
- Add extra logging to print the raw response from the exception.