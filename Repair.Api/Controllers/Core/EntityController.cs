using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Repair.Abstractions.Persistence;
using Repair.Abstractions.Dto;
using Repair.Abstractions.Api;
using Repair.Api.Exceptions;

namespace Repair.Api.Controllers.Core;

[ApiController]
[Route(Constants.ROUTE_TEMPLATE)]
public abstract class
    EntityController<TEntity, TSearchable, TComplexSearchable, TAddDto, TUpdateDto, TGetDto, TController> :
    ControllerBase,
    IEntityApi<TSearchable, TComplexSearchable, TAddDto, TUpdateDto, TGetDto> where TEntity : class, IEntity
    where TSearchable : class, ISearchable, new()
    where TComplexSearchable : class, IComplexSearchable<TSearchable>
    where TAddDto : class, IAddDto
    where TUpdateDto : class, IUpdateDto
    where TGetDto : class, IGetDto
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

    protected virtual bool HasMatchingVersion(TEntity entity, TUpdateDto dto)
    {
        return entity.Version.SequenceEqual(dto.Version);
    }

    [HttpGet("{id:int:min(1)}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public virtual async Task<TGetDto> GetSingle(int id)
    {
        var entity = await queryService.GetEntity(new TSearchable { Id = id });

        if (entity is null)
        {
            logger.LogWarning("Requested {EntityType} with ID {EntityId}, which does not exist.", typeof(TEntity).Name,
                id);
            throw new EntityNotFoundException(typeof(TEntity), id);
        }

        return CreateGetDto(entity);
    }

    [HttpGet]
    [ProducesResponseType(200)]
    public virtual async Task<IEnumerable<TGetDto>> GetAll()
    {
        var entities = await queryService.GetAllEntities();

        return entities.Select(CreateGetDto).ToList();
    }

    [HttpPost]
    [ProducesResponseType(200)]
    public virtual async Task<IEnumerable<TGetDto>> Search([FromBody] TSearchable searchable)
    {
        var entities = await queryService.GetEntities(searchable);

        return entities.Select(CreateGetDto).ToList();
    }

    [HttpPost]
    [ProducesResponseType(200)]
    public virtual async Task<IEnumerable<TGetDto>> SearchComplex([FromBody] TComplexSearchable searchable)
    {
        var entities = await queryService.GetEntitiesComplex(searchable);

        return entities.Select(CreateGetDto).ToList();
    }

    [HttpPost]
    [ProducesResponseType(201)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    public virtual async Task<TGetDto> AddSingle([FromBody] TAddDto dto)
    {
        var entity = CreateEntity(dto);

        await queryService.AddEntity(entity);

        logger.LogInformation(
            "Created {EntityType} with ID {EntityId}.",
            typeof(TEntity).Name, entity.Id);

        Response.StatusCode = StatusCodes.Status201Created;
        Response.Headers.Location = Url.Action(nameof(GetSingle), new {id = entity.Id,});
        return CreateGetDto(entity);
    }

    [HttpPost]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    public virtual async Task<IEnumerable<TGetDto>> AddMultiple([FromBody] IEnumerable<TAddDto> dtos)
    {
        var entities = dtos.Select(CreateEntity).ToList();

        await queryService.AddEntities(entities);

        logger.LogInformation(
            "Created {Count} entities of type {EntityType}.",
            entities.Count, typeof(TEntity).Name);

        return entities.Select(CreateGetDto).ToList();
    }

    [HttpPut("{id:int:min(1)}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public virtual async Task<TGetDto> UpdateSingle(int id, [FromBody] TUpdateDto dto)
    {
        var entity = await queryService.GetEntity(new TSearchable { Id = id });

        if (entity is null)
        {
            logger.LogWarning("Cannot update {EntityType} with ID {EntityId}, as it does not exist.",
                typeof(TEntity).Name, id);
            throw new EntityNotFoundException(typeof(TEntity), id);
        }

        if (!HasMatchingVersion(entity, dto))
        {
            logger.LogWarning("Cannot update {EntityType} with ID {EntityId} due to a version mismatch.",
                typeof(TEntity).Name, id);
            throw new EntityConcurrencyException("The entity has been modified since it was retrieved.");
        }

        UpdateEntity(entity, dto);

        try
        {
            await queryService.UpdateEntity(entity);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            logger.LogWarning("Cannot update {EntityType} with ID {EntityId} due to a database concurrency conflict.",
                typeof(TEntity).Name, id);
            throw new EntityConcurrencyException("The entity has been modified since it was retrieved.", exception);
        }

        logger.LogInformation(
            "Updated {EntityType} with ID {EntityId}.",
            typeof(TEntity).Name, id);

        return CreateGetDto(entity);
    }

    [HttpDelete("{id:int:min(1)}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public virtual async Task DeleteSingle(int id)
    {
        var entity = await queryService.GetEntity(new TSearchable { Id = id });

        if (entity is null)
        {
            logger.LogWarning("Cannot delete {EntityType} with ID {EntityId}, as it does not exist.",
                typeof(TEntity).Name, id);
            throw new EntityNotFoundException(typeof(TEntity), id);
        }

        try
        {
            await queryService.DeleteEntityById(id);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            logger.LogWarning("Cannot delete {EntityType} with ID {EntityId} due to a database concurrency conflict.",
                typeof(TEntity).Name, id);
            throw new EntityConcurrencyException("The entity was modified or deleted.", exception);
        }

        logger.LogInformation(
            "Deleted {EntityType} with ID {EntityId}.",
            typeof(TEntity).Name, id);

        Response.StatusCode = StatusCodes.Status204NoContent;
    }
}
