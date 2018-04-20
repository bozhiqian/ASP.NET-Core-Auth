using Microsoft.AspNetCore.Authorization;

namespace TodoApi.Authorization
{
    public class MustOwnTodoItemRequirement : IAuthorizationRequirement
    {
        public MustOwnTodoItemRequirement()
        {

        }
    }
}
