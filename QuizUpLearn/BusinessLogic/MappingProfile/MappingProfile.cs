using AutoMapper;

namespace BusinessLogic.MappingProfile
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            //QuizSet Mappings
            CreateMap<Repository.Entities.QuizSet, DTOs.QuizSetDtos.QuizSetRequestDto>().ReverseMap();
            CreateMap<Repository.Entities.QuizSet, DTOs.QuizSetDtos.QuizSetResponseDto>().ReverseMap();
            //Quiz Mappings
            CreateMap<Repository.Entities.Quiz, DTOs.QuizDtos.QuizRequestDto>().ReverseMap();
            CreateMap<Repository.Entities.Quiz, DTOs.QuizDtos.QuizResponseDto>().ReverseMap();
            //Role Mappings
            CreateMap<Repository.Entities.Role, DTOs.RoleDtos.ResponseRoleDto>().ReverseMap();
            CreateMap<Repository.Entities.Role, DTOs.RoleDtos.RequestRoleDto>().ReverseMap();
            // Account Mappings
            CreateMap<Repository.Entities.Account, DTOs.ResponseAccountDto>().ReverseMap();
            CreateMap<Repository.Entities.Account, DTOs.RequestAccountDto>().ReverseMap();
            
            // User Mappings
            CreateMap<Repository.Entities.User, DTOs.ResponseUserDto>().ReverseMap();
            CreateMap<Repository.Entities.User, DTOs.RequestUserDto>().ReverseMap();
            
            // Quiz Attempt Mappings
            CreateMap<Repository.Entities.QuizAttempt, DTOs.ResponseQuizAttemptDto>().ReverseMap();
            CreateMap<Repository.Entities.QuizAttempt, DTOs.RequestQuizAttemptDto>().ReverseMap();
            
            // Quiz Attempt Detail Mappings
            CreateMap<Repository.Entities.QuizAttemptDetail, DTOs.ResponseQuizAttemptDetailDto>().ReverseMap();
            CreateMap<Repository.Entities.QuizAttemptDetail, DTOs.RequestQuizAttemptDetailDto>().ReverseMap();
            
            // Answer Option Mappings
            CreateMap<Repository.Entities.AnswerOption, DTOs.ResponseAnswerOptionDto>().ReverseMap();
            CreateMap<Repository.Entities.AnswerOption, DTOs.RequestAnswerOptionDto>().ReverseMap();
            CreateMap<Repository.Entities.AnswerOption, DTOs.AnswerOptionStartDto>().ReverseMap();
            
            // Quiz Start Response Mappings
            CreateMap<Repository.Entities.Quiz, DTOs.QuizDtos.QuizStartResponseDto>()
                .ForMember(dest => dest.AnswerOptions, opt => opt.MapFrom(src => src.AnswerOptions));
            
            // Pagination Mappings
            CreateMap<DTOs.PaginationRequestDto, DTOs.PaginationRequestDto>().ReverseMap();
            
            // Dashboard Mappings
            CreateMap<Repository.Entities.QuizAttempt, DTOs.DashboardDtos.QuizHistoryDto>()
                .ForMember(dest => dest.QuizName, opt => opt.MapFrom(src => src.QuizSet != null ? src.QuizSet.Title : "Unknown Quiz"))
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.QuizSet != null ? src.QuizSet.Description : "General"))
                .ForMember(dest => dest.CompletedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.ScorePercentage, opt => opt.MapFrom(src => (double)src.Accuracy))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Accuracy >= 70 ? "Passed" : "Failed"))
                .ForMember(dest => dest.TimeSpent, opt => opt.MapFrom(src => src.TimeSpent ?? 0));
            // UserMistake Mappings
            CreateMap<Repository.Entities.UserMistake, DTOs.UserMistakeDtos.RequestUserMistakeDto>().ReverseMap();
            CreateMap<Repository.Entities.UserMistake, DTOs.UserMistakeDtos.ResponseUserMistakeDto>().ReverseMap();
            // QuizGroupItem Mappings
            CreateMap<Repository.Entities.QuizGroupItem, DTOs.QuizGroupItemDtos.RequestQuizGroupItemDto>().ReverseMap();
            CreateMap<Repository.Entities.QuizGroupItem, DTOs.QuizGroupItemDtos.ResponseQuizGroupItemDto>().ReverseMap();
            //Add other mappings here as needed
        }
    }
}
