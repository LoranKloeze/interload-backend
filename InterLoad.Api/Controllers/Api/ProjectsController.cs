using InterLoad.Data;
using InterLoad.Dtos;
using InterLoad.Models.Ef;
using InterLoad.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InterLoad.Controllers.Api;

[Route("/api/[controller]")]
public class ProjectsController(ApplicationDbContext context) : ApiControllerBase
{
    
    [HttpGet("{id:int}")]
    public async Task<Ok<ProjectDtoOut>> GetOne(int id)
    {
        var project = await context.Projects
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new ProjectDtoOut
            {
                Id = p.Id,
                Name = p.Name,
            })
            .SingleOrDefaultAsync();

        return project == null ? 
            throw new InvalidOperationException($"Project with ID {id} not found.") : TypedResults.Ok(project);
    }
    
}