# AI Chat API - Simple Project Guide

This project is a simple web application where a user can:
- create an account,
- log in,
- chat with an AI model,
- save conversations,
- manage favorite messages,
- search old chats,
- export history,
- and use a clean API system.

The project is built using .NET and works as a backend API. A browser page is also included so a user can use it without writing extra code.

---

## 1) What this project is doing

Think of this project as a small AI chat app like ChatGPT for a private system.

It does these main jobs:

1. User login and registration
   - A person can sign up with a username, email, and password.
   - The app validates the data.
   - It creates a secure token (JWT) for the user after login.

2. AI chat feature
   - A user sends a message to the AI.
   - The app sends that message to Google Gemini or Gemma.
   - The AI responds with an answer.
   - The answer is returned to the user.

3. Sessions and chat history
   - The app keeps conversation sessions.
   - Each session can have many messages.
   - Old conversations can be loaded later.

4. Search and export
   - A user can search chats by keyword or date.
   - Chat history can be exported in JSON or CSV.

5. Favorites and feedback
   - A user can mark messages as favorites.
   - The app can track likes or comments.

6. Prompt templates
   - Users can save reusable prompt text.
   - This helps repeat common tasks.

7. Security and rate limiting
   - The API blocks unauthorized users.
   - It also limits how many requests a user can make.

---

## 2) How this project works from start to finish

### Step 1: The app starts
When the project runs, the .NET app starts from Program.cs.

This file does important setup:
- configures CORS so the browser can talk to the API,
- configures JWT authentication,
- adds Swagger for API testing,
- registers all services,
- maps controllers,
- serves static files from the web folder.

### Step 2: A user reaches the website
The app includes a frontend in the wwwroot folder.

This frontend contains:
- login/register form,
- chat box,
- session list,
- template list,
- settings,
- messages.

### Step 3: User creates account
When someone clicks Create account, the browser sends a POST request to:

POST /api/auth/register

The request includes:
- username,
- email,
- password.

The API checks if the user already exists. If not, it saves the user and stores a password hash.

### Step 4: User logs in
When login happens, the browser sends:

POST /api/auth/login

If the username and password match, the app creates a JWT token and sends it back.

That token is then sent in every future request as a Bearer token.

### Step 5: User asks AI a question
The browser sends the request to:

POST /api/chat

This API receives:
- the user question,
- model name,
- temperature,
- max output length,
- session information.

### Step 6: AI service talks to Gemini
The backend uses a service class (GeminiService or GemmaService) to call the AI model.

The app uses API keys and model names from configuration.

### Step 7: Response is returned and saved
The AI answer is returned to the user.

At the same time, the app also stores:
- user input,
- AI response,
- timestamp,
- session details,
- model used.

This makes history and search possible.

### Step 8: User can manage sessions and history
The app can:
- create a new conversation,
- switch between sessions,
- load old conversations,
- delete messages,
- export as JSON or CSV,
- search by keywords or date.

---

## 3) Main project structure

Here is the meaning of the main folders:

### Controllers
These files handle API requests. Examples:
- AuthController: login/register/profile
- ChatController: send chat requests
- SessionController: session management
- HistoryController: history and export
- FavoritesController: favorites
- AnalyticsController: stats
- SearchController: search
- PromptTemplateController: saved prompts

### Models
These files define the data shapes.
Examples:
- User
- ChatRequest
- ChatResponse
- ChatSession
- Favorite
- ChatHistoryItem
- PromptTemplate

### Services
This is where the real logic lives.
Examples:
- GeminiService: sends requests to Google Gemini
- GemmaService: sends requests to Gemma model
- PersistenceUserService: manages users and login logic
- PersistenceChatHistoryService: saves chat records
- FavoritesService: manages saved messages
- PromptTemplateService: manages saved prompts

### wwwroot
This folder contains the website frontend:
- index.html
- app.js
- styles.css

This is what people see in the browser.

### Data files
These store app data locally:
- data/history.json
- data/sessions.json
- data/users.json

This means the project can work without a database.

---

## 4) Technologies used

This project uses a few simple but powerful technologies:

### .NET
This is the main framework for building the application.
It helps create web APIs and servers.

### ASP.NET Core Web API
This is the engine that exposes HTTP routes such as:
- /api/auth/login
- /api/chat
- /api/sessions

### C#
This is the language used to write the backend code.

### JWT Authentication
JWT stands for JSON Web Token.
It is used to check whether a user is logged in.

