using System.ComponentModel.DataAnnotations;

public class AddressDto
{
    [Required]
    [MaxLength(100)]
    public string Country { get; set; }

    [Required]
    [MaxLength(100)]
    public string City { get; set; }

    [Required]
    [MaxLength(200)]
    public string Street { get; set; }

    [Required]
    [MaxLength(20)]
    public string ZipCode { get; set; }
}