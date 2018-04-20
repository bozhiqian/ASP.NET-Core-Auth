using System;
using System.ComponentModel.DataAnnotations;

namespace TodoWebClient.ViewModels
{
    public class EditTodoItemViewModel
    {
        [Required]
        public long Id { get; set; }

        public string Name { get; set; }
        public bool IsComplete { get; set; }
    }
}
