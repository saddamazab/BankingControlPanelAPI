using System.ComponentModel.DataAnnotations;

public class AccountDto
{
    public int Id { get; set; }  

    [Required]
    public string AccountNumber { get; set; }

    [Required]
    public string Currency { get; set; }
}
