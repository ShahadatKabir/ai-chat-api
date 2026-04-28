# AI Chat API (.NET + Gemini)

## 🚀 Requirements
- .NET 10 SDK installed

---

## ✅ Features
- **User Management**: Register and login with username/password, JWT authentication
- **User Profiles**: Update user profile information (first name, last name, bio)
- **Chat with Gemini**: Protected chat endpoint using Google's Gemini AI models
- **Session Management**: Create, switch, and manage conversation sessions
- **Chat History**: In-memory storage with search and export capabilities
- **Analytics**: Detailed analytics and conversation summaries
- **Favorites**: Mark and manage favorite messages
- **Feedback System**: Rate and comment on chat interactions
- **Prompt Templates**: Create, share, and manage reusable chat prompts
- **Rate Limiting**: Built-in rate limiting to prevent abuse
- **Export**: Export chat history as JSON or CSV
- **Swagger UI**: Interactive API documentation with Bearer auth support
- **Health Checks**: Basic health and status endpoints

---

## 🐳 Docker Deployment

### Quick Start with Docker Compose

1. **Clone and navigate to the project directory**

2. **Set up environment variables:**
   ```bash
   cp .env.example .env
   # Edit .env with your actual API keys
   ```

3. **Build and run with Docker Compose:**
   ```bash
   docker-compose up --build
   ```

4. **Access the API:**
   - API: http://localhost:5000
   - Swagger UI: http://localhost:5000/swagger
   - Health Check: http://localhost:5000/api/test/health

### Manual Docker Build

```bash
# Build the image
docker build -t aichatapi .

# Run the container
docker run -p 5000:5000 \
  -e JWT_KEY=your-jwt-key \
  -e GEMINI_API_KEY=your-gemini-key \
  aichatapi
```

### Environment Variables

Create a `.env` file or set these environment variables:

- `JWT_KEY`: Secret key for JWT token signing
- `GEMINI_API_KEY`: Your Google Gemini API key
- `GEMINI_MODEL`: AI model to use (default: gemini-pro)

### Using Makefile (Linux/Mac)

If you have `make` installed, you can use the provided Makefile for convenience:

```bash
# Build and run
make build
make run

# Or use docker-compose
make up

# View logs
make logs

# Stop container
make stop

# Clean up
make clean
```

### Export/Import Docker Image

To share the image with another machine:

```bash
# Export image
docker save aichatapi > aichatapi.tar

# Transfer aichatapi.tar to another machine

# Import image on target machine
docker load < aichatapi.tar
```

### Windows Batch Script

For Windows users, use the provided `docker-run.bat` script:

```cmd
REM Set environment variables first
set JWT_KEY=your-jwt-key
set GEMINI_API_KEY=your-gemini-key

REM Build and run
docker-run.bat build
docker-run.bat run

REM View logs
docker-run.bat logs

REM Stop container
docker-run.bat stop
```

---

## 💻 Local Development

Open terminal inside project folder:

```bash
dotnet run
```

Navigate to `http://localhost:5000/swagger` for API documentation.

---

## 📚 API Endpoints

### Authentication
- `POST /api/auth/register` - Register a new user
- `POST /api/auth/login` - Login and get JWT token
- `GET /api/auth/profile` - Get current user profile

### Chat
- `POST /api/chat` - Send message to Gemini AI (requires auth)

### Sessions
- `POST /api/sessions` - Create new session
- `GET /api/sessions` - List all sessions
- `GET /api/sessions/{id}` - Get session details
- `POST /api/sessions/{id}/switch` - Switch to session
- `GET /api/sessions/{id}/history` - Get session chat history
- `DELETE /api/sessions/{id}` - Delete session

### History
- `GET /api/chat/history` - Get all chat history
- `DELETE /api/chat/history` - Clear all history
- `GET /api/chat/history/export/json` - Export history as JSON
- `GET /api/chat/history/export/csv` - Export history as CSV

### Search
- `GET /api/search/keyword?keyword=...` - Search by keyword
- `GET /api/search/daterange?startDate=...&endDate=...` - Search by date range

### Analytics
- `GET /api/analytics` - Get overall analytics
- `GET /api/analytics/session/{sessionId}` - Get session analytics
- `GET /api/analytics/session/{sessionId}/summary` - Get session summary

### Favorites
- `POST /api/favorites/{messageId}` - Add message to favorites
- `DELETE /api/favorites/{messageId}` - Remove from favorites
- `GET /api/favorites` - Get user's favorites
- `GET /api/favorites/{messageId}/status` - Check if message is favorited

### Test
- `GET /api/test` - Basic API test
- `GET /api/test/health` - Health check

---

## 🔧 Configuration

Set your Gemini API key in `appsettings.json`:

```json
{
  "Gemini": {
    "ApiKey": "your-api-key-here",
    "Model": "gemini-pro"
  },
  "Jwt": {
    "Key": "your-jwt-secret-key"
  }
}
```

Or use environment variables (recommended for Docker):
- `JWT_KEY`
- `GEMINI_API_KEY`
- `GEMINI_MODEL`
