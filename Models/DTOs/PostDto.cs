using System.ComponentModel.DataAnnotations;

namespace MiniBlog.Models.DTOs;

public class PostDto
{
        [Required]
        public string Title { get; set; }
        [Required,MaxLength(300)]
        public string Content { get; set; }
        
}