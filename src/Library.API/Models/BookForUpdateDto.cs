using System.ComponentModel.DataAnnotations;

namespace Library.API.Models
{
	public class BookForUpdateDto : BookForManipulationDto
	{
		[Required(ErrorMessage = "You need to enter a description.")]
		public override string Description { get => base.Description; set => base.Description = value; }
	}
}
