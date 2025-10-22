# 🎮 1vs1 Realtime Game Flow - Complete Documentation

## 📋 **Tổng quan**
Hệ thống realtime game 1vs1 được implement hoàn chỉnh với SignalR, bao gồm full flow từ tạo phòng đến kết thúc game với documentation chi tiết.

## 🏗️ **Architecture Overview**

### **1. Core Components**
```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Frontend      │    │   SignalR Hub   │    │  Game Service   │
│   (JavaScript)  │◄──►│   (GameHub)     │◄──►│ (RealtimeGame)  │
└─────────────────┘    └─────────────────┘    └─────────────────┘
         │                        │                        │
         ▼                        ▼                        ▼
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   REST API      │    │   Database      │    │   In-Memory     │
│ (GameController)│    │  (PostgreSQL)   │    │   Storage       │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

### **2. Data Flow**
```
1. Host creates room → API → Game Service → Database
2. Guest joins room → SignalR → Game Service → Memory
3. Game starts → SignalR → Both players → Questions
4. Players answer → SignalR → Game Service → Scoring
5. Game ends → SignalR → Both players → Results
```

## 🎯 **Complete Game Flow**

### **PHASE 1: Room Creation & Setup**

#### **1.1 Host Creates Room**
```javascript
// Frontend - Host creates room
const createRoomRequest = {
    hostUserId: 1,
    hostUserName: "Player1",
    quizSetId: 5,
    timeLimit: 30
};

const response = await fetch('/api/game/create-room', {
    method: 'POST',
    headers: {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer ' + jwtToken
    },
    body: JSON.stringify(createRoomRequest)
});

const result = await response.json();
// Returns: { "success": true, "data": "ABC12345", "message": "Game room created successfully" }
```

**Backend Flow:**
1. `GameController.CreateGameRoom()` receives request
2. `RealtimeGameService.CreateGameRoomAsync()` validates quiz set
3. Generates unique room ID (8 characters)
4. Loads questions from database
5. Stores room info in memory
6. Returns room ID to client

#### **1.2 Host Connects to SignalR**
```javascript
// Frontend - Host connects to SignalR
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/game-hub", {
        accessTokenFactory: () => localStorage.getItem('jwt_token')
    })
    .build();

await connection.start();
console.log('Host connected to SignalR');

// Join the room
await connection.invoke("JoinRoom", "ABC12345");
```

**Backend Flow:**
1. `GameHub.OnConnectedAsync()` - User connects
2. `GameHub.JoinRoom()` - Host joins room
3. `RealtimeGameService.JoinGameRoomAsync()` - Updates room info
4. SignalR broadcasts `PlayerJoined` event
5. SignalR broadcasts `RoomUpdated` event

### **PHASE 2: Guest Joins Room**

#### **2.1 Guest Discovers Room**
```javascript
// Frontend - Guest gets available rooms
const roomsResponse = await fetch('/api/game/available-rooms', {
    headers: { 'Authorization': 'Bearer ' + jwtToken }
});

const rooms = await roomsResponse.json();
// Returns: { "success": true, "data": [{"roomId": "ABC12345", "hostUserName": "Player1", ...}] }
```

#### **2.2 Guest Connects and Joins**
```javascript
// Frontend - Guest connects and joins
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/game-hub", {
        accessTokenFactory: () => localStorage.getItem('jwt_token')
    })
    .build();

await connection.start();

// Join the room
await connection.invoke("JoinRoom", "ABC12345");

// Listen for events
connection.on("PlayerJoined", (data) => {
    console.log("Player joined:", data);
    // Update UI - show both players
});

connection.on("RoomUpdated", (roomInfo) => {
    console.log("Room updated:", roomInfo);
    // Update room status in UI
});
```

**Backend Flow:**
1. `GameHub.JoinRoom()` - Guest joins room
2. `RealtimeGameService.JoinGameRoomAsync()` - Updates room with guest
3. SignalR broadcasts `PlayerJoined` to all players in room
4. SignalR broadcasts `RoomUpdated` with new room info

### **PHASE 3: Game Starts**

#### **3.1 Host Starts Game**
```javascript
// Frontend - Host starts game
await connection.invoke("StartGame", "ABC12345");

// Listen for game started
connection.on("GameStarted", (data) => {
    console.log("Game started:", data);
    // Hide lobby, show game UI
    document.getElementById('lobby').style.display = 'none';
    document.getElementById('game').style.display = 'block';
});
```

**Backend Flow:**
1. `GameHub.StartGame()` - Host starts game
2. `RealtimeGameService.StartGameAsync()` - Validates and updates room status
3. SignalR broadcasts `GameStarted` to all players
4. `RealtimeGameService.GetNextQuestionAsync()` - Gets first question for both players
5. SignalR sends `QuestionReceived` to each player individually

#### **3.2 Players Receive Questions**
```javascript
// Frontend - Listen for questions
connection.on("QuestionReceived", (question) => {
    console.log("New question:", question);
    // Display question and start timer
    displayQuestion(question);
    startTimer(question.timeLimit);
});

