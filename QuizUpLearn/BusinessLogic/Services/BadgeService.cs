using AutoMapper;
using BusinessLogic.DTOs.BadgeDtos;
using BusinessLogic.Interfaces;
using Microsoft.EntityFrameworkCore;
using Repository.Entities;
using Repository.Enums;
using Repository.Interfaces;

namespace BusinessLogic.Services
{
    public class BadgeService : IBadgeService
    {
        private readonly IBadgeRepo _badgeRepo;
        private readonly IQuizAttemptRepo _quizAttemptRepo;
        private readonly IQuizRepo _quizRepo;
        private readonly IQuizSetRepo _quizSetRepo;
        private readonly IQuizAttemptDetailRepo _quizAttemptDetailRepo;
        private readonly IUserRepo _userRepo;
        private readonly IUserBadgeRepo _userBadgeRepo;
        private readonly IMapper _mapper;

        public BadgeService(
            IBadgeRepo badgeRepo,
            IQuizAttemptRepo quizAttemptRepo,
            IQuizRepo quizRepo,
            IQuizSetRepo quizSetRepo,
            IQuizAttemptDetailRepo quizAttemptDetailRepo,
            IUserRepo userRepo,
            IUserBadgeRepo userBadgeRepo,
            IMapper mapper)
        {
            _badgeRepo = badgeRepo;
            _quizAttemptRepo = quizAttemptRepo;
            _quizRepo = quizRepo;
            _quizSetRepo = quizSetRepo;
            _quizAttemptDetailRepo = quizAttemptDetailRepo;
            _userRepo = userRepo;
            _userBadgeRepo = userBadgeRepo;
            _mapper = mapper;
        }

        /// <summary>
        /// Lấy danh sách badge đã đạt được từ DB (nhanh)
        /// </summary>
        public async Task<List<ResponseBadgeDto>> GetUserBadgesAsync(Guid userId)
        {
            var userBadges = await _userBadgeRepo.GetByUserIdAsync(userId, includeDeleted: false);
            return userBadges
                .Where(ub => ub.Badge != null)
                .Select(ub => _mapper.Map<ResponseBadgeDto>(ub.Badge!))
                .ToList();
        }

        /// <summary>
        /// Check và assign badges cho user (chạy khi có event: hoàn thành quiz, login, etc.)
        /// </summary>
        public async Task CheckAndAssignBadgesAsync(Guid userId)
        {
            var allBadges = await _badgeRepo.GetAllAsync(includeDeleted: false);
            var badgesList = allBadges.ToList();

            // Lấy dữ liệu user
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null) return;

            // Lấy tất cả quiz attempts của user
            var allAttempts = await _quizAttemptRepo.GetByUserIdAsync(userId, includeDeleted: false);
            var completedAttempts = allAttempts.Where(a => a.Status == "completed").ToList();

            // Lấy danh sách badge user đã có
            var existingUserBadges = await _userBadgeRepo.GetByUserIdAsync(userId, includeDeleted: false);
            var existingBadgeIds = existingUserBadges.Select(ub => ub.BadgeId).ToHashSet();

            foreach (var badge in badgesList)
            {
                // Skip nếu đã có badge này
                if (existingBadgeIds.Contains(badge.Id))
                    continue;

                // Check requirement
                if (await CheckBadgeRequirementAsync(badge, userId, user, completedAttempts))
                {
                    // Assign badge
                    var userBadge = new UserBadge
                    {
                        UserId = userId,
                        BadgeId = badge.Id
                    };
                    await _userBadgeRepo.CreateAsync(userBadge);
                }
            }
        }

