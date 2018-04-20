using System;
using System.Collections.Generic;
using System.Net.Mime;

namespace TodoApi.Entities
{
    public static class TodoContextContextSeeder
    {
        public static void SeedDataForTodoContext(this TodoContext context)
        {
            // first, clear the database.  This ensures we can always start 
            // fresh with each demo.  Not advised for production environments, obviously :-)

            context.TodoItems.RemoveRange(context.TodoItems);
            context.SaveChanges();

            // init seed data
            var todoItems = new List<TodoItem>()
            {
                new TodoItem { Name = "Catch up Microservices.", OwnerId = "d860efca-22d9-47fd-8249-791ba61b07c7"},
                new TodoItem { Name = "Catch up Xamarin.", OwnerId = "d860efca-22d9-47fd-8249-791ba61b07c7" },
                new TodoItem { Name = "Catch up Azure.", OwnerId = "d860efca-22d9-47fd-8249-791ba61b07c7" },
            };

            context.TodoItems.AddRange(todoItems);
            context.SaveChanges();
        }
    }
}
