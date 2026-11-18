using Gastitis.Application.DTOs.Expenses;
using Gastitis.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

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
        public async Task<IActionResult> CreateExpense(CreateExpenseDTO expenseRequest, CancellationToken cancellationToken)
        {
            var expenseResponseDTO = await _expenseService.CreateAsync(expenseRequest, cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id = expenseResponseDTO.Id },
                expenseResponseDTO);

        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateExpense(int id, [FromBody] UpdateExpenseDTO expenseRequest, CancellationToken cancellationToken)
        {
            var expenseResponseDTO = await _expenseService.UpdateAsync(id, expenseRequest, cancellationToken);
            return Ok(expenseResponseDTO);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteExpense(int id, CancellationToken cancellationToken)
        {
            await _expenseService.DeleteAsync(id, cancellationToken);
            return NoContent();
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ExpenseResponseDTO>> GetById(int id, CancellationToken cancellationToken)
        {
            var expense = await _expenseService.GetByIdAsync(id, cancellationToken);
            return Ok(expense);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] ExpenseFilterDTO filter,
            [FromQuery] ExpenseSortingDTO sorting,
            CancellationToken cancellationToken
            )
        {

            var allExpenses = await _expenseService.GetAllAsync(filter, sorting, cancellationToken);
            return Ok(allExpenses);
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary(
            [FromQuery] ExpenseFilterDTO filter,
			CancellationToken cancellationToken
			)
		{

            var allExpenses = await _expenseService.GetSummaryAsync(filter, cancellationToken);
            return Ok(allExpenses);
        }

        [HttpGet("category-summary")]
        public async Task<IActionResult> GetCategorySummary(
            [FromQuery] ExpenseFilterDTO filter,
			CancellationToken cancellationToken
			)
		{
            var categorySummary = await _expenseService.GetCategorySummaryAsync(filter, cancellationToken);
            return Ok(categorySummary);
        }

    }

}

