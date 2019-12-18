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

## role-db
In this branch, we're using roles from our existing membership or authorization database. This is commonly used when your membership requirements can't be expressed via roles/groups, or when you need time to migrate to roles/groups.
- Here we need to add claims to our claimset on login. The `SecurityTokenValidated` openid event fires right _after_ the token has been validated, but _before_ the authentication cookie is written - it's the perfect time for us to add any additional claim data we'd like to have.
- By using `ClaimTypes.Role` (which equates to `http://schemas.microsoft.com/ws/2008/06/identity/claims/role`), aspnet Authorization will use these values when evaluating `User.IsInRole`. This makes it pretty simple to surface your existing authorization data in your claimset.
- Add `AuthorizeErrorAttribute`, which redirects to an error page if the user is not in a role specified for the action. Otherwise we end up in a redirect loop.
- Add an error page for showing arbitrary error messages.
- Add an `AuthenticationFailed` notification handler for openid.

## aad-resolve-group-names
Permissions required: `openid profile` (e.g., `https://graph.microsoft.com/User.Read`), `https://graph.microsoft.com/Directory.Read.All` :warning:
Back to our groups, if we get guids and want to resolve them to actual names, we need to do that via the graph. Until the new preview features arrived to send samAccountName in the claimset, this was an option to prevent modifying code from group name to aad group guid. 
- Since we need to query the graph for group names, our app needs more permissions to accomplish this. While `Groups.Read.All` seems to make sense on the surface, it actually grants access to significantly more than you'd expect - not only is it group metadata, it's data _within_ those groups. We'll use `Directory.Read.All` here, although that's still a lot of access. Our next iteration will address this.
- Once we've added the permission, we need to change our grant type to include an authorization_code, then swap that code for an access token. We're going to use it immediately so no need to worry about caching.
- We'll also need to add a client secret or certificate to our app to authenticate during the authorization_code callback. Azure offers a few different ways to store secrets - check out KeyVault and Managed Service Identity before putting a secret in your configuration files. 