using AadModernization.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace AadModernization
{
    public class AuthorizeErrorAttribute : AuthorizeAttribute
    {
        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            if (!filterContext.HttpContext.User.Identity.IsAuthenticated)
            {
                base.HandleUnauthorizedRequest(filterContext);
                return;
            }
            else if (!Roles.Split(',').Any(x => filterContext.HttpContext.User.IsInRole(x)))
            {
                var message = $"so valiantly you tried, but alas, it wasn't meant to be. things you need: {Roles}, things you aren't: {Roles}";
                filterContext.Result = new RedirectResult($"~/Home/Error?e={message}");
                return;
            }
            base.HandleUnauthorizedRequest(filterContext);
        }
    }
}