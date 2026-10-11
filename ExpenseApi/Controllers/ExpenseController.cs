using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ExpenseApi.Models;
using Microsoft.AspNetCore.Authorization;
namespace ExpensesApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]

public class ExpensesController: ControllerBase
{
    private readonly ExpenseContext _context;
    public ExpensesController(ExpenseContext context)
    {
        _context = context;
    }

    //GET: api/Expenses
    [HttpGet]

    public async Task<ActionResult<IEnumerable<ExpenseItemDTO>>> GetExpenseItems()
    {
        return await _context.ExpenseItems
            .Select(x => ItemToDTO(x))
            .ToListAsync();
    }

    //GET: api/Expenses/5
    [HttpGet("{id}")]
    public async Task<ActionResult<ExpenseItemDTO>> GetExpenseItem(long id)
    {
        var expenseItem = await _context.ExpenseItems.FindAsync(id);

        if (expenseItem == null)
        {
            return NotFound();
        }
        return ItemToDTO(expenseItem);
    }
    // PUT api/Expenses/5
    // how do we protect from overposting attacks? 
    [HttpPut("{id}")]
    public async Task<IActionResult> PutExpenseItem(long id, ExpenseItemDTO expenseDTO)
    {
        if (id != expenseDTO.Id)
        {
            return BadRequest();
        }
        var expenseItem = await _context.ExpenseItems.FindAsync(id);
        if (expenseItem == null)
        {
            return NotFound();
        }
        expenseItem.PurchaseName = expenseDTO.PurchaseName;
        expenseItem.Category = expenseDTO.Category;
        expenseItem.value = expenseDTO.value;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException) when (!ExpenseItemExists(id))
        {
            return NotFound();
        }
        return NoContent();
    }


    [HttpPost]
    public async Task<ActionResult<ExpenseItemDTO>> PostExpense(ExpenseItemDTO expenseDTO)
    {
        var expenseItem = new ExpenseItem
        {
            PurchaseName = expenseDTO.PurchaseName,
            PurchaseDate = expenseDTO.PurchaseDate,
            Category = expenseDTO.Category,
            value = expenseDTO.value
        };

        _context.ExpenseItems.Add(expenseItem);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetExpenseItem),
            new { id = expenseItem.Id},
            ItemToDTO(expenseItem));
        
    }

    //DELETE: api/Expenses/5
    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTodoItem(long id)
    {
        var expenseItem = await _context.ExpenseItems.FindAsync(id);
        if (expenseItem == null)
        {
            return NotFound();
        }
        _context.ExpenseItems.Remove(expenseItem);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private bool ExpenseItemExists(long id)
    {
        return _context.ExpenseItems.Any(e => e.Id == id); //What the LINQ?
    }

    private static ExpenseItemDTO ItemToDTO(ExpenseItem expenseItem) =>
        new ExpenseItemDTO
        {
            Id = expenseItem.Id,
            PurchaseName = expenseItem.PurchaseName,
            PurchaseDate = expenseItem.PurchaseDate,
            Category = expenseItem.Category,
            value = expenseItem.value

        };

}