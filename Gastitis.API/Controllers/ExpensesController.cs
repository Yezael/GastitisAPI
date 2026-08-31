using Gastitis.Application.DTOs;
using Gastitis.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;

namespace Gastitis.API.Controllers
{
    [ApiController]
    [Route("api/expenses")]
    public class ExpensesController : ControllerBase
    {
        private readonly IExpenseService _expenseService;

        public ExpensesController(IExpenseService expenseService)
        {
            _expenseService = expenseService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateExpense(CreateExpenseDTO expenseRequest)
        {
            var expenseResponseDTO = await _expenseService.CreateAsync(expenseRequest);

            return CreatedAtAction(
                nameof(GetById),
                new { id = expenseResponseDTO.Id },
                expenseResponseDTO);

        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateExpense(int id, [FromBody] UpdateExpenseDTO expenseRequest)
        {
            var expenseResponseDTO = await _expenseService.UpdateAsync(id, expenseRequest);

            if(expenseResponseDTO == null)
            {
                return NotFound();
            }

            return Ok(expenseResponseDTO);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteExpense(int id)
        {
            var deleted = await _expenseService.DeleteAsync(id);

            if (!deleted)
            {
                NotFound();
            }

            return NoContent();

        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ExpenseResponseDTO>> GetById(int id)
        {
            var expense = await _expenseService.GetByIdAsync(id);

            return Ok(expense);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] ExpenseFilterDTO filter,
            [FromQuery] ExpenseSortingDTO sorting
            )
        {

            var allExpenses = await _expenseService.GetAllAsync(filter, sorting);
            return Ok(allExpenses);
        }

        [HttpGet("Summary")]
        public async Task<IActionResult> GetSummary(
            [FromQuery] ExpenseFilterDTO filter
            )
        {

            var allExpenses = await _expenseService.GetSummaryAsync(filter);
            return Ok(allExpenses);
        }

        [HttpGet("CategorySummary")]
        public async Task<IActionResult> GetCategorySummary(
            [FromQuery] ExpenseFilterDTO filter
            )
        {
            var categorySummary = await _expenseService.GetCategorySummaryAsync(filter);
            return Ok(categorySummary);
        }

    }

}

