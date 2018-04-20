using System;
using System.Collections.Generic;
using System.Linq;
using TodoApi.Entities;

namespace TodoApi.Services
{
    public class TodoRepository : ITodoRepository, IDisposable
    {
        TodoContext _context;

        public TodoRepository(TodoContext context)
        {
            _context = context;
        }

        public IEnumerable<TodoItem> GetTodoItems(string ownerId)
        {
            return _context.TodoItems.Where(i => i.OwnerId == ownerId).OrderBy(i => i.Name).ToList();
        }

        public bool IsTodoItemOwner(long todoItemId, string ownerId)
        {
            return _context.TodoItems.Any(i => i.Id == todoItemId && i.OwnerId == ownerId);
        }

        public TodoItem GetTodoItem(long id)
        {
            return _context.TodoItems.FirstOrDefault(i => i.Id == id);
        }

        public bool TodoItemExists(long id)
        {
            return _context.TodoItems.Any(i => i.Id == id);
        }

        public void AddTodoItem(TodoItem todoItem)
        {
            _context.TodoItems.Add(todoItem);
        }

        public void UpdateTodoItem(TodoItem todoItem)
        {
            _context.TodoItems.Update(todoItem);
        }

        public void DeleteTodoItem(TodoItem todoItem)
        {
            _context.TodoItems.Remove(todoItem);
        }

        public bool Save()
        {
            return (_context.SaveChanges() >= 0);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_context != null)
                {
                    _context.Dispose();
                    _context = null;
                }

            }
        }     
    }
}
