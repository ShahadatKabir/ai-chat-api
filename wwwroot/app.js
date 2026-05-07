const state = {
  token: localStorage.getItem("aiChatToken") || "",
  user: JSON.parse(localStorage.getItem("aiChatUser") || "null"),
  authMode: "login",
  sessions: [],
  activeSessionId: localStorage.getItem("aiChatSessionId") || "",
  templates: []
};

const elements = {
  authPanel: document.querySelector("#authPanel"),
  userPanel: document.querySelector("#userPanel"),
  userName: document.querySelector("#userName"),
  authForm: document.querySelector("#authForm"),
  loginTab: document.querySelector("#loginTab"),
  registerTab: document.querySelector("#registerTab"),
  emailField: document.querySelector("#emailField"),
  emailInput: document.querySelector("#emailInput"),
  usernameInput: document.querySelector("#usernameInput"),
  passwordInput: document.querySelector("#passwordInput"),
  authSubmit: document.querySelector("#authSubmit"),
  logoutButton: document.querySelector("#logoutButton"),
  sessionList: document.querySelector("#sessionList"),
  newSessionButton: document.querySelector("#newSessionButton"),
  activeSessionTitle: document.querySelector("#activeSessionTitle"),
  chatLog: document.querySelector("#chatLog"),
  chatForm: document.querySelector("#chatForm"),
  promptInput: document.querySelector("#promptInput"),
  modelSelect: document.querySelector("#modelSelect"),
  temperatureInput: document.querySelector("#temperatureInput"),
  temperatureValue: document.querySelector("#temperatureValue"),
  tokensInput: document.querySelector("#tokensInput"),
  exportJsonButton: document.querySelector("#exportJsonButton"),
  exportCsvButton: document.querySelector("#exportCsvButton"),
  templateNameInput: document.querySelector("#templateNameInput"),
  templateList: document.querySelector("#templateList"),
  saveTemplateButton: document.querySelector("#saveTemplateButton"),
  toast: document.querySelector("#toast")
};

function authHeaders(extra = {}) {
  return {
    "Content-Type": "application/json",
    Authorization: `Bearer ${state.token}`,
    ...extra
  };
}

async function api(path, options = {}) {
  const response = await fetch(path, {
    ...options,
    headers: state.token ? authHeaders(options.headers) : { "Content-Type": "application/json", ...(options.headers || {}) }
  });

  if (response.status === 401) {
    clearAuth();
    throw new Error("Please log in again.");
  }

  if (!response.ok) {
    const text = await response.text();
    let message = text || response.statusText;
    try {
      const parsed = JSON.parse(text);
      message = parsed.error || parsed.detail || message;
    } catch {
      // Keep plain text errors readable.
    }
    throw new Error(message);
  }

  if (response.status === 204) {
    return null;
  }

  return response.json();
}

function showToast(message) {
  elements.toast.textContent = message;
  elements.toast.classList.add("show");
  window.clearTimeout(showToast.timeout);
  showToast.timeout = window.setTimeout(() => elements.toast.classList.remove("show"), 3200);
}

function setAuthMode(mode) {
  state.authMode = mode;
  const isRegister = mode === "register";
  elements.loginTab.classList.toggle("active", !isRegister);
  elements.registerTab.classList.toggle("active", isRegister);
  elements.emailField.classList.toggle("hidden", !isRegister);
  elements.emailInput.required = isRegister;
  elements.authSubmit.textContent = isRegister ? "Create account" : "Login";
}

function persistAuth(token, user) {
  state.token = token;
  state.user = user;
  localStorage.setItem("aiChatToken", token);
  localStorage.setItem("aiChatUser", JSON.stringify(user));
  renderAuth();
}

function clearAuth() {
  state.token = "";
  state.user = null;
  state.sessions = [];
  state.templates = [];
  state.activeSessionId = "";
  localStorage.removeItem("aiChatToken");
  localStorage.removeItem("aiChatUser");
  localStorage.removeItem("aiChatSessionId");
  renderAuth();
  renderSessions();
  renderTemplates();
  renderMessages([]);
}

