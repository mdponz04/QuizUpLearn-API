using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using BusinessLogic.Interfaces;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace QuizUpLearn.API.Attributes
{
    public class SubscriptionAndRoleAuthorizeAttribute : Attribute, IAsyncActionFilter
    {
        // Subscription requirements
        public bool RequireAiFeatures { get; set; } = false;
        public bool RequirePremiumContent { get; set; } = false;
        public bool CheckRemainingUsage { get; set; } = false;
        
        // Role requirements
        public string[] AllowedRoles { get; set; } = [];
        public string[] AllowedPermissions { get; set; } = [];
        
        // Bypass subscription check for certain roles
        public bool AllowBypass { get; set; } = true;

        public SubscriptionAndRoleAuthorizeAttribute(params string[] allowedRoles)
        {
            AllowedRoles = allowedRoles ?? [];
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var subscriptionService = context.HttpContext.RequestServices.GetRequiredService<ISubscriptionService>();
            var subscriptionPlanService = context.HttpContext.RequestServices.GetRequiredService<ISubscriptionPlanService>();
            var userService = context.HttpContext.RequestServices.GetRequiredService<IUserService>();
            var roleService = context.HttpContext.RequestServices.GetRequiredService<IRoleService>();

            // Extract user ID from claims
            var accountIdClaim = context.HttpContext.User?.FindFirst("UserId")?.Value
                ?? context.HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? context.HttpContext.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            if (string.IsNullOrEmpty(accountIdClaim) || !Guid.TryParse(accountIdClaim, out var accountId))
            {
                context.Result = new UnauthorizedObjectResult(new { Success = false, Message = "Invalid user authentication." });
                return;
            }

            // Get roleId from claims
            var roleIdClaim = context.HttpContext.User?.FindFirst("roleId")?.Value;
            if (string.IsNullOrEmpty(roleIdClaim) || !Guid.TryParse(roleIdClaim, out var roleId))
            {
                context.Result = new UnauthorizedObjectResult(new { Success = false, Message = "Role information not found in token." });
                return;
            }

            try
            {
                // Get role details first
                var role = await roleService.GetRoleByIdAsync(roleId);
                if (role == null || !role.IsActive)
                {
                    context.Result = new JsonResult(new
                    {
                        Success = false,
                        Message = "Invalid or inactive role."
                    })
                    {
                        StatusCode = StatusCodes.Status403Forbidden
                    };
                    return;
                }

                // Check if user has required role
                bool hasRequiredRole = AllowedRoles.Length == 0 || AllowedRoles.Contains(role.RoleName, StringComparer.OrdinalIgnoreCase);

                // Check if user has required permissions
                bool hasRequiredPermission = true;
                if (AllowedPermissions.Length > 0)
                {
                    var userPermissions = role.Permissions?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(p => p.Trim()) ?? [];
                    
                    hasRequiredPermission = AllowedPermissions.Any(required => 
                        userPermissions.Contains(required, StringComparer.OrdinalIgnoreCase));
                }

                if (!hasRequiredRole || !hasRequiredPermission)
                {
                    context.Result = new JsonResult(new
                    {
                        Success = false,
                        Message = $"Access denied. Required role: {string.Join(", ", AllowedRoles)} or permissions: {string.Join(", ", AllowedPermissions)}"
                    })
                    {
                        StatusCode = StatusCodes.Status403Forbidden
                    };
                    return;
                }

                // Check if admin bypass is allowed and user is admin
                bool isAdmin = role.RoleName.Equals("Administrator", StringComparison.OrdinalIgnoreCase);
                bool isMod = role.RoleName.Equals("Moderator", StringComparison.OrdinalIgnoreCase);
                bool skipSubscriptionCheck = AllowBypass && (isAdmin || isMod);

                // Store role info in HttpContext
                context.HttpContext.Items["UserRole"] = role.RoleName;
                context.HttpContext.Items["UserPermissions"] = role.Permissions;
                context.HttpContext.Items["IsAdmin"] = isAdmin;
                context.HttpContext.Items["IsMod"] = isMod;

                // Skip subscription check for admin if bypass is enabled
                if (!skipSubscriptionCheck)
                {
                    // Get user from account ID
                    var user = await userService.GetByAccountIdAsync(accountId);
                    if (user == null)
                    {
                        context.Result = new UnauthorizedObjectResult(new { Success = false, Message = "User not found." });
                        return;
                    }

                    // Get user's subscription
                    var subscription = await subscriptionService.GetByUserIdAsync(user.Id);
                    context.HttpContext.Items["RemainingUsage"] = subscription!.AiGenerateQuizSetRemaining;

                    if (subscription == null)
                    {
                        context.Result = new JsonResult(new
                        {
                            Success = false,
                            Message = "No active subscription found."
                        })
                        {
                            StatusCode = StatusCodes.Status403Forbidden
                        };
                        return;
                    }

                    // Check if subscription is expired
                    if (subscription.EndDate.HasValue && subscription.EndDate.Value < DateTime.UtcNow)
                    {
                        context.Result = new JsonResult(new
                        {
                            Success = false,
                            Message = "Subscription has expired."
                        })
                        {
                            StatusCode = StatusCodes.Status403Forbidden
                        };
                        return;
                    }

                    // Get subscription plan details
                    var subscriptionPlan = await subscriptionPlanService.GetByIdAsync(subscription.SubscriptionPlanId);
                    if (subscriptionPlan == null || !subscriptionPlan.IsActive)
                    {
                        context.Result = new JsonResult(new
                        {
                            Success = false,
                            Message = "Invalid or inactive subscription plan."
                        })
                        {
                            StatusCode = StatusCodes.Status403Forbidden
                        };
                        return;
                    }

                    // Check AI features access
                    if (RequireAiFeatures && !subscriptionPlan.CanAccessAiFeatures)
                    {
                        context.Result = new JsonResult(new
                        {
                            Success = false,
                            Message = "Your subscription plan does not include AI features."
                        })
                        {
                            StatusCode = StatusCodes.Status403Forbidden
                        };
                        return;
                    }

                    // Check premium content access
                    if (RequirePremiumContent && !subscriptionPlan.CanAccessPremiumContent)
                    {
                        context.Result = new JsonResult(new
                        {
                            Success = false,
                            Message = "Your subscription plan does not include premium content access."
                        })
                        {
                            StatusCode = StatusCodes.Status403Forbidden
                        };
                        return;
                    }

                    // Check remaining AI usage
                    if (CheckRemainingUsage && subscription.AiGenerateQuizSetRemaining <= 0)
                    {
                        context.Result = new JsonResult(new
                        {
                            Success = false,
                            Message = "You have reached your AI quiz generation limit."
                        })
                        {
                            StatusCode = StatusCodes.Status403Forbidden
                        };
                        return;
                    }

                    // Store subscription info in HttpContext
                    context.HttpContext.Items["UserId"] = user.Id;
                    context.HttpContext.Items["Subscription"] = subscription;
                    context.HttpContext.Items["SubscriptionPlan"] = subscriptionPlan;
                }
                else
                {
                    var user = await userService.GetByAccountIdAsync(accountId);
                    if (user == null)
                    {
                        context.Result = new UnauthorizedObjectResult(new { Success = false, Message = "User not found." });
                        return;
                    }
                    context.HttpContext.Items["UserId"] = user.Id;
                }

                await next();
            }
            catch (KeyNotFoundException)
            {
                context.Result = new JsonResult(new
                {
                    Success = false,
                    Message = "Role not found."
                })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return;
            }
            catch (Exception ex)
            {
                context.Result = new JsonResult(new
                {
                    Success = false,
                    Message = $"Error during authorization: {ex.Message}"
                })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
                return;
            }
        }
    }
}