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
    Task<TGetDto> GetSingle(int id);
    Task<IEnumerable<TGetDto>> GetAll();
    Task<IEnumerable<TGetDto>> Search(TSearchable searchable);
    Task<IEnumerable<TGetDto>> SearchComplex(TComplexSearchable searchable);
    Task<TGetDto> AddSingle(TAddDto dto);
    Task<IEnumerable<TGetDto>> AddMultiple(IEnumerable<TAddDto> dtos);
    Task<TGetDto> UpdateSingle(int id, TUpdateDto dto);
    Task DeleteSingle(int id);
}
