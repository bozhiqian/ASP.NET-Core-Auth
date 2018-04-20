using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace TodoWebClient.ViewModels
{
    public class AddTodoItemViewModel
    {
        [Required]
        public string Name { get; set; }
    }
}
