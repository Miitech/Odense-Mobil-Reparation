using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Repair.Abstractions.Persistence;

namespace Repair.Api.Controllers.Core;

[ApiController]
[Route(Constants.ROUTE_TEMPLATE)]
public abstract class EntityController<
    TEntity,
    TSearchable,
    TComplexSearchable,
    TAddDto,
    TUpdateDto,
    TGetDto,
    TController> : ControllerBase
    where TEntity : class, IEntity
    where TSearchable : class, ISearchable, new()
    where TComplexSearchable : class, IComplexSearchable<TSearchable>
    where TAddDto : class
    where TUpdateDto : class
    where TGetDto : class
    where TController : ControllerBase
{
    protected readonly IEntityQueryService<TEntity, TSearchable> queryService;
    protected readonly ILogger<TController> logger;

    protected EntityController(
        IEntityQueryService<TEntity, TSearchable> queryService,
        ILogger<TController> logger)
    {
        this.queryService = queryService;
        this.logger = logger;
    }

    protected abstract TEntity CreateEntity(TAddDto dto);

    protected abstract void UpdateEntity(TEntity entity, TUpdateDto dto);

    protected abstract TGetDto CreateGetDto(TEntity entity);

    protected abstract bool HasMatchingVersion(TEntity entity, TUpdateDto dto);

    [HttpGet("{id:int:min(1)}")]
    public virtual async Task<ActionResult<TGetDto>> GetSingle(int id)
    {
        var entity = await queryService.GetEntity(new TSearchable { Id = id });

        if (entity is null)
            return NotFound();

        return Ok(CreateGetDto(entity));
    }

    [HttpGet]
    public virtual async Task<ActionResult<IEnumerable<TGetDto>>> GetAll()
    {
        var entities = await queryService.GetAllEntities();

        return Ok(entities.Select(CreateGetDto).ToList());
    }

    [HttpPost]
    public virtual async Task<ActionResult<IEnumerable<TGetDto>>> Search(
        [FromBody] TSearchable searchable)
    {
        var entities = await queryService.GetEntities(searchable);

        return Ok(entities.Select(CreateGetDto).ToList());
    }

    [HttpPost]
    public virtual async Task<ActionResult<IEnumerable<TGetDto>>> SearchComplex(
        [FromBody] TComplexSearchable searchable)
    {
        var entities = await queryService.GetEntitiesComplex(searchable);

        return Ok(entities.Select(CreateGetDto).ToList());
    }

    [HttpPost]
    public virtual async Task<ActionResult<TGetDto>> AddSingle(
        [FromBody] TAddDto dto)
    {
        var entity = CreateEntity(dto);

        await queryService.AddEntity(entity);

        logger.LogInformation(
            "Created {EntityType} with ID {EntityId}.",
            typeof(TEntity).Name, entity.Id);

        return CreatedAtAction(
            nameof(GetSingle),
            new { id = entity.Id },
            CreateGetDto(entity));
    }

    [HttpPost]
    public virtual async Task<ActionResult<IEnumerable<TGetDto>>> AddMultiple(
        [FromBody] IEnumerable<TAddDto> dtos)
    {
        var entities = dtos.Select(CreateEntity).ToList();

        await queryService.AddEntities(entities);

        logger.LogInformation(
            "Created {Count} entities of type {EntityType}.",
            entities.Count, typeof(TEntity).Name);

        return Ok(entities.Select(CreateGetDto).ToList());
    }

    [HttpPut("{id:int:min(1)}")]
    public virtual async Task<ActionResult<TGetDto>> UpdateSingle(
        int id, [FromBody] TUpdateDto dto)
    {
        var entity = await queryService.GetEntity(new TSearchable { Id = id });

        if (entity is null)
            return NotFound();

        if (!HasMatchingVersion(entity, dto))
            return Conflict("The entity has been modified since it was retrieved.");

        UpdateEntity(entity, dto);

        try
        {
            await queryService.UpdateEntity(entity);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict("The entity has been modified since it was retrieved.");
        }

        logger.LogInformation(
            "Updated {EntityType} with ID {EntityId}.",
            typeof(TEntity).Name, id);

        return Ok(CreateGetDto(entity));
    }

    [HttpDelete("{id:int:min(1)}")]
    public virtual async Task<IActionResult> DeleteSingle(int id)
    {
        var entity = await queryService.GetEntity(new TSearchable { Id = id });

        if (entity is null)
            return NotFound();

        try
        {
            await queryService.DeleteEntityById(id);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict("The entity was modified or deleted.");
        }

        logger.LogInformation(
            "Deleted {EntityType} with ID {EntityId}.",
            typeof(TEntity).Name, id);

        return NoContent();
    }
}
