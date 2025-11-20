using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.Enums
{
    public enum GameModeEnum
    {
        OneVsOne = 0, 
        Multiplayer = 1 
    }
    public enum OneVsOneRoomStatus
    {
        Waiting = 0,        // Đang chờ players join
        Ready = 1,          // Đủ người (1vs1: 2 players, Multiplayer: >= 2 players), chờ start
        InProgress = 2,     // Đang chơi
        ShowingResult = 3,  // Đang hiển thị kết quả round
        Completed = 4,      // Đã kết thúc
        Cancelled = 5       // Đã hủy
    }
    public enum GameStatus
    {
        Lobby = 0,          // Đang chờ người chơi
        InProgress = 1,     // Đang chơi
        ShowingResult = 2,  // Đang hiển thị kết quả câu hỏi
        ShowingLeaderboard = 3, // Đang hiển thị bảng xếp hạng
        Completed = 4,      // Đã kết thúc
        Cancelled = 5       // Đã hủy
    }
}
