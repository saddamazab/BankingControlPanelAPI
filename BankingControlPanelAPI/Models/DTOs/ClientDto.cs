using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public class ClientDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    [MaxLength(60)]
    public string FirstName { get; set; }

    [Required]
    [MaxLength(60)]
    public string LastName { get; set; }

    [Required]
   // [RegularExpression(@"^\+[1-9]\d{1,14}$", ErrorMessage = "Invalid phone number format.")]
    public string MobileNumber { get; set; }

    [Required]
    [StringLength(11, MinimumLength = 11, ErrorMessage = "Personal ID must be exactly 11 characters.")]
    public string PersonalId { get; set; }

    [Required]
    public int Sex { get; set; }  // Enum represented as an integer

    [Required]
    public AddressDto Address { get; set; } // DTO for Address


    public List<AccountDto> Accounts { get; set; } = new List<AccountDto>();

    public string? ProfilePhoto { get; set; } // Optional

}

