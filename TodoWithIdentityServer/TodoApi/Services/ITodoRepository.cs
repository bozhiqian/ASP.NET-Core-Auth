using System;
using System.Collections.Generic;
using TodoApi.Entities;

namespace TodoApi.Services
{
    public interface ITodoRepository
    {
        IEnumerable<TodoItem> GetTodoItems(string ownerId);
        bool IsTodoItemOwner(long id, string ownerId);
        TodoItem GetTodoItem(long id);
        bool TodoItemExists(long id);
        void AddTodoItem(TodoItem todoItem);
        void UpdateTodoItem(TodoItem todoItem);
        void DeleteTodoItem(TodoItem todoItem);
        bool Save();
    }
}
