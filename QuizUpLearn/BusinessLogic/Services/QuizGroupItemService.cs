using AutoMapper;
using BusinessLogic.DTOs.QuizGroupItemDtos;
using BusinessLogic.Interfaces;
using Repository.Entities;
using Repository.Interfaces;
using BusinessLogic.Extensions;
using BusinessLogic.DTOs;
using BusinessLogic.Helpers;

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

        public async Task<PaginationResponseDto<ResponseQuizGroupItemDto>> GetAllGroupItemAsync(PaginationRequestDto pagination)
        {
            if(pagination == null) pagination = new PaginationRequestDto();
            ValidateHelper.Validate(pagination);
            var quizGroupItems = await _repo.GetAllAsync();
            var dtos = _mapper.Map<IEnumerable<ResponseQuizGroupItemDto>>(quizGroupItems);
            return dtos.ToPagedResponse(pagination);
        }

        public async Task<ResponseQuizGroupItemDto?> GetGroupItemByIdAsync(Guid id)
        {
            if(id == Guid.Empty)
                throw new ArgumentException("Invalid Group Item ID");
            var quizGroupItem = await _repo.GetByIdAsync(id);
            return quizGroupItem != null ? _mapper.Map<ResponseQuizGroupItemDto>(quizGroupItem) : null;
        }

        public async Task<ResponseQuizGroupItemDto?> CreateGroupItemAsync(RequestQuizGroupItemDto requestDto)
        {
            if(requestDto == null)
                throw new ArgumentNullException("Request DTO cannot be null");
            var quizGroupItem = _mapper.Map<QuizGroupItem>(requestDto);
            var item = await _repo.CreateAsync(quizGroupItem);
            return item != null ? _mapper.Map<ResponseQuizGroupItemDto>(item) : null;
        }

        public async Task<ResponseQuizGroupItemDto?> UpdateGroupItemAsync(Guid id, RequestQuizGroupItemDto requestDto)
        {
            if(id == Guid.Empty)
                throw new ArgumentException("Invalid Group Item ID");
            if(requestDto == null)
                throw new ArgumentNullException("Request DTO cannot be null");

            var item = await _repo.UpdateAsync(id, _mapper.Map<QuizGroupItem>(requestDto));
            return item != null ? _mapper.Map<ResponseQuizGroupItemDto>(item) : null;
        }

        public async Task<bool> DeleteGroupItemAsync(Guid id)
        {
            if(id == Guid.Empty)
                throw new ArgumentException("Invalid Group Item ID");
            return await _repo.DeleteAsync(id);
        }
    }
}
