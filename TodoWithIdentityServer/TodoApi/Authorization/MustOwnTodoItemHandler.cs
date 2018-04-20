using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using TodoApi.Services;

namespace TodoApi.Authorization
{
    public class MustOwnTodoItemHandler : AuthorizationHandler<MustOwnTodoItemRequirement>
    {
        private readonly ITodoRepository _todoRepository;

        public MustOwnTodoItemHandler(ITodoRepository todoRepository)
        {
            _todoRepository = todoRepository;
        }

        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context, MustOwnTodoItemRequirement requirement)
        {
            var filterContext = context.Resource as AuthorizationFilterContext;
            if (filterContext == null)
            {
                context.Fail();
                return Task.FromResult(0);
            }

            var todoItemIdString = filterContext.RouteData.Values["id"].ToString();
            long todoItemId;
            if (!long.TryParse(todoItemIdString, out todoItemId))
            {
                context.Fail();
                return Task.CompletedTask;
            }

            // get the sub claim
            var ownerId = context.User.Claims.FirstOrDefault(c => c.Type == "sub").Value;

            if (!_todoRepository.IsTodoItemOwner(todoItemId, ownerId))
            {
                context.Fail();
                return Task.FromResult(0);
            }

            context.Succeed(requirement);
            return Task.FromResult(0);
        }
    }
}