function displayQuestion(question) {
    document.getElementById('question').innerHTML = `
        <h3>Question ${question.questionNumber}/${question.totalQuestions}</h3>
        <p>${question.questionText}</p>
    `;
    
    const answersDiv = document.getElementById('answers');
    answersDiv.innerHTML = '';
    question.answerOptions.forEach(option => {
        const button = document.createElement('button');
        button.textContent = option.optionText;
        button.onclick = () => submitAnswer(option.id);
        answersDiv.appendChild(button);
    });
}
```

### **PHASE 4: Gameplay**

#### **4.1 Players Answer Questions**
```javascript
// Frontend - Submit answer
let gameStartTime = Date.now();

function submitAnswer(answerId) {
    const timeSpent = (Date.now() - gameStartTime) / 1000;
    
    connection.invoke("SubmitAnswer", 
        "ABC12345", 
        currentQuestion.questionId, 
        answerId, 
        timeSpent
    );
}

// Listen for answer submissions
connection.on("AnswerSubmitted", (data) => {
    console.log("Answer submitted:", data);
    // Show opponent answered
    showOpponentAnswered(data.userId);
});
```

**Backend Flow:**
1. `GameHub.SubmitAnswer()` - Player submits answer
2. `RealtimeGameService.SubmitAnswerAsync()` - Stores answer and checks completion
3. SignalR broadcasts `AnswerSubmitted` to all players
4. If both players answered all questions, game completes
5. `RealtimeGameService.GetGameResultAsync()` - Calculates final scores
6. SignalR broadcasts `GameEnded` with results

#### **4.2 Scoring System**
```csharp
// Backend - Scoring calculation
private int CalculateScore(int? userId, List<SubmitAnswerDto> answers, List<GameQuestionDto> questions)
{
    var score = 0;
    foreach (var answer in userAnswers)
    {
        if (IsCorrectAnswer(answer, questions))
        {
            // Base score: 10 points
            // Time bonus: +1 point for each second under 10 seconds
            var timeBonus = Math.Max(0, 10 - (int)answer.TimeSpent);
            score += 10 + timeBonus;
        }
    }
    return score;
}
```

### **PHASE 5: Game Completion**

#### **5.1 Game Ends**
```javascript
// Frontend - Listen for game end
connection.on("GameEnded", (result) => {
    console.log("Game ended:", result);
    // Display final results
    displayGameResult(result);
});

function displayGameResult(result) {
    document.getElementById('game').style.display = 'none';
    document.getElementById('results').style.display = 'block';
    
    document.getElementById('final-score').innerHTML = `
        <h3>Game Results</h3>
        <p>Winner: ${result.winnerUserName}</p>
        <p>Host Score: ${result.hostScore}</p>
        <p>Guest Score: ${result.guestScore}</p>
        <p>Completed at: ${new Date(result.completedAt).toLocaleString()}</p>
    `;
}
```

## 📡 **SignalR Events Documentation**

### **Client → Server (Invoke Methods)**
| Method | Parameters | Description |
|--------|------------|-------------|
| `JoinRoom` | `roomId: string` | Join a game room |
| `LeaveRoom` | `roomId: string` | Leave a game room |
| `StartGame` | `roomId: string` | Start the game (host only) |
| `SubmitAnswer` | `roomId, questionId, answerId, timeSpent` | Submit an answer |
| `GetNextQuestion` | `roomId: string` | Get next question |

### **Server → Client (Event Handlers)**
| Event | Data | Description |
|-------|------|-------------|
| `PlayerJoined` | `{userId, userName, roomId, timestamp}` | Player joined room |
| `PlayerLeft` | `{userId, roomId, timestamp}` | Player left room |
| `RoomUpdated` | `GameRoomInfoDto` | Room information updated |
| `GameStarted` | `{roomId, timestamp}` | Game started |
| `QuestionReceived` | `GameQuestionDto` | New question received |
| `AnswerSubmitted` | `{userId, questionId, roomId, timestamp}` | Answer submitted |
| `GameEnded` | `GameResultDto` | Game completed with results |
| `Error` | `string` | Error message |

## 🔧 **REST API Endpoints**

### **Game Management**
```http
POST /api/game/create-room
GET  /api/game/available-rooms
GET  /api/game/room/{roomId}
POST /api/game/join-room
POST /api/game/leave-room
POST /api/game/start-game
POST /api/game/submit-answer
GET  /api/game/game-result/{roomId}
```

### **Request/Response Examples**

#### **Create Room**
```http
POST /api/game/create-room
Content-Type: application/json
Authorization: Bearer <jwt_token>

{
    "hostUserId": 1,
    "hostUserName": "Player1",
    "quizSetId": 5,
    "timeLimit": 30
}

Response:
{
    "success": true,
    "data": "ABC12345",
    "message": "Game room created successfully"
}
```

#### **Join Room**
```http
POST /api/game/join-room
Content-Type: application/json
Authorization: Bearer <jwt_token>

{
    "roomId": "ABC12345",
    "userId": 2,
    "userName": "Player2"
}

Response:
{
    "success": true,
    "data": true,
    "message": "Successfully joined the game room"
}
```

## 🎮 **Complete Frontend Implementation**

### **HTML Structure**
```html
<!DOCTYPE html>
<html>
<head>
    <title>1vs1 Realtime Game</title>
    <script src="https://unpkg.com/@microsoft/signalr@latest/dist/browser/signalr.min.js"></script>
</head>
<body>
    <div id="app">
        <!-- Lobby Screen -->
        <div id="lobby">
            <h2>Game Lobby</h2>
            <button onclick="createRoom()">Create Room</button>
            <button onclick="joinRoom()">Join Room</button>
            <div id="room-info"></div>
        </div>
        
        <!-- Game Screen -->
        <div id="game" style="display:none">
            <h2>Game in Progress</h2>
            <div id="question-container">
                <div id="question"></div>
                <div id="answers"></div>
                <div id="timer"></div>
            </div>
        </div>
        
        <!-- Results Screen -->
        <div id="results" style="display:none">
            <h2>Game Results</h2>
            <div id="final-score"></div>
        </div>
    </div>
</body>
</html>
```

### **JavaScript Implementation**
```javascript
class RealtimeGame {
    constructor() {
        this.connection = null;
        this.currentRoomId = null;
        this.currentQuestion = null;
        this.gameStartTime = null;
    }

    async initialize() {
        // Initialize SignalR connection
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl("/game-hub", {
                accessTokenFactory: () => localStorage.getItem('jwt_token')
            })
            .build();

        // Setup event handlers
        this.setupEventHandlers();
        
        // Start connection
        await this.connection.start();
        console.log('Connected to SignalR');
    }

    setupEventHandlers() {
        this.connection.on("PlayerJoined", (data) => {
            console.log("Player joined:", data);
            this.updateRoomInfo();
        });

        this.connection.on("GameStarted", (data) => {
            console.log("Game started:", data);
            this.showGameScreen();
        });

        this.connection.on("QuestionReceived", (question) => {
            console.log("Question received:", question);
            this.displayQuestion(question);
            this.startTimer(question.timeLimit);
        });

        this.connection.on("AnswerSubmitted", (data) => {
            console.log("Answer submitted:", data);
            this.showOpponentAnswered(data.userId);
        });

        this.connection.on("GameEnded", (result) => {
            console.log("Game ended:", result);
            this.showResults(result);
        });

        this.connection.on("Error", (error) => {
            console.error("SignalR Error:", error);
            alert("Error: " + error);
        });
    }

    async createRoom() {
        try {
            const response = await fetch('/api/game/create-room', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': 'Bearer ' + localStorage.getItem('jwt_token')
                },
                body: JSON.stringify({
                    hostUserId: 1,
                    hostUserName: "Player1",
                    quizSetId: 5,
                    timeLimit: 30
                })
            });

            const result = await response.json();
            if (result.success) {
                this.currentRoomId = result.data;
                await this.connection.invoke("JoinRoom", this.currentRoomId);
                this.updateRoomInfo();
            }
        } catch (error) {
            console.error('Error creating room:', error);
        }
    }

    async joinRoom() {
        const roomId = prompt("Enter room ID:");
        if (roomId) {
            this.currentRoomId = roomId;
            await this.connection.invoke("JoinRoom", roomId);
        }
    }

    async startGame() {
        await this.connection.invoke("StartGame", this.currentRoomId);
    }

    submitAnswer(answerId) {
        const timeSpent = (Date.now() - this.gameStartTime) / 1000;
        this.connection.invoke("SubmitAnswer", 
            this.currentRoomId, 
            this.currentQuestion.questionId, 
            answerId, 
            timeSpent
        );
    }

    displayQuestion(question) {
        this.currentQuestion = question;
        this.gameStartTime = Date.now();
        
        document.getElementById('question').innerHTML = `
            <h3>Question ${question.questionNumber}/${question.totalQuestions}</h3>
            <p>${question.questionText}</p>
        `;
        
        const answersDiv = document.getElementById('answers');
        answersDiv.innerHTML = '';
        question.answerOptions.forEach(option => {
            const button = document.createElement('button');
            button.textContent = option.optionText;
            button.onclick = () => this.submitAnswer(option.id);
            answersDiv.appendChild(button);
        });
    }

    startTimer(seconds) {
        let timeLeft = seconds;
        const timerDiv = document.getElementById('timer');
        
        const timer = setInterval(() => {
            timerDiv.textContent = `Time: ${timeLeft}s`;
            timeLeft--;
            
            if (timeLeft < 0) {
                clearInterval(timer);
                this.submitAnswer(-1); // Timeout
            }
        }, 1000);
    }

    showResults(result) {
        document.getElementById('game').style.display = 'none';
        document.getElementById('results').style.display = 'block';
        
        document.getElementById('final-score').innerHTML = `
            <h3>Game Results</h3>
            <p>Winner: ${result.winnerUserName}</p>
            <p>Host Score: ${result.hostScore}</p>
            <p>Guest Score: ${result.guestScore}</p>
            <p>Completed at: ${new Date(result.completedAt).toLocaleString()}</p>
        `;
    }

    showGameScreen() {
        document.getElementById('lobby').style.display = 'none';
        document.getElementById('game').style.display = 'block';
    }

    async updateRoomInfo() {
        if (this.currentRoomId) {
            try {
                const response = await fetch(`/api/game/room/${this.currentRoomId}`, {
                    headers: { 'Authorization': 'Bearer ' + localStorage.getItem('jwt_token') }
                });
                const result = await response.json();
                if (result.success) {
                    const room = result.data;
                    document.getElementById('room-info').innerHTML = `
                        <p>Room ID: ${room.roomId}</p>
                        <p>Host: ${room.hostUserName}</p>
                        <p>Guest: ${room.guestUserName || 'Waiting...'}</p>
                        <p>Status: ${room.status}</p>
                        ${room.guestUserId ? '<button onclick="game.startGame()">Start Game</button>' : ''}
                    `;
                }
            } catch (error) {
                console.error('Error updating room info:', error);
            }
        }
    }
}

// Initialize game
const game = new RealtimeGame();
window.onload = () => game.initialize();
```

## 🧪 **Testing Guide**

### **1. Setup Test Environment**
```bash
# Start API server
cd QuizUpLearn.API
dotnet run

# Server will run on https://localhost:7000
# SignalR Hub available at: https://localhost:7000/game-hub
```

### **2. Test with 2 Browser Windows**
1. **Window 1 (Host)**:
   - Open browser to frontend
   - Click "Create Room"
   - Wait for room ID
   - Wait for guest to join

2. **Window 2 (Guest)**:
   - Open browser to frontend
   - Click "Join Room"
   - Enter room ID from Window 1
   - Wait for host to start game

3. **Both Windows**:
   - Game starts automatically
   - Answer questions in real-time
   - See opponent's progress
   - View final results

### **3. Test SignalR Connection**
```javascript
// Test connection in browser console
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/game-hub")
    .build();

await connection.start();
console.log('Connected:', connection.state);
```

## 🚀 **Production Deployment**

### **1. Environment Configuration**
```json
// appsettings.Production.json
{
  "ConnectionStrings": {
    "PostgreSqlConnection": "Host=localhost;Database=QuizUpLearn;Username=user;Password=pass"
  },
  "Jwt": {
    "Key": "your-secret-key-here",
    "Issuer": "QuizUpLearn",
    "Audience": "QuizUpLearn-Users"
  }
}
```

### **2. Scaling Considerations**
- **Redis Backplane**: For multiple server instances
- **Load Balancing**: For SignalR connections
- **Database Optimization**: Indexes for game queries
- **Monitoring**: Application insights for SignalR metrics

### **3. Security Best Practices**
- JWT token validation
- Rate limiting on API endpoints
- Input validation on all DTOs
- CORS configuration for production domains

## ✅ **Implementation Status**

- ✅ **SignalR Hub** - Complete realtime communication
- ✅ **Game Service** - Full business logic implementation
- ✅ **REST APIs** - All endpoints implemented
- ✅ **DTOs** - Complete data structures
- ✅ **Authentication** - JWT integration
- ✅ **Error Handling** - Comprehensive logging
- ✅ **Frontend Example** - Complete HTML/JS implementation
- ✅ **Documentation** - Full flow documentation
- ✅ **Testing Guide** - Step-by-step testing instructions

## 🎯 **Ready for Production!**

Hệ thống 1vs1 realtime game đã được implement hoàn chỉnh với:
- **Full SignalR integration** cho realtime communication
- **Complete game flow** từ tạo phòng đến kết thúc
- **Comprehensive documentation** với examples
- **Production-ready code** với error handling
- **Frontend implementation** sẵn sàng sử dụng

**Chỉ cần deploy và test!** 🚀
