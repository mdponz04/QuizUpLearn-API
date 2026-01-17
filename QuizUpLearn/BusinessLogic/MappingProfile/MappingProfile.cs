using AutoMapper;

namespace BusinessLogic.MappingProfile
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            //QuizSet Mappings
            CreateMap<Repository.Entities.QuizSet, DTOs.QuizSetDtos.QuizSetRequestDto>().ReverseMap();
            CreateMap<Repository.Entities.QuizSet, DTOs.QuizSetDtos.QuizSetResponseDto>();
            //Quiz Mappings
            CreateMap<Repository.Entities.Quiz, DTOs.QuizDtos.QuizRequestDto>().ReverseMap();
            CreateMap<Repository.Entities.Quiz, DTOs.QuizDtos.QuizResponseDto>();
            //Role Mappings
            CreateMap<Repository.Entities.Role, DTOs.RoleDtos.ResponseRoleDto>();
            CreateMap<Repository.Entities.Role, DTOs.RoleDtos.RequestRoleDto>().ReverseMap();
            // Account Mappings
            CreateMap<Repository.Entities.Account, DTOs.ResponseAccountDto>()
                .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.User != null ? src.User.Username : string.Empty))
                .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom(src => src.User != null ? src.User.AvatarUrl : string.Empty))
                .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role != null ? src.Role.RoleName : string.Empty))
                .ReverseMap();
            CreateMap<Repository.Entities.Account, DTOs.RequestAccountDto>().ReverseMap();
            
            // User Mappings
            CreateMap<Repository.Entities.User, DTOs.ResponseUserDto>();
            CreateMap<Repository.Entities.User, DTOs.RequestUserDto>().ReverseMap();
            
            // Quiz Attempt Mappings
            CreateMap<Repository.Entities.QuizAttempt, DTOs.ResponseQuizAttemptDto>();
            CreateMap<Repository.Entities.QuizAttempt, DTOs.RequestQuizAttemptDto>().ReverseMap();
            
            // Quiz Attempt Detail Mappings
            CreateMap<Repository.Entities.QuizAttemptDetail, DTOs.ResponseQuizAttemptDetailDto>();
            CreateMap<Repository.Entities.QuizAttemptDetail, DTOs.RequestQuizAttemptDetailDto>().ReverseMap();
            
            // Answer Option Mappings
            CreateMap<Repository.Entities.AnswerOption, DTOs.ResponseAnswerOptionDto>();
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
            CreateMap<Repository.Entities.QuizGroupItem, DTOs.QuizGroupItemDtos.ResponseQuizGroupItemDto>().ForMember(d => d.Quizzes, o => o.Ignore());
            // Grammar Mappings
            CreateMap<Repository.Entities.Grammar, DTOs.GrammarDtos.RequestGrammarDto>().ReverseMap();
            CreateMap<Repository.Entities.Grammar, DTOs.GrammarDtos.ResponseGrammarDto>();
            // Vocabulary Mappings
            CreateMap<Repository.Entities.Vocabulary, DTOs.VocabularyDtos.RequestVocabularyDto>().ReverseMap();
            CreateMap<Repository.Entities.Vocabulary, DTOs.VocabularyDtos.ResponseVocabularyDto>();

            // UserWeakPoint Mappings
            CreateMap<Repository.Entities.UserWeakPoint, DTOs.UserWeakPointDtos.ResponseUserWeakPointDto>();
            CreateMap<Repository.Entities.UserWeakPoint, DTOs.UserWeakPointDtos.RequestUserWeakPointDto>().ReverseMap();

            //subscription mappings
            CreateMap<Repository.Entities.Subscription, DTOs.SubscriptionDtos.RequestSubscriptionDto>().ReverseMap();
            CreateMap<Repository.Entities.Subscription, DTOs.SubscriptionDtos.ResponseSubscriptionDto>();
            //Subscription plan mappings
            CreateMap<Repository.Entities.SubscriptionPlan, DTOs.SubscriptionPlanDtos.RequestSubscriptionPlanDto>().ReverseMap();
            CreateMap<Repository.Entities.SubscriptionPlan, DTOs.SubscriptionPlanDtos.ResponseSubscriptionPlanDto>();
            //Payment transaction mappings
            CreateMap<Repository.Entities.PaymentTransaction, DTOs.PaymentTransactionDtos.RequestPaymentTransactionDto>().ReverseMap();
            CreateMap<Repository.Entities.PaymentTransaction, DTOs.PaymentTransactionDtos.ResponsePaymentTransactionDto>();
            //App setting mappings
            CreateMap<Repository.Entities.AppSetting, DTOs.AppSettingDto>().ReverseMap();
            // QuizQuizSet mappings
            CreateMap<Repository.Entities.QuizQuizSet, DTOs.QuizQuizSetDtos.ResponseQuizQuizSetDto>();
            CreateMap<Repository.Entities.QuizQuizSet, DTOs.QuizQuizSetDtos.RequestQuizQuizSetDto>().ReverseMap();
            //QuizReport mappings
            CreateMap<Repository.Entities.QuizReport, DTOs.QuizReportDtos.ResponseQuizReportDto>();
            CreateMap<Repository.Entities.QuizReport, DTOs.QuizReportDtos.RequestQuizReportDto>().ReverseMap();
            //UserReport mappings
            CreateMap<Repository.Entities.UserReport, DTOs.UserReportDtos.ResponseUserReportDto>();
            CreateMap<Repository.Entities.UserReport, DTOs.UserReportDtos.RequestUserReportDto>().ReverseMap();
            //QuizSetComment mappings
            CreateMap<Repository.Entities.QuizSetComment, DTOs.QuizSetCommentDtos.ResponseQuizSetCommentDto>();
            CreateMap<Repository.Entities.QuizSetComment, DTOs.QuizSetCommentDtos.RequestQuizSetCommentDto>().ReverseMap();
            //UserQuizSetFavorite mappings
            CreateMap<Repository.Entities.UserQuizSetFavorite, DTOs.UserQuizSetFavoriteDtos.ResponseUserQuizSetFavoriteDto>();
            CreateMap<Repository.Entities.UserQuizSetFavorite, DTOs.UserQuizSetFavoriteDtos.RequestUserQuizSetFavoriteDto>().ReverseMap();
            //UserQuizSetLike mappings
            CreateMap<Repository.Entities.UserQuizSetLike, DTOs.UserQuizSetLikeDtos.ResponseUserQuizSetLikeDto>();
            CreateMap<Repository.Entities.UserQuizSetLike, DTOs.UserQuizSetLikeDtos.RequestUserQuizSetLikeDto>().ReverseMap();
            //Notification mappings
            CreateMap<Repository.Entities.Notification, DTOs.NotificationDtos.NotificationRequestDto>().ReverseMap();
            CreateMap<Repository.Entities.Notification, DTOs.NotificationDtos.NotificationResponseDto>();
            //UserNotification mappings
            CreateMap<Repository.Entities.UserNotification, DTOs.UserNotificationDtos.UserNotificationRequestDto>().ReverseMap();
            CreateMap<Repository.Entities.UserNotification, DTOs.UserNotificationDtos.UserNotificationResponseDto>();
            //Badge mappings
            CreateMap<Repository.Entities.Badge, DTOs.BadgeDtos.RequestBadgeDto>().ReverseMap();
            CreateMap<Repository.Entities.Badge, DTOs.BadgeDtos.ResponseBadgeDto>();
            //UserBadge mappings
            CreateMap<DTOs.BadgeDtos.RequestUserBadgeDto, Repository.Entities.UserBadge>()
                .ForMember(dest => dest.BadgeId, opt => opt.Ignore()); // BadgeId sẽ được set trong controller
            CreateMap<Repository.Entities.UserBadge, DTOs.BadgeDtos.RequestUserBadgeDto>();
            CreateMap<Repository.Entities.UserBadge, DTOs.BadgeDtos.ResponseUserBadgeDto>();
        }
    }
}
