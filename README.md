# 💬 Financial Chat - Real-Time Messaging & StockBot API (.NET + SignalR)

This project is a complete solution for a real-time chat system with user authentication via JWT and integration with a stock bot. The bot responds to stock commands using RabbitMQ messaging and fetches stock quotes from external APIs.

## 📦 Technologies Used

- ✅ ASP.NET Core 7.0 / 9.0 (Web API and SignalR)
- ✅ Entity Framework Core (SQLite)
- ✅ SignalR for real-time communication
- ✅ RabbitMQ for message queueing
- ✅ JWT Bearer Authentication
- ✅ HTML/CSS/JavaScript (frontend)
- ✅ Swagger for API documentation
- ✅ Docker (optional for RabbitMQ)

---

## 🚀 Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/Lucca-Muniz/FinancialProject.git
cd FinancialProject
````

---

### 2. Prerequisites

Make sure the following tools are installed:

* [.NET SDK 9.0 (or 7.0)](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
* [RabbitMQ](https://www.rabbitmq.com/download.html)
  (You can use Docker if preferred)
* [Visual Studio 2022+](https://visualstudio.microsoft.com/) (optional)

---

### 3. Run RabbitMQ via Docker (optional)

```bash
docker run -d --hostname rabbit --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
```

Access the RabbitMQ Management UI:
[http://localhost:15672](http://localhost:15672)
Username: `guest`
Password: `guest`

---

### 4. Restore, Build, and Run the Project

```bash
dotnet restore
dotnet build
dotnet run --project FinancialChat.Api
```

---

### 5. Access the Application

* Frontend (UI):
  [http://localhost:5197/client.html](http://localhost:5197/client.html)
* API Documentation (Swagger):
  [http://localhost:5197/swagger](http://localhost:5197/swagger)

---

## 🔐 Authentication Endpoints

* `POST /api/auth/register` – Register a new user
* `POST /api/auth/login` – Login and receive a JWT token

The SignalR client uses the JWT token via `accessTokenFactory`.

---

## 💬 How to Use the Chat

* Send regular messages to the chat room
* To get a stock quote, type:
  `/stock=AAPL.US`
  or
  `/stock=GOOGL.US`

The bot will respond automatically with the latest stock quote retrieved via RabbitMQ.

---

## 📁 Project Structure

```
FinancialProject/
├── FinancialChat.Core/            # Domain models and interfaces
├── FinancialChat.Infrastructure/  # Data access and RabbitMQ services
├── FinancialChat.Api/             # API endpoints and SignalR Hub
├── client.html                    # Frontend interface
├── FinancialChat.sln              # Solution file
```

---

## ✅ Notes & Troubleshooting

* If SignalR fails to connect:

  * Ensure CORS is properly configured
  * Ensure the JWT token is valid and passed correctly
  * RabbitMQ must be running locally

* If you're using Docker for RabbitMQ, ensure the ports 5672 and 15672 are not blocked

---

## 📜 License

This project was created as part of a technical challenge.
Use is permitted for evaluation or educational purposes only.

---

Made with 💙 by [Lucca Muniz](https://www.linkedin.com/in/lucca-developer-fullstack/)
