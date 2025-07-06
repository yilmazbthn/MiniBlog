using System.ComponentModel.DataAnnotations;

namespace MiniBlog.Models.DTOs;


public class CommentDto
{
        [Required,MaxLength(300)]
        public string Text { get; set; }
}