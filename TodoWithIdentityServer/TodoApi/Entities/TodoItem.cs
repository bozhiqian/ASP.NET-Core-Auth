using System.ComponentModel.DataAnnotations;

namespace TodoApi.Entities
{
    public class TodoItem
    {
        [Key]
        public long Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }
        public bool IsComplete { get; set; }

        [Required]
        [MaxLength(50)]
        public string OwnerId { get; set; }
    }
}
