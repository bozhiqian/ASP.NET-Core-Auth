using System.Collections.Generic;
using TodoViewModel;

namespace TodoWebClient.ViewModels
{
    public class TodoItemsIndexViewModel
    {
        public IEnumerable<TodoItemViewModel> Images { get; private set; }

        public TodoItemsIndexViewModel(List<TodoItemViewModel> images)
        {
           Images = images;
        }
    }
}