### Swagger / OpenAPI
This gives a built-in API documentation page.
You can test the endpoints from the browser without writing custom clients.

### Google Gemini / Gemma
This is the AI model provider.
The app sends the incoming prompt to Gemini or Gemma and receives the answer.

### JSON files for storage
Instead of a database, the project uses local JSON files for storing data.
This is easy for beginners and simple projects.

### HTML, CSS, JavaScript
These build the simple front-end page users interact with.

---

## 5) How to run this project

### Step 1: Install .NET SDK
Install .NET 10 or a supported version from Microsoft.

Check version:

```bash
dotnet --version
```

### Step 2: Open the project folder
Open the folder where this project is saved.

### Step 3: Restore dependencies
Run:

```bash
dotnet restore
```

### Step 4: Build the project
Run:

```bash
dotnet build
```

### Step 5: Start the app
Run:

```bash
dotnet run
```

### Step 6: Open in browser
Visit:

```text
http://localhost:5000
```

For API docs:

```text
http://localhost:5000/swagger
```

---

## 6) Required configuration

The app needs some values in appsettings.json or environment variables.

### JWT secret key
This is used to keep user logins secure.

### Gemini API key
This is required to connect to the Google AI model.

### Model name
Example: gemini-pro

### Optional Gemma settings
If using Gemma, you may set a local API URL.

Example settings are already in appsettings.json.

---

## 7) How to create a .NET project like this

Here is the simple process to create a project like this from scratch.

### Step 1: Create the project
Run:

```bash
dotnet new webapi -n MyAiApp
```

This creates a new ASP.NET Core Web API project.

### Step 2: Add NuGet packages
Add needed packages for security and Swagger.

```bash
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package Swashbuckle.AspNetCore
dotnet add package Newtonsoft.Json
```

### Step 3: Create folders
Create folders such as:
- Controllers
- Models
- Services
- Data

### Step 4: Create Program.cs setup
Configure:
- CORS,
- authentication,
- Swagger,
- dependency injection,
- static files,
- controllers.

### Step 5: Create a controller
Example:
- AuthController for login/register
- ChatController for AI chat

### Step 6: Create models
Example:
- User
- ChatRequest
- ChatResponse

### Step 7: Create services
Services contain business logic.
Example:
- AI call service,
- user service,
- history service.

### Step 8: Add appsettings.json
Store settings like:
- JWT key,
- API key,
- model name,
- rate limits.

### Step 9: Run and test
Run:

```bash
dotnet run
```

Then open Swagger or the browser to test it.

---

## 8) How this project is managed in a simple way

The project is easy to manage because it is organized into 3 layers:

### 1. Frontend layer
This is the browser UI.
It shows forms and buttons.

### 2. API layer
This is the backend logic.
It handles requests and sends responses.

### 3. Service layer
This is where the actual work happens.
Examples:
- validate login,
- call AI,
- save chat history,
- search data.

This is the usual pattern in .NET projects.

---

## 9) Why this project is useful

This project is useful because it shows how to build a real-world application with:
- login and security,
- AI integration,
- session management,
- saving user data,
- working with JSON,
- full API design,
- and a simple browser UI.

It is a very good example for:
- student projects,
- demo apps,
- AI service prototypes,
- internal business tools.

---

## 10) Very simple summary

If someone asks, “What is this project?” the simplest answer is:

“This is a small .NET AI chat application where users can register, log in, chat with Gemini or Gemma, save sessions, search history, and manage their chats through a web interface.”

In plain words:

“It is like a private chat app powered by AI, built with Microsoft .NET technology.”

---

## 11) Best practice for future improvement

If you want to make this project bigger and more professional, you can later add:
- a real database like SQL Server or PostgreSQL,
- image upload support,
- user roles like admin and normal user,
- email confirmation,
- cloud deployment,
- more security rules,
- Docker production setup,
- better UI with React or Blazor.

---

## 12) Final note

This project is a great beginner-friendly example of:
- user management,
- API development,
- AI integration,
- .NET backend basics,
- and simple project architecture.

Even a non-technical person can understand the main idea:

> It is an AI-powered chat application that lets people sign in, talk to an AI, and save their conversations.

---

## 13) Quick command list

```bash
dotnet restore
dotnet build
dotnet run
```

Then open:

```text
http://localhost:5000
http://localhost:5000/swagger
```

---

End of guide.
