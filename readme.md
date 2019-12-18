# aad-modernization

Sample repo taking a netfx 4.5 app from windows integrated auth (AD, nt, kerb) to Azure AD in different stages.

## aad-auth
Permissions required: `openid profile` (e.g., `https://graph.microsoft.com/User.Read`)
In this branch, we're only concerned with getting all of the right pieces in place for basic OpenIDConnect. We need to:
- Add `Microsoft.Owin.Security.OpenIDConnect`, `Microsoft.Owin.Security.Cookies` and `Microsoft.Owin.SystemWeb` packages
- Add a `Startup` class, decorated to be `OwinStartup`, where we configure our OpenIDConnect parameters
- Add an `Account` controller with challenge mechanisms to redirect users to the right endpoint.
- Map the `preferred_username` claim (or another claim of your liking) to the Name property of the identity. This ensures things like `User.Identity.Name` resolve to what you want. 
- Add our AAD app registration configuration details to `web.config`
- Remove Windows Integrated Authentication &amp; enable Anonymous Authentication on IIS/IIS Express

By using the `[Authorize]` tag in our `Claims` controller action (on Home), we're asking aspnet to ensure an authenticated user for that action. We can also use the `SignIn` method on the Account controller to the same effect. 

## aad-groups
Permissions required: `openid profile` (e.g., `https://graph.microsoft.com/User.Read`)
In this branch, we add some authorization data using groups. This is a common practice with AD and other directory systems on-prem, where specific groups are used for controlling what areas users have access to use. 
- Add a group to the protected `Claims` method on the `Home` controller. Note the group is a GUID - for groups from Azure AD, you'll get the GUID only. For groups synced from on-prem AD, there is a preview feature (as of 12/19) to get group names in the claims, simplifying development. See [here](https://docs.microsoft.com/en-us/azure/active-directory/hybrid/how-to-connect-fed-group-claims) for more info.
- We add `"groupMembershipClaims": "all"` to our Azure AD app manifest to ensure the group claims are returned.