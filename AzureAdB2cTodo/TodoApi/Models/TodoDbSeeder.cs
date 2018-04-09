using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TodoApi.Models
{
    public class TodoDbSeeder
    {
        private readonly TodoContext _context;
        public TodoDbSeeder(TodoContext context)
        {
            _context = context;
        }

        public void Seed()
        {
            if (!_context.TodoItems.Any())
            {
                _context.TodoItems.Add(new TodoItem { Name = "Catch up Microservices." });
                _context.TodoItems.Add(new TodoItem { Name = "Catch up Xamarin." });
                _context.TodoItems.Add(new TodoItem { Name = "Catch up Azure." });
                _context.SaveChanges();
            }
        }
    }
}
