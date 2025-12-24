# 📋 THÔNG TIN CẬP NHẬT CHO FRONTEND - EVENT & BOSS FIGHT

## 🚨 THAY ĐỔI QUAN TRỌNG

### 1. **JoinGame - BẮT BUỘC PHẢI ĐĂNG NHẬP**

**Trước đây:**
- Cho phép join game mà không cần đăng nhập (guest mode)
- Nếu không có token, vẫn có thể chơi nhưng không lưu lịch sử

**Bây giờ:**
- **BẮT BUỘC phải có JWT token hợp lệ** để join game
- Nếu không có token hoặc token không hợp lệ → **TỪ CHỐI** và gửi error message

**SignalR Connection:**
```javascript
// PHẢI gửi JWT token khi kết nối SignalR
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/gamehub", {
        accessTokenFactory: () => {
            return localStorage.getItem("token"); // JWT token
        }
    })
    .build();
```

**Error Messages:**
- `"Bạn phải đăng nhập để tham gia game. Vui lòng đăng nhập và thử lại."` - Khi không có token
- `"Token không hợp lệ hoặc đã hết hạn. Vui lòng đăng nhập lại."` - Khi token invalid/expired

---

### 2. **BOSS FIGHT - KHÔNG LẶP LẠI CÂU HỎI**

**Thay đổi:**
- Boss Fight là mini game của Event → **KHÔNG lặp lại câu hỏi**
- Nếu bộ đề có 4 câu hỏi → chỉ được trả lời đúng 4 lần
- Không còn infinite loop như trước

**QuestionDto:**
```typescript
{
    QuestionId: Guid,
    QuestionText: string,
    ImageUrl?: string,
    AudioUrl?: string,
    AnswerOptions: AnswerOptionDto[],
    QuestionNumber: number,        // 1, 2, 3, 4 (không lặp lại)
    TotalQuestions: number,         // 4 (tổng số câu, không phải -1)
    TimeLimit?: number,
    QuizGroupItemId?: Guid,
    ToeicPart?: string
}
```

**Lưu ý:**
- `TotalQuestions` bây giờ là số thực (ví dụ: 4), không còn -1 (infinite)
- Khi player trả lời hết câu hỏi, `GetPlayerNextQuestion` sẽ trả về `null`

---

### 3. **EVENT MỚI: BossFightQuestionsExhausted**

**Khi nào gửi:**
- Khi **TẤT CẢ players** đã trả lời hết tất cả câu hỏi
- Nhưng **Boss chưa bị defeated** (BossCurrentHP > 0)

**Event Name:**
```
BossFightQuestionsExhausted
```

**Payload Structure:**

**Option 1: Có kết quả từ service**
```typescript
{
    GamePin: string,
    TotalDamageDealt: number,
    DamageRankings: PlayerDamageRanking[],  // Xếp hạng theo damage
    MvpPlayer?: PlayerDamageRanking,        // Player gây nhiều damage nhất
    TimeToDefeat: number,                    // Thời gian chơi (seconds)
    BossWins: true                           // Boss thắng
}
```

**Option 2: Fallback (nếu service trả về null)**
```typescript
{
    GamePin: string,
    Message: "Đã trả lời hết tất cả câu hỏi nhưng Boss vẫn còn sống! Boss thắng!",
    BossCurrentHP: number,
    BossMaxHP: number,
    TotalDamageDealt: number,
    BossWins: true
}
```

**PlayerDamageRanking:**
```typescript
{
    PlayerName: string,
    TotalDamage: number,        // Tổng damage gây ra
    CorrectAnswers: number,     // Số câu trả lời đúng
    TotalAnswered: number,       // Tổng số câu đã trả lời
    Rank: number,               // Xếp hạng (1, 2, 3...)
    DamagePercent: number       // % damage so với tổng damage
}
```

**Xử lý ở Frontend:**
```javascript
connection.on("BossFightQuestionsExhausted", (result) => {
    // Hiển thị màn hình "Boss Thắng"
    // Hiển thị:
    // - Thông báo: "Đã trả lời hết tất cả câu hỏi nhưng Boss vẫn còn sống! Boss thắng!"
    // - Boss HP còn lại: result.BossCurrentHP / result.BossMaxHP
    // - Tổng damage đã gây: result.TotalDamageDealt
    // - Bảng xếp hạng players theo damage: result.DamageRankings
    // - MVP Player: result.MvpPlayer
    
    // Điểm đã được tự động sync vào database
    // Có thể gọi API để lấy leaderboard event để hiển thị điểm
});
```