        private async Task<bool> CheckBadgeRequirementAsync(
            Badge badge,
            Guid userId,
            User user,
            List<QuizAttempt> completedAttempts)
        {
            if (string.IsNullOrEmpty(badge.Code)) return false;

            return badge.Code switch
            {
                "FIRST_STEP" => await CheckFirstStepAsync(completedAttempts),
                "PART_MASTER" => await CheckPartMasterAsync(userId),
                "QUIZ_SET_10" => CheckQuizSetCount(completedAttempts, 10),
                "QUIZ_SET_100" => CheckQuizSetCount(completedAttempts, 100),
                "QUIZ_SET_1000" => CheckQuizSetCount(completedAttempts, 1000),
                "ACCURACY_PRO" => CheckAccuracyPro(completedAttempts),
                "LISTENING_ACE" => await CheckListeningAceAsync(completedAttempts),
                "READING_KING" => await CheckReadingKingAsync(completedAttempts),
                "LIGHTNING_FAST" => await CheckLightningFastAsync(completedAttempts),
                "STREAK_7_DAYS" => CheckStreak(user, 7),
                "STREAK_30_DAYS" => CheckStreak(user, 30),
                "MOCK_TEST_FINISHER" => await CheckMockTestFinisherAsync(userId, completedAttempts),
                "MOCK_TEST_DESTROYER" => await CheckMockTestDestroyerAsync(userId, completedAttempts),
                _ => false
            };
        }

        // FIRST_STEP: finish first quiz
        private async Task<bool> CheckFirstStepAsync(List<QuizAttempt> completedAttempts)
        {
            return completedAttempts.Any();
        }

        // PART_MASTER: complete all quizzes in a TOEIC part (Part 1-7)
        private async Task<bool> CheckPartMasterAsync(Guid userId)
        {
            // Lấy tất cả quiz attempts completed
            var allAttempts = await _quizAttemptRepo.GetByUserIdAsync(userId, includeDeleted: false);
            var completedAttempts = allAttempts.Where(a => a.Status == "completed").ToList();

            if (!completedAttempts.Any()) return false;

            // Lấy tất cả quiz IDs từ completed attempts
            var allQuizIds = new HashSet<Guid>();
            foreach (var attempt in completedAttempts)
            {
                var details = await _quizAttemptDetailRepo.GetByAttemptIdAsync(attempt.Id, includeDeleted: false);
                foreach (var detail in details)
                {
                    allQuizIds.Add(detail.QuestionId);
                }
            }

            // Check từng part (1-7)
            var parts = new[] { "PART1", "PART2", "PART3", "PART4", "PART5", "PART6", "PART7" };
            foreach (var part in parts)
            {
                // Lấy tất cả quiz trong part này từ DB
                var quizzesInPart = await _quizRepo.GetQuizzesByPartAsync(part);
                var quizIdsInPart = quizzesInPart.Select(q => q.Id).ToHashSet();

                // Kiểm tra user đã làm hết tất cả quiz trong part này chưa
                if (quizIdsInPart.Any() && !quizIdsInPart.IsSubsetOf(allQuizIds))
                {
                    return false; // Chưa làm hết quiz trong part này
                }
            }

            return true; // Đã làm hết quiz ở tất cả các part
        }

        // QUIZ_SET_10/100/1000: total quizzes done
        private bool CheckQuizSetCount(List<QuizAttempt> completedAttempts, int requiredCount)
        {
            var uniqueQuizSets = completedAttempts
                .Select(a => a.QuizSetId)
                .Distinct()
                .Count();
            return uniqueQuizSets >= requiredCount;
        }

        // ACCURACY_PRO: 90%+ correct in a quiz (tỷ lệ làm đúng trung bình > 90%)
        private bool CheckAccuracyPro(List<QuizAttempt> completedAttempts)
        {
            if (!completedAttempts.Any()) return false;
            var averageAccuracy = completedAttempts.Average(a => (double)a.Accuracy);
            return averageAccuracy >= 0.90; // 90%
        }

