# appsettings.json


{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=Manh-Laptop\\SQLEXPRESS;Uid=sa;Pwd=12345;Database=QuizUpLearnDB;Trusted_Connection=true;TrustServerCertificate=true;MultipleActiveResultSets=True;"
  },
  "Jwt": {
    "Key": "u8hK3pL0sXz9Nd2A4vYb7Rf5Qw9Zx1Cm",
    "Issuer": "QuizUpLearn",
    "Audience": "QuizUpLearnClient",
    "ExpiresMinutes": 120,
    "RefreshExpiresDays": 7
  },
  "Cloudinary": {
    "CloudName": "dh6m4qqhr",
    "APIKey": "739149964496264",
    "APISecret": "C3IgiXbQtwZHFjg7IeSctV59JTs"
  },
  "MailerSend": {
    "ApiKey": "mlsn.5b7c69a115b1c21634735bb6358cac29d724d25df732c477676e4c86e0026066",
    "FromEmail": "no-reply@test-69oxl5e79rzl785k.mlsender.net",
    "FromName": "QuizUpLearn",
    "BaseUrl": "https://api.mailersend.com",
    "EmailEndpoint": "/v1/email"
  },
  "Gemini": {
    "ApiKey": "AIzaSyBShA_ksvzBrQ6A6AWCpNxjfQT3yTZtr34"
  },
  "AllowedHosts": "*"
}
