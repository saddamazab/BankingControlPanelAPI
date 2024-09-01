using System.ComponentModel.DataAnnotations;

public class Client
{
    public int Id { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(60)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(60)]
    public string LastName { get; set; } = string.Empty;

    [Required]
   // [RegularExpression(@"^\+[1-9]\d{1,14}$", ErrorMessage = "Invalid phone number format.")]
    public string MobileNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(11, MinimumLength = 11)]
    public string PersonalId { get; set; } = string.Empty;

    [Required]
    public Sex Sex { get; set; }

    [Required]
    public int AddressId { get; set; } // Foreign key to Address


    public Address Address { get; set; } // Navigation property


    public List<Account> Accounts { get; set; } = new List<Account>();

    public string ProfilePhoto { get; set; } // Optional
}
public enum Sex
{
    Male = 0,
    Female = 1
}