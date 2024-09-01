using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PhoneNumbers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class ClientsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ClientsController> _logger;

    // In-memory cache for search history
    private static Queue<(string search, string sort, int page, int pageSize)> searchHistoryCache = new Queue<(string, string, int, int)>();


    public ClientsController(ApplicationDbContext context, ILogger<ClientsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // Get all clients with filtering, sorting, and paging
    [HttpGet]
    public async Task<IActionResult> GetClients(string search = "", string sort = "id", int page = 1, int pageSize = 10)
    {
        // Save search parameters to cache 
        searchHistoryCache.Enqueue((search, sort, page, pageSize));
        if (searchHistoryCache.Count > 3)
        {
            searchHistoryCache.Dequeue();
        }

        // Save search parameters to the database 
         var searchParameter = new SearchParameter
         {
             Search = search,
             Sort = sort,
             Page = page,
             PageSize = pageSize
         };        
        _context.SearchParameters.Add(searchParameter);
        await _context.SaveChangesAsync();
      

        // Query to get clients
        var query = _context.Clients
                     .Include(c => c.Address)  
                    // .Include(c => c.Accounts)    //uncomment to show accounts
                     .AsQueryable();

        
        //  filtering 
        if (!string.IsNullOrEmpty(search))
        {
            var lowerSearch = search.ToLower(); 
            query = query.Where(c => c.Email.ToLower().Contains(lowerSearch) ||
                                     c.FirstName.ToLower().Contains(lowerSearch) ||
                                     c.LastName.ToLower().Contains(lowerSearch));
        }

        // Sorting
        query = sort switch
        {
            "email" => query.OrderBy(c => c.Email),
            "firstname" => query.OrderBy(c => c.FirstName),
            "lastname" => query.OrderBy(c => c.LastName),
            _ => query.OrderBy(c => c.Id),
        };

        // Paging
        var clientsWithFullDetails = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();


        return Ok(clientsWithFullDetails);
    }



   
    // Add a new client with accounts using DTO
    [HttpPost("addClientWithAccounts")]
    public async Task<IActionResult> AddClientWithAccounts([FromBody] ClientDto clientDto)
    {
        if (clientDto == null)
        {
            return BadRequest("Client data is required.");
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Check for existing clients with the same email, mobile number, or personal ID
        bool emailExists = await _context.Clients.AnyAsync(c => c.Email == clientDto.Email);
        bool mobileNumberExists = await _context.Clients.AnyAsync(c => c.MobileNumber == clientDto.MobileNumber);
        bool personalIdExists = await _context.Clients.AnyAsync(c => c.PersonalId == clientDto.PersonalId);

        if (emailExists)
        {
            return BadRequest($"A client with the email {clientDto.Email} already exists.");
        }

        if (mobileNumberExists)
        {
            return BadRequest($"A client with the mobile number {clientDto.MobileNumber} already exists.");
        }

        if (personalIdExists)
        {
            return BadRequest($"A client with the personal ID {clientDto.PersonalId} already exists.");
        }

        // Validate the mobile number
        var phoneNumberUtil = PhoneNumberUtil.GetInstance();
        try
        {
            var parsedNumber = phoneNumberUtil.Parse(clientDto.MobileNumber, "SA"); 
            if (!phoneNumberUtil.IsValidNumber(parsedNumber))
            {
                return BadRequest("Invalid mobile number format   .");
            }
        }
        catch (NumberParseException)
        {
            return BadRequest("Invalid mobile number format    .");
        }


        try
        {
            // Start a transaction to ensure both client and accounts are saved together
            using var transaction = await _context.Database.BeginTransactionAsync();

            // Initialize and save the Address first to get the AddressId
            var address = new Address
            {
                Country = clientDto.Address.Country,
                City = clientDto.Address.City,
                Street = clientDto.Address.Street,
                ZipCode = clientDto.Address.ZipCode
            };
            _context.Addresses.Add(address);
            await _context.SaveChangesAsync();

            // Map DTO to domain model
            var client = new Client
            {
                Email = clientDto.Email,
                FirstName = clientDto.FirstName,
                LastName = clientDto.LastName,
                MobileNumber = clientDto.MobileNumber,
                PersonalId = clientDto.PersonalId,
                Sex = (Sex)clientDto.Sex,
                AddressId = address.Id, 
                ProfilePhoto = clientDto.ProfilePhoto ?? "defaultProfilePhotoUrl", 
                Accounts = clientDto.Accounts.Select(a => new Account
                {
                    AccountNumber = a.AccountNumber,
                    Currency = a.Currency
                }).ToList()
            };


            // Add the client first
            _context.Clients.Add(client);
            await _context.SaveChangesAsync();

            // Commit the transaction
            await transaction.CommitAsync();

            return CreatedAtAction(nameof(GetClient), new { id = client.Id }, client);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "An error occurred while saving the client and accounts.");
            return StatusCode(500, "An error occurred while saving the client and accounts. Please try again later.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while saving the client and accounts.");
            return StatusCode(500, "An unexpected error occurred. Please try again later.");
        }
    }

    // Method to retrieve client by ID
    [HttpGet("{id}")]
    public async Task<IActionResult> GetClient(int id)
    {
        var client = await _context.Clients.Include(c => c.Accounts).FirstOrDefaultAsync(c => c.Id == id);
        if (client == null)
        {
            return NotFound($"Client with ID {id} not found.");
        }
        return Ok(client);
    }



    // update existing client
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateClient(int id, [FromBody] ClientDto clientDto)
    {
        if (clientDto == null)
        {
            return BadRequest("Client data is required.");
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Validate mobile number format using libphonenumber-csharp
        var phoneNumberUtil = PhoneNumberUtil.GetInstance();
        try
        {
            var parsedNumber = phoneNumberUtil.Parse(clientDto.MobileNumber, "SA");
            if (!phoneNumberUtil.IsValidNumber(parsedNumber))
            {
                return BadRequest("Invalid mobile number format.");
            }
        }
        catch (NumberParseException)
        {
            return BadRequest("Invalid mobile number format.");
        }

        // Check for existing clients with the same email, mobile number, or personal ID
        var existingClient = await _context.Clients
            .Include(c => c.Accounts)
            .Include(c => c.Address)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (existingClient == null)
        {
            return NotFound($"Client with ID {id} not found.");
        }

        // Check for unique constraints, ensuring other clients do not have the same email, mobile number, or personal ID
        bool emailExists = await _context.Clients.AnyAsync(c => c.Email == clientDto.Email && c.Id != id);
        bool mobileNumberExists = await _context.Clients.AnyAsync(c => c.MobileNumber == clientDto.MobileNumber && c.Id != id);
        bool personalIdExists = await _context.Clients.AnyAsync(c => c.PersonalId == clientDto.PersonalId && c.Id != id);

        if (emailExists)
        {
            return BadRequest($"A client with the email {clientDto.Email} already exists.");
        }

        if (mobileNumberExists)
        {
            return BadRequest($"A client with the mobile number {clientDto.MobileNumber} already exists.");
        }

        if (personalIdExists)
        {
            return BadRequest($"A client with the personal ID {clientDto.PersonalId} already exists.");
        }

        try
        {
            // Start a transaction to ensure the update process is atomic
            using var transaction = await _context.Database.BeginTransactionAsync();

            // Update client's address if changed
            if (existingClient.Address != null)
            {
                existingClient.Address.Country = clientDto.Address.Country;
                existingClient.Address.City = clientDto.Address.City;
                existingClient.Address.Street = clientDto.Address.Street;
                existingClient.Address.ZipCode = clientDto.Address.ZipCode;
            }
            else
            {
                var address = new Address
                {
                    Country = clientDto.Address.Country,
                    City = clientDto.Address.City,
                    Street = clientDto.Address.Street,
                    ZipCode = clientDto.Address.ZipCode
                };
                _context.Addresses.Add(address);
                await _context.SaveChangesAsync();  // Save to get the new AddressId
                existingClient.AddressId = address.Id;
            }

            // Update client fields
            existingClient.Email = clientDto.Email;
            existingClient.FirstName = clientDto.FirstName;
            existingClient.LastName = clientDto.LastName;
            existingClient.MobileNumber = clientDto.MobileNumber;
            existingClient.PersonalId = clientDto.PersonalId;
            existingClient.Sex = (Sex)clientDto.Sex;
            existingClient.ProfilePhoto = clientDto.ProfilePhoto ?? "defaultProfilePhotoUrl"; // Ensure non-null value

            // Update accounts - detailed update
            var existingAccountIds = existingClient.Accounts.Select(a => a.Id).ToList();
            var updatedAccountIds = clientDto.Accounts.Select(a => a.Id).ToList();

            // Accounts to be added
            var accountsToAdd = clientDto.Accounts
                .Where(a => !existingAccountIds.Contains(a.Id))
                .ToList();

            // Accounts to be updated
            var accountsToUpdate = existingClient.Accounts
                .Where(a => updatedAccountIds.Contains(a.Id))
                .ToList();

            // Accounts to be removed
            var accountsToRemove = existingClient.Accounts
                .Where(a => !updatedAccountIds.Contains(a.Id))
                .ToList();

            // Remove accounts that are no longer present
            foreach (var account in accountsToRemove)
            {
                _context.Accounts.Remove(account);
            }

            // Update existing accounts with new values
            foreach (var account in accountsToUpdate)
            {
                var updatedAccount = clientDto.Accounts.First(a => a.Id == account.Id);
                account.AccountNumber = updatedAccount.AccountNumber;
                account.Currency = updatedAccount.Currency;
                
            }

            // Add new accounts that were not present
            foreach (var account in accountsToAdd)
            {
                existingClient.Accounts.Add(new Account
                {
                    AccountNumber = account.AccountNumber,
                    Currency = account.Currency,
                    
                });
            }

            // Save changes to the context
            await _context.SaveChangesAsync();

            // Commit the transaction
            await transaction.CommitAsync();

            return NoContent();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "A concurrency error occurred while updating the client with ID {ClientId}.", id);
            return StatusCode(500, "A concurrency error occurred. Please try again.");
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "An error occurred while updating the client with ID {ClientId}.", id);
            return StatusCode(500, "An error occurred while updating the client. Please try again later.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while updating the client with ID {ClientId}.", id);
            return StatusCode(500, "An unexpected error occurred. Please try again later.");
        }
    }


    // Delete a client
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteClient(int id)
    {
        try
        {
            var client = await _context.Clients.FindAsync(id);
            if (client == null)
            {
                return NotFound($"Client with ID {id} not found.");
            }

            _context.Clients.Remove(client);
            await _context.SaveChangesAsync();
            return NoContent();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "A concurrency error occurred while deleting the client with ID {ClientId}.", id);
            return StatusCode(500, "A concurrency error occurred. Please try again.");
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "An error occurred while deleting the client with ID {ClientId}.", id);
            return StatusCode(500, "An error occurred while deleting the client. Please try again later.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while deleting the client with ID {ClientId}.", id);
            return StatusCode(500, "An unexpected error occurred. Please try again later.");
        }
    }

    
    // Get the last 3 search parameters from cache
    [HttpGet("search-history")]
    public IActionResult GetSearchHistory()
    {
        // Convert the queue of tuples to a list of DTOs
        var historyDto = searchHistoryCache
            .Select(item => new SearchParameterDto
            {
                Search = item.search,  
                Sort = item.sort,      
                Page = item.page,      
                PageSize = item.pageSize
            })
            .ToList();

        return Ok(historyDto);
    }
        
     
    // Get the last 3 search parameters from the database
    [HttpGet("search-history-db")]
    public async Task<IActionResult> GetSearchHistoryFromDb()
    {
        var searchHistory = await _context.SearchParameters
            .OrderByDescending(sp => sp.CreatedAt)
            .Take(3)
            .ToListAsync();

        return Ok(searchHistory);
    }
     

   
}