function renderAuth() {
  const signedIn = Boolean(state.token);
  elements.authPanel.classList.toggle("hidden", signedIn);
  elements.userPanel.classList.toggle("hidden", !signedIn);
  elements.userName.textContent = state.user?.username || "User";
}

function renderSessions() {
  elements.sessionList.innerHTML = "";
  if (!state.sessions.length) {
    elements.sessionList.innerHTML = '<p class="empty">Log in to load sessions.</p>';
    elements.activeSessionTitle.textContent = "Default Conversation";
    return;
  }

  for (const session of state.sessions) {
    const button = document.createElement("button");
    button.type = "button";
    button.className = `session-item ${session.id === state.activeSessionId ? "active" : ""}`;
    button.innerHTML = `<strong>${escapeHtml(session.title || "Untitled")}</strong><span class="meta">${session.messageCount || 0} messages</span>`;
    button.addEventListener("click", () => selectSession(session.id));
    elements.sessionList.appendChild(button);
  }

  const active = state.sessions.find(session => session.id === state.activeSessionId) || state.sessions[0];
  elements.activeSessionTitle.textContent = active?.title || "Default Conversation";
}

function renderTemplates() {
  elements.templateList.innerHTML = "";
  if (!state.templates.length) {
    elements.templateList.innerHTML = '<p class="empty">Saved prompts appear here.</p>';
    return;
  }

  for (const template of state.templates) {
    const button = document.createElement("button");
    button.type = "button";
    button.className = "template-item";
    button.innerHTML = `<strong>${escapeHtml(template.name)}</strong><span class="meta">${escapeHtml(template.description || "Click to use")}</span>`;
    button.addEventListener("click", () => {
      elements.promptInput.value = template.template || "";
      elements.promptInput.focus();
    });
    elements.templateList.appendChild(button);
  }
}

function renderMessages(items) {
  elements.chatLog.innerHTML = "";
  if (!items.length) {
    elements.chatLog.innerHTML = '<p class="empty">Start a conversation from the composer below.</p>';
    return;
  }

  for (const item of items) {
    appendMessage("user", item.userMessage, new Date(item.timestamp).toLocaleString());
    appendMessage("assistant", item.botResponse, `${item.model || "AI"} · ${item.maxOutputTokens || 0} tokens`);
  }
}

function appendMessage(role, text, meta = "") {
  const wrapper = document.createElement("article");
  wrapper.className = `message ${role}`;
  wrapper.innerHTML = `<div class="bubble">${escapeHtml(text)}</div>${meta ? `<span class="meta">${escapeHtml(meta)}</span>` : ""}`;
  elements.chatLog.appendChild(wrapper);
  elements.chatLog.scrollTop = elements.chatLog.scrollHeight;
}

function escapeHtml(value) {
  return String(value || "")
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
}

async function loadAppData() {
  if (!state.token) {
    renderAuth();
    renderSessions();
    renderTemplates();
    renderMessages([]);
    return;
  }

  try {
    const [sessions, templates] = await Promise.all([
      api("/api/sessions"),
      api("/api/templates")
    ]);
    state.sessions = sessions;
    state.templates = templates;
    if (!state.sessions.some(session => session.id === state.activeSessionId)) {
      state.activeSessionId = state.sessions[0]?.id || "";
    }
    if (state.activeSessionId) {
      localStorage.setItem("aiChatSessionId", state.activeSessionId);
    }
    renderAuth();
    renderSessions();
    renderTemplates();
    await loadHistory();
  } catch (error) {
    showToast(error.message);
  }
}

async function loadHistory() {
  if (!state.activeSessionId) {
    renderMessages([]);
    return;
  }

  const items = await api(`/api/sessions/${encodeURIComponent(state.activeSessionId)}/history`);
  renderMessages(items);
}

async function selectSession(sessionId) {
  state.activeSessionId = sessionId;
  localStorage.setItem("aiChatSessionId", sessionId);
  try {
    await api(`/api/sessions/${encodeURIComponent(sessionId)}/switch`, { method: "POST" });
    renderSessions();
    await loadHistory();
  } catch (error) {
    showToast(error.message);
  }
}

