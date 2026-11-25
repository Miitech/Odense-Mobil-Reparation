using Refit;
using Repair.Abstractions.Dto;
using Repair.Abstractions.Persistence;

namespace Repair.Abstractions.Api;

public interface IEntityApi<TSearchable, TComplexSearchable, TAddDto, TUpdateDto, TGetDto>
    where TSearchable : class, ISearchable, new()
    where TComplexSearchable : class, IComplexSearchable<TSearchable>
    where TAddDto : class, IAddDto
    where TUpdateDto : class, IUpdateDto
    where TGetDto : class, IGetDto
{
    [Get("/" + nameof(GetSingle) + ApiRoutes.ID)]
    Task<TGetDto> GetSingle(int id);
    [Get("/" + nameof(GetAll))]
    Task<IEnumerable<TGetDto>> GetAll();
    [Post("/" + nameof(Search))]
    Task<IEnumerable<TGetDto>> Search([Body] TSearchable searchable);
    [Post("/" + nameof(SearchComplex))]
    Task<IEnumerable<TGetDto>> SearchComplex([Body] TComplexSearchable searchable);
    [Post("/" + nameof(AddSingle))]
    Task<TGetDto> AddSingle([Body] TAddDto dto);
    [Post("/" + nameof(AddMultiple))]
    Task<IEnumerable<TGetDto>> AddMultiple([Body] IEnumerable<TAddDto> dtos);
    [Put("/" + nameof(UpdateSingle) + ApiRoutes.ID)]
    Task<TGetDto> UpdateSingle(int id, [Body] TUpdateDto dto);
    [Delete("/" + nameof(DeleteSingle) + ApiRoutes.ID)]
    Task DeleteSingle(int id);
}
