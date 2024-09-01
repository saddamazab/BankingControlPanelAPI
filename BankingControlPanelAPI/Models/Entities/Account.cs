using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Account
{
    public int Id { get; set; }

    [Required]
    public string AccountNumber { get; set; }

    public string Currency { get; set; }

    public int ClientId { get; set; }

}