elements.loginTab.addEventListener("click", () => setAuthMode("login"));
elements.registerTab.addEventListener("click", () => setAuthMode("register"));

elements.authForm.addEventListener("submit", async event => {
  event.preventDefault();
  const username = elements.usernameInput.value.trim();
  const password = elements.passwordInput.value;
  const email = elements.emailInput.value.trim();

  try {
    if (state.authMode === "register") {
      await api("/api/auth/register", {
        method: "POST",
        body: JSON.stringify({ username, email, password })
      });
      showToast("Account created. Signing you in.");
    }

    const result = await api("/api/auth/login", {
      method: "POST",
      body: JSON.stringify({ username, password })
    });
    persistAuth(result.token, result.user);
    elements.authForm.reset();
    await loadAppData();
  } catch (error) {
    showToast(error.message);
  }
});

elements.logoutButton.addEventListener("click", clearAuth);

elements.newSessionButton.addEventListener("click", async () => {
  if (!state.token) {
    showToast("Log in before creating a session.");
    return;
  }

  const title = window.prompt("Session title", "New Conversation");
  if (!title?.trim()) {
    return;
  }

  try {
    const session = await api("/api/sessions", {
      method: "POST",
      body: JSON.stringify({ title: title.trim() })
    });
    state.sessions.unshift(session);
    await selectSession(session.id);
  } catch (error) {
    showToast(error.message);
  }
});

elements.chatForm.addEventListener("submit", async event => {
  event.preventDefault();
  if (!state.token) {
    showToast("Log in to chat.");
    return;
  }

  const prompt = elements.promptInput.value.trim();
  if (!prompt) {
    return;
  }

  appendMessage("user", prompt, "Sending");
  elements.promptInput.value = "";

  try {
    const response = await api("/api/chat", {
      method: "POST",
      body: JSON.stringify({
        prompt,
        sessionId: state.activeSessionId,
        model: elements.modelSelect.value,
        temperature: Number(elements.temperatureInput.value),
        maxOutputTokens: Number(elements.tokensInput.value)
      })
    });
    appendMessage("assistant", response.responseText, `${response.model} · ${response.maxOutputTokens} tokens`);
    await loadAppData();
  } catch (error) {
    showToast(error.message);
    await loadHistory().catch(() => {});
  }
});

elements.temperatureInput.addEventListener("input", () => {
  elements.temperatureValue.textContent = elements.temperatureInput.value;
});

elements.exportJsonButton.addEventListener("click", () => exportHistory("json"));
elements.exportCsvButton.addEventListener("click", () => exportHistory("csv"));

async function exportHistory(format) {
  if (!state.token) {
    showToast("Log in before exporting history.");
    return;
  }

  const session = state.activeSessionId ? `?sessionId=${encodeURIComponent(state.activeSessionId)}` : "";
  try {
    const response = await fetch(`/api/chat/history/export/${format}${session}`, {
      headers: { Authorization: `Bearer ${state.token}` }
    });
    if (!response.ok) {
      throw new Error("Export failed.");
    }
    const blob = await response.blob();
    const disposition = response.headers.get("content-disposition") || "";
    const match = disposition.match(/filename="?([^"]+)"?/i);
    const filename = match?.[1] || `chat_history.${format}`;
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    link.remove();
    URL.revokeObjectURL(url);
  } catch (error) {
    showToast(error.message);
  }
}

elements.saveTemplateButton.addEventListener("click", async () => {
  if (!state.token) {
    showToast("Log in before saving templates.");
    return;
  }

  const name = elements.templateNameInput.value.trim();
  const template = elements.promptInput.value.trim();
  if (!name || !template) {
    showToast("Add a template name and prompt text first.");
    return;
  }

  try {
    await api("/api/templates", {
      method: "POST",
      body: JSON.stringify({
        name,
        template,
        description: "Saved from the chat console",
        isPublic: false
      })
    });
    elements.templateNameInput.value = "";
    await loadAppData();
    showToast("Template saved.");
  } catch (error) {
    showToast(error.message);
  }
});

renderAuth();
setAuthMode("login");
loadAppData();