---

### 4. **TỰ ĐỘNG SYNC ĐIỂM KHI QUESTIONS EXHAUSTED**

**Thay đổi:**
- Khi hết câu hỏi nhưng boss chưa defeated, **hệ thống tự động sync điểm** cho tất cả players
- Không cần đợi mod gọi EndEvent API
- Đảm bảo tất cả players đều có lịch sử và điểm trên leaderboard

**Lưu ý:**
- Điểm được sync tự động → Frontend có thể gọi API lấy leaderboard ngay
- Tất cả players đều được sync, kể cả player bị disconnect/văng

---

## 📡 SIGNALR EVENTS CẦN XỬ LÝ

### Events liên quan đến Boss Fight:

1. **BossFightQuestionsExhausted** (MỚI)
   - Khi hết câu hỏi nhưng boss chưa defeated
   - Payload: `BossDefeatedDto` hoặc fallback object

2. **BossDefeated**
   - Khi boss bị defeated (HP <= 0)
   - Payload: `BossDefeatedDto` với `BossWins: false`

3. **BossFightTimeUp**
   - Khi hết thời gian (nếu có time limit)
   - Payload: `BossDefeatedDto` với `BossWins: true`

4. **BossDamaged**
   - Khi boss nhận damage từ player
   - Payload: `BossDamagedDto`

---

## 🔄 FLOW KHI QUESTIONS EXHAUSTED

```
1. Player trả lời câu hỏi cuối cùng
   ↓
2. MovePlayerToNextQuestionAsync() → CurrentQuestionIndex >= TotalQuestions
   ↓
3. CheckAndHandleQuestionsExhaustedAsync() → Tất cả players đã hết câu hỏi?
   ↓
4. Nếu Boss chưa defeated:
   - Tự động sync điểm cho tất cả players (nếu là Event)
   - Gửi event "BossFightQuestionsExhausted" cho tất cả players
   - Game status = Completed
   ↓
5. Frontend nhận event → Hiển thị màn hình "Boss Thắng"
```

---

## ✅ CHECKLIST CHO FRONTEND

- [ ] **JoinGame**: Đảm bảo gửi JWT token khi kết nối SignalR
- [ ] **JoinGame**: Xử lý error message khi không có token hoặc token invalid
- [ ] **Boss Fight**: Hiển thị `TotalQuestions` đúng (không còn -1)
- [ ] **Boss Fight**: Xử lý khi `GetPlayerNextQuestion` trả về `null` (hết câu hỏi)
- [ ] **BossFightQuestionsExhausted**: Thêm handler cho event mới này
- [ ] **BossFightQuestionsExhausted**: Hiển thị UI "Boss Thắng" với đầy đủ thông tin
- [ ] **Leaderboard**: Gọi API lấy leaderboard event sau khi nhận `BossFightQuestionsExhausted`
- [ ] **Error Handling**: Xử lý trường hợp player bị văng/disconnect

---

## 📝 API ENDPOINTS LIÊN QUAN

Sau khi nhận `BossFightQuestionsExhausted`, có thể gọi:

```
GET /api/event/{eventId}/leaderboard
```
Để lấy leaderboard event với điểm đã được sync tự động.

---

## 🐛 DEBUGGING

Nếu player bị văng hoặc không nhận được thông báo:

1. **Kiểm tra SignalR connection:**
   - Player có còn kết nối không?
   - Player có trong group `Game_{gamePin}` không?

2. **Kiểm tra logs:**
   - Backend log: `📢 Sent BossFightQuestionsExhausted to all players in group Game_{gamePin}`
   - Backend log: `✅ Auto-sync completed for Event {eventId}`

3. **Kiểm tra điểm đã được sync:**
   - Gọi API: `GET /api/event/{eventId}/leaderboard`
   - Kiểm tra player có trong leaderboard không

---

## 📞 LIÊN HỆ

Nếu có vấn đề, vui lòng kiểm tra:
- Logs backend để xem có sync điểm không
- SignalR connection status
- Event payload có đúng format không

