using AutoMapper;
using BusinessLogic.DTOs.QuizGroupItemDtos;
using BusinessLogic.Interfaces;
using Repository.Entities;
using Repository.Interfaces;
using BusinessLogic.Extensions;
using BusinessLogic.DTOs;

namespace BusinessLogic.Services
{
    public class QuizGroupItemService : IQuizGroupItemService
    {
        private readonly IQuizGroupItemRepo _repo;
        private readonly IMapper _mapper;

        public QuizGroupItemService(IQuizGroupItemRepo repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<PaginationResponseDto<ResponseQuizGroupItemDto>> GetAllAsync(PaginationRequestDto pagination)
        {
            var quizGroupItems = await _repo.GetAllAsync();
            var dtos = _mapper.Map<IEnumerable<ResponseQuizGroupItemDto>>(quizGroupItems);
            return dtos.ToPagedResponse(pagination);
        }

        public async Task<ResponseQuizGroupItemDto?> GetByIdAsync(Guid id)
        {
            var quizGroupItem = await _repo.GetByIdAsync(id);
            return quizGroupItem != null ? _mapper.Map<ResponseQuizGroupItemDto>(quizGroupItem) : null;
        }

        public async Task<ResponseQuizGroupItemDto?> CreateAsync(RequestQuizGroupItemDto requestDto)
        {
            var quizGroupItem = _mapper.Map<QuizGroupItem>(requestDto);
            var item = await _repo.CreateAsync(quizGroupItem);
            return item != null ? _mapper.Map<ResponseQuizGroupItemDto>(item) : null;
        }

        public async Task<ResponseQuizGroupItemDto?> UpdateAsync(Guid id, RequestQuizGroupItemDto requestDto)
        {
            var item = await _repo.UpdateAsync(id, _mapper.Map<QuizGroupItem>(requestDto));
            return item != null ? _mapper.Map<ResponseQuizGroupItemDto>(item) : null;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            return await _repo.DeleteAsync(id);
        }

        /*public async Task<PaginationResponseDto<ResponseQuizGroupItemDto>> GetAllByQuizSetIdAsync(Guid quizGroupId, PaginationRequestDto pagination)
        {
            //var quizGroupItems = await _repo.GetAllByQuizSetIdAsync(quizGroupId);
            var dtos = _mapper.Map<IEnumerable<ResponseQuizGroupItemDto>>(quizGroupItems);
            return dtos.ToPagedResponse(pagination);
        }*/
    }
}