        // LISTENING_ACE: high score in listening parts (>= 400)
        private async Task<bool> CheckListeningAceAsync(List<QuizAttempt> completedAttempts)
        {
            foreach (var attempt in completedAttempts)
            {
                var details = await _quizAttemptDetailRepo.GetByAttemptIdAsync(attempt.Id, includeDeleted: false);
                var detailList = details.ToList();

                int correctLisCount = 0;
                foreach (var detail in detailList)
                {
                    if (detail.IsCorrect == true)
                    {
                        var quiz = await _quizRepo.GetQuizByIdAsync(detail.QuestionId);
                        if (quiz != null)
                        {
                            var isListening = quiz.TOEICPart == "PART1" || quiz.TOEICPart == "PART2" ||
                                            quiz.TOEICPart == "PART3" || quiz.TOEICPart == "PART4";
                            if (isListening)
                                correctLisCount++;
                        }
                    }
                }

                int lisPoint = ConvertToTOEICScore(correctLisCount, isListening: true);
                if (lisPoint >= 400)
                    return true; // Đã đạt >= 400 ở listening
            }
            return false;
        }

        // READING_KING: high score in reading parts (>= 400)
        private async Task<bool> CheckReadingKingAsync(List<QuizAttempt> completedAttempts)
        {
            foreach (var attempt in completedAttempts)
            {
                var details = await _quizAttemptDetailRepo.GetByAttemptIdAsync(attempt.Id, includeDeleted: false);
                var detailList = details.ToList();

                int correctReaCount = 0;
                foreach (var detail in detailList)
                {
                    if (detail.IsCorrect == true)
                    {
                        var quiz = await _quizRepo.GetQuizByIdAsync(detail.QuestionId);
                        if (quiz != null)
                        {
                            var isReading = quiz.TOEICPart == "PART5" || quiz.TOEICPart == "PART6" ||
                                          quiz.TOEICPart == "PART7";
                            if (isReading)
                                correctReaCount++;
                        }
                    }
                }

                int reaPoint = ConvertToTOEICScore(correctReaCount, isListening: false);
                if (reaPoint >= 400)
                    return true; // Đã đạt >= 400 ở reading
            }
            return false;
        }

        // LIGHTNING_FAST: tốc độ làm 1 câu quiz nhanh (giả sử < 10 giây/câu)
        private async Task<bool> CheckLightningFastAsync(List<QuizAttempt> completedAttempts)
        {
            foreach (var attempt in completedAttempts)
            {
                if (!attempt.TimeSpent.HasValue || attempt.TotalQuestions == 0)
                    continue;

                var averageTimePerQuestion = (double)attempt.TimeSpent.Value / attempt.TotalQuestions;
                if (averageTimePerQuestion <= 10) // <= 10 giây/câu
                    return true;
            }
            return false;
        }

        // STREAK_7_DAYS / STREAK_30_DAYS: login liên tục
        private bool CheckStreak(User user, int requiredDays)
        {
            return user.LoginStreak >= requiredDays;
        }

        // MOCK_TEST_FINISHER: finish full TOEIC mock test (không bỏ trống câu nào)
        private async Task<bool> CheckMockTestFinisherAsync(Guid userId, List<QuizAttempt> completedAttempts)
        {
            // Tìm placement test attempts
            var placementAttempts = completedAttempts
                .Where(a => a.AttemptType == "placement")
                .ToList();

            foreach (var attempt in placementAttempts)
            {
                var quizSet = await _quizSetRepo.GetQuizSetByIdAsync(attempt.QuizSetId);
                if (quizSet?.QuizSetType != QuizSetTypeEnum.Placement)
                    continue;

                var details = await _quizAttemptDetailRepo.GetByAttemptIdAsync(attempt.Id, includeDeleted: false);
                var detailList = details.ToList();

                // Kiểm tra tất cả câu đều có answer (không bỏ trống)
                if (detailList.Count == attempt.TotalQuestions &&
                    detailList.All(d => !string.IsNullOrEmpty(d.UserAnswer)))
                {
                    return true;
                }
            }
            return false;
        }

