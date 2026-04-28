using AccountingHelper.Contexts;
using AccountingHelper.Models;
using Microsoft.AspNetCore.Mvc;

namespace AccountingHelper.Controllers;

[ApiController]
[Route("employees")]
public class EmployeesController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet("get-all")]
    public IActionResult GetEmployees()
    {
        var employees = context.Employees.ToList();

        return Ok(employees);
    }


    [HttpGet("get/{id}")]
    public IActionResult GetEmployee(string id)
    {
        var employee = context.Employees.FirstOrDefault(e => e.Id == id);

        if (employee == null) return NotFound();

        return Ok(employee);
    }

    [HttpPost("create")]
    public IActionResult CreateEmployee(Employee employee)
    {
        try
        {
            context.Employees.Add(employee);
            return Ok();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return BadRequest();
        }
    }

    [HttpPut("fire/{id}")]
    public IActionResult FireEmployee(string id)
    {
        try
        {
            var employee = context.Employees.FirstOrDefault(e => e.Id == id);
            employee.Status = "Fired";

            return Ok();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return BadRequest();
        }
    }
}