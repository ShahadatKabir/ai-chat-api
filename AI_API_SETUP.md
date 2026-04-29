# AI API Configuration Guide

## Overview
This project supports both **Gemini API** and **Gemma API** for content generation. You can configure which provider to use globally or override on a per-request basis.

## Environment Setup

### Gemini API (Default)

1. Get your API key from [Google AI Studio](https://makersuite.google.com/app/apikey)

2. Set up environment variables:
   ```powershell
   $env:GEMINI_API_KEY="your-gemini-api-key"
   $env:GEMINI_MODEL="gemini-pro"  # or gemini-1.5-pro, etc.
   ```

3. Or configure in `appsettings.json`:
   ```json
   {
     "Gemini": {
       "ApiKey": "your-gemini-api-key",
       "Model": "gemini-pro"
     },
     "AiProvider": "Gemini"
   }
   ```

### Gemma API (Alternative)

1. Get your API key from [Google AI Studio](https://makersuite.google.com/app/apikey) or your Gemma provider

2. Set up environment variables:
   ```powershell
   $env:GEMMA_API_KEY="your-gemma-api-key"
   $env:GEMMA_MODEL="gemma-7b"
   ```

3. Or configure in `appsettings.json`:
   ```json
   {
     "Gemma": {
       "ApiKey": "your-gemma-api-key",
       "Model": "gemma-7b",
       "BaseUrl": "https://generativelanguage.googleapis.com/v1beta/models"
     },
     "AiProvider": "Gemma"
   }
   ```

## API Usage

### Chat Endpoint

**Endpoint:** `POST /api/chat`

**Authentication:** Required (Bearer token)

**Request Body:**
```json
{
  "prompt": "Your question here",
  "model": "gemini-pro",
  "temperature": 0.7,
  "maxOutputTokens": 256
}
```

**Parameters:**
- `prompt` (string, required): The message to send to the AI
- `model` (string, optional): The model to use. If it contains "gemma", Gemma API is used; otherwise Gemini API is used. Defaults to configured model.
- `temperature` (float, optional): Controls randomness (0.0-1.0). Default: 0.7
- `maxOutputTokens` (integer, optional): Maximum response length. Default: 256

**Response:**
```json
{
  "responseText": "Generated response",
  "model": "gemini-pro",
  "temperature": 0.7,
  "maxOutputTokens": 256
}
```

## Docker Deployment

### Build and Run with Docker Compose

```bash
# Set environment variables
$env:GEMINI_API_KEY="your-key"
$env:JWT_KEY="your-secret-key"

# Build and start
docker-compose up --build

# Alternatively using the batch script
docker-run.bat build
docker-run.bat run
```

### Using Docker Run

```bash
docker build -t aichatapi .

docker run -d `
  --name aichatapi-container `
  -p 5000:5000 `
  -e GEMINI_API_KEY="your-gemini-key" `
  -e JWT_KEY="your-jwt-key" `
  -e GEMMA_API_KEY="your-gemma-key" `
  aichatapi
```

## Switching Between Providers

### Globally in Configuration
Update `appsettings.json`:
```json
{
  "AiProvider": "Gemini"  // or "Gemma"
}
```

### Per Request
In your chat request, specify the model:
```json
{
  "prompt": "Your question",
  "model": "gemma-7b"  // Will automatically use Gemma API
}
```

## Available Models

### Gemini Models
- `gemini-pro` (Default)
- `gemini-1.5-pro`
- `gemini-pro-vision`

### Gemma Models
- `gemma-7b`
- `gemma-7b-it` (Instruction-tuned)

## Testing

### Using cURL

```bash
# Get JWT token first
$token = "your-jwt-token"

# Make a chat request
curl -X POST http://localhost:5000/api/chat `
  -H "Authorization: Bearer $token" `
  -H "Content-Type: application/json" `
  -d '{
    "prompt": "What is C#?",
    "model": "gemini-pro",
    "temperature": 0.7,
    "maxOutputTokens": 256
  }'
```

### Using PowerShell

```powershell
$token = "your-jwt-token"
$uri = "http://localhost:5000/api/chat"

$body = @{
    prompt = "Explain .NET"
    model = "gemini-pro"
    temperature = 0.7
    maxOutputTokens = 256
} | ConvertTo-Json

$headers = @{
    Authorization = "Bearer $token"
    "Content-Type" = "application/json"
}

Invoke-RestMethod -Uri $uri -Method Post -Headers $headers -Body $body
```

## Troubleshooting

### "API key is not configured"
- Verify environment variables are set correctly
- Check `appsettings.json` configuration
- Ensure the API key is valid and not expired

### "Model not found"
- Check the model name spelling
- Verify the model is available for your API key
- Check API documentation for available models

### CORS Issues
- CORS is enabled for all origins by default
- Modify `Program.cs` if you need to restrict domains

### Rate Limiting
- The project includes rate limiting middleware
- Check `RateLimitingMiddleware.cs` for configuration
- Adjust limits based on your API plan

## Dependencies

- **Newtonsoft.Json**: JSON serialization/deserialization
- **Microsoft.AspNetCore.Authentication.JwtBearer**: JWT authentication
- **Swashbuckle.AspNetCore**: Swagger/OpenAPI documentation

## References

- [Google Gemini API Documentation](https://ai.google.dev/docs)
- [Gemma Model Overview](https://ai.google.dev/models)
- [REST API Reference](https://generativelanguage.googleapis.com/docs)