        // MOCK_TEST_DESTROYER: full điểm placement test
        private async Task<bool> CheckMockTestDestroyerAsync(Guid userId, List<QuizAttempt> completedAttempts)
        {
            // Tìm placement test attempts
            var placementAttempts = completedAttempts
                .Where(a => a.AttemptType == "placement")
                .ToList();

            foreach (var attempt in placementAttempts)
            {
                var quizSet = await _quizSetRepo.GetQuizSetByIdAsync(attempt.QuizSetId);
                if (quizSet?.QuizSetType != QuizSetTypeEnum.Placement)
                    continue;

                // Kiểm tra full điểm (tất cả câu đều đúng)
                if (attempt.CorrectAnswers == attempt.TotalQuestions && attempt.TotalQuestions > 0)
                {
                    return true;
                }
            }
            return false;
        }

        private int ConvertToTOEICScore(int correctAnswers, bool isListening)
        {
            if (correctAnswers <= 0) return 5;
            if (correctAnswers > 100) return 495;

            var scoreMap = new Dictionary<int, (int Listening, int Reading)>
            {
                { 1, (5, 5) }, { 2, (5, 5) }, { 3, (5, 5) }, { 4, (5, 5) }, { 5, (5, 5) },
                { 6, (5, 5) }, { 7, (5, 5) }, { 8, (5, 5) }, { 9, (5, 5) }, { 10, (5, 5) },
                { 11, (5, 5) }, { 12, (5, 5) }, { 13, (5, 5) }, { 14, (5, 5) }, { 15, (5, 5) },
                { 16, (5, 5) }, { 17, (5, 5) }, { 18, (10, 5) }, { 19, (15, 5) }, { 20, (20, 5) },
                { 21, (25, 5) }, { 22, (30, 10) }, { 23, (35, 15) }, { 24, (40, 20) }, { 25, (45, 25) },
                { 26, (50, 30) }, { 27, (55, 35) }, { 28, (60, 40) }, { 29, (70, 45) }, { 30, (80, 55) },
                { 31, (85, 60) }, { 32, (90, 65) }, { 33, (95, 70) }, { 34, (100, 75) }, { 35, (105, 80) },
                { 36, (115, 85) }, { 37, (125, 90) }, { 38, (135, 95) }, { 39, (140, 105) }, { 40, (150, 115) },
                { 41, (160, 120) }, { 42, (170, 125) }, { 43, (175, 130) }, { 44, (180, 135) }, { 45, (190, 140) },
                { 46, (200, 145) }, { 47, (205, 155) }, { 48, (215, 160) }, { 49, (220, 170) }, { 50, (225, 175) },
                { 51, (230, 185) }, { 52, (235, 195) }, { 53, (245, 205) }, { 54, (255, 210) }, { 55, (260, 215) },
                { 56, (265, 220) }, { 57, (275, 230) }, { 58, (285, 240) }, { 59, (290, 245) }, { 60, (295, 250) },
                { 61, (300, 255) }, { 62, (310, 260) }, { 63, (320, 270) }, { 64, (325, 275) }, { 65, (330, 280) },
                { 66, (335, 285) }, { 67, (340, 290) }, { 68, (345, 295) }, { 69, (350, 295) }, { 70, (355, 300) },
                { 71, (360, 310) }, { 72, (365, 315) }, { 73, (370, 320) }, { 74, (375, 325) }, { 75, (385, 330) },
                { 76, (395, 335) }, { 77, (400, 340) }, { 78, (405, 345) }, { 79, (415, 355) }, { 80, (420, 360) },
                { 81, (425, 370) }, { 82, (430, 375) }, { 83, (435, 385) }, { 84, (440, 390) }, { 85, (445, 395) },
                { 86, (455, 405) }, { 87, (460, 415) }, { 88, (465, 420) }, { 89, (475, 425) }, { 90, (480, 435) },
                { 91, (485, 440) }, { 92, (490, 450) }, { 93, (495, 455) }, { 94, (495, 460) }, { 95, (495, 470) },
                { 96, (495, 475) }, { 97, (495, 485) }, { 98, (495, 485) }, { 99, (495, 490) }, { 100, (495, 495) }
            };

            if (scoreMap.TryGetValue(correctAnswers, out var scores))
            {
                return isListening ? scores.Listening : scores.Reading;
            }

            return isListening ? 5 : 5;
        }
    }
}

