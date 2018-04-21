using System.Collections.Generic;
using TodoViewModel;

namespace TodoWebClient.ViewModels
{
    public class TodoItemsIndexViewModel
    {
        public IEnumerable<TodoItemViewModel> TodoItemViewModels { get; private set; }

        public TodoItemsIndexViewModel(List<TodoItemViewModel> todoItemViewModels)
        {
            TodoItemViewModels = todoItemViewModels;
        }
    }
}
