(function () {
  const storage = {
    token: "kudos.token",
    user: "kudos.user",
    dailyDraft: "kudos.daily.draft"
  };

  const state = {
    token: localStorage.getItem(storage.token) || "",
    user: tryParse(localStorage.getItem(storage.user)),
    view: "dashboard",
    feedPage: 1
  };

  const loginView = document.getElementById("loginView");
  const appView = document.getElementById("appView");
  const loginForm = document.getElementById("loginForm");
  const loginError = document.getElementById("loginError");
  const content = document.getElementById("content");
  const viewTitle = document.getElementById("viewTitle");
  const logoutBtn = document.getElementById("logoutBtn");

  function tryParse(value) {
    if (!value) return null;
    try {
      return JSON.parse(value);
    } catch {
      return null;
    }
  }

  function setLoginError(message) {
    if (!message) {
      loginError.classList.add("hidden");
      loginError.textContent = "";
      return;
    }

    loginError.classList.remove("hidden");
    loginError.textContent = message;
  }

  function setSession(token, user) {
    state.token = token;
    state.user = user;
    localStorage.setItem(storage.token, token);
    localStorage.setItem(storage.user, JSON.stringify(user));
  }

  function clearSession() {
    state.token = "";
    state.user = null;
    localStorage.removeItem(storage.token);
    localStorage.removeItem(storage.user);
  }

  async function api(path, options) {
    const response = await fetch(`/api${path}`, {
      method: options?.method || "GET",
      headers: {
        "Content-Type": "application/json",
        ...(state.token ? { Authorization: `Bearer ${state.token}` } : {})
      },
      body: options?.body ? JSON.stringify(options.body) : undefined
    });

    if (response.status === 401) {
      clearSession();
      paint();
      throw new Error("Session expired. Please login again.");
    }

    if (!response.ok) {
      const text = await response.text();
      throw new Error(text || `Request failed (${response.status})`);
    }

    if (response.status === 204) {
      return null;
    }

    return response.json();
  }

  function setNavActive() {
    document.querySelectorAll(".bottom-nav button").forEach((btn) => {
      btn.classList.toggle("active", btn.dataset.view === state.view);
    });
  }

  function showCardError(message) {
    return `<div class="error">${escapeHtml(message)}</div>`;
  }

  function escapeHtml(value) {
    return String(value)
      .replaceAll("&", "&amp;")
      .replaceAll("<", "&lt;")
      .replaceAll(">", "&gt;")
      .replaceAll('"', "&quot;")
      .replaceAll("'", "&#039;");
  }

  async function renderDashboard() {
    try {
      const [dashboard, profile] = await Promise.all([api("/dashboard"), api("/performance/my")]);
      content.innerHTML = `
        <div class="grid">
          <section class="card">
            <h3>Pending Tasks</h3>
            <div class="pill">${dashboard.pendingTasks}</div>
            <p class="muted">Tasks waiting for your vote or response.</p>
          </section>
          <section class="card">
            <h3>Daily Update</h3>
            <div class="pill">${dashboard.hasTodayUpdate ? "Submitted" : "Pending"}</div>
            <p class="muted">Mandatory for all users every day.</p>
          </section>
          <section class="card">
            <h3>Points</h3>
            <div class="pill">${dashboard.currentMonthPoints}</div>
            <p class="muted">Rank: ${dashboard.rank || "N/A"}</p>
          </section>
          <section class="card">
            <h3>Badges</h3>
            <p>${(profile.badges || []).join(", ") || "No badges yet"}</p>
          </section>
        </div>`;
    } catch (error) {
      content.innerHTML = showCardError(error.message);
    }
  }

  async function renderTasks() {
    try {
      const tasks = await api("/tasks/active");
      if (!tasks.length) {
        content.innerHTML = `<section class="card"><p>No active tasks.</p></section>`;
        return;
      }

      content.innerHTML = tasks
        .map(
          (task) => `
            <section class="card stack" data-task-card="${task.taskId}">
              <div class="between">
                <h3>${escapeHtml(task.title)}</h3>
                <span class="small muted">${new Date(task.dueAtUtc).toLocaleDateString()}</span>
              </div>
              <p>${escapeHtml(task.description || "")}</p>
              <label>
                Option
                <select data-field="option">
                  <option value="">Select option</option>
                  <option value="Yes">Yes</option>
                  <option value="No">No</option>
                  <option value="Maybe">Maybe</option>
                </select>
              </label>
              <label>
                Remark
                <textarea data-field="remark" placeholder="Optional remark"></textarea>
              </label>
              <div class="row">
                <button data-action="submit-task" data-task-id="${task.taskId}">Submit</button>
                <button class="secondary" data-action="load-task-report" data-task-id="${task.taskId}">View report</button>
              </div>
              <div data-field="task-msg"></div>
              <div data-field="task-report"></div>
            </section>`
        )
        .join("");
    } catch (error) {
      content.innerHTML = showCardError(error.message);
    }
  }

  async function renderDaily() {
    const draft = tryParse(localStorage.getItem(storage.dailyDraft)) || {
      projectId: "",
      workDate: new Date().toISOString().slice(0, 10),
      ticketNumber: "",
      description: "",
      status: "InProgress"
    };

    try {
      const projects = await api("/master-data/projects");
      const projectOptions = ['<option value="">Select project</option>']
        .concat(
          projects.map(
            (project) =>
              `<option value="${project.projectId}" ${String(project.projectId) === String(draft.projectId) ? "selected" : ""}>
                ${escapeHtml(project.projectName)}
              </option>`
          )
        )
        .join("");

      content.innerHTML = `
        <form id="dailyForm" class="card stack">
          <h3>Daily Update</h3>
          <label>Project<select name="projectId" required>${projectOptions}</select></label>
          <label>Work Date<input type="date" name="workDate" value="${draft.workDate}" required /></label>
          <label>Status
            <select name="status">
              ${["Open", "InProgress", "Completed", "Blocked", "NoTask"]
                .map((status) => `<option value="${status}" ${status === draft.status ? "selected" : ""}>${status}</option>`)
                .join("")}
            </select>
          </label>
          <label>Ticket Number<input name="ticketNumber" value="${escapeHtml(draft.ticketNumber)}" /></label>
          <label>Description<textarea name="description" required>${escapeHtml(draft.description)}</textarea></label>
          <div class="row">
            <button type="submit">Submit update</button>
            <button type="button" class="secondary" id="clearDraftBtn">Clear draft</button>
          </div>
          <div id="dailyMsg"></div>
        </form>`;

      const form = document.getElementById("dailyForm");
      form.addEventListener("input", () => {
        const payload = formToObject(form);
        localStorage.setItem(storage.dailyDraft, JSON.stringify(payload));
      });

      document.getElementById("clearDraftBtn").addEventListener("click", () => {
        localStorage.removeItem(storage.dailyDraft);
        renderDaily();
      });

      form.addEventListener("submit", async (event) => {
        event.preventDefault();
        const payload = formToObject(form);
        const submitPayload = {
          projectId: Number(payload.projectId),
          workDate: payload.workDate,
          ticketNumber: payload.status === "NoTask" ? "NO-TASK" : payload.ticketNumber,
          description: payload.description,
          status: payload.status
        };

        const msg = document.getElementById("dailyMsg");
        try {
          await api("/daily-updates", { method: "POST", body: submitPayload });
          msg.className = "success";
          msg.textContent = "Daily update submitted.";
          localStorage.removeItem(storage.dailyDraft);
        } catch (error) {
          msg.className = "error";
          msg.textContent = error.message;
        }
      });
    } catch (error) {
      content.innerHTML = showCardError(error.message);
    }
  }

  function formToObject(form) {
    return Object.fromEntries(new FormData(form).entries());
  }

  async function renderFeed() {
    state.feedPage = 1;
    try {
      const rows = await api(`/feed?page=${state.feedPage}&pageSize=10`);
      content.innerHTML = `
        <div id="feedRows" class="stack">
          ${rows.map(feedCard).join("")}
        </div>
        <button id="loadMoreFeed" class="secondary">Load more</button>`;

      document.getElementById("loadMoreFeed").addEventListener("click", async () => {
        state.feedPage += 1;
        const more = await api(`/feed?page=${state.feedPage}&pageSize=10`);
        document.getElementById("feedRows").insertAdjacentHTML("beforeend", more.map(feedCard).join(""));
      });
    } catch (error) {
      content.innerHTML = showCardError(error.message);
    }
  }

  function feedCard(item) {
    return `<section class="card">
      <span class="pill">${escapeHtml(item.kind)}</span>
      <h3>${escapeHtml(item.title)}</h3>
      <p>${escapeHtml(item.description || "")}</p>
    </section>`;
  }

  async function renderLeaderboard() {
    const now = new Date();
    try {
      const rows = await api(`/performance/leaderboard?year=${now.getUTCFullYear()}&month=${now.getUTCMonth() + 1}`);
      content.innerHTML = `<div class="stack">
        ${rows
          .map(
            (row, index) => `
              <section class="card between">
                <strong>#${index + 1}</strong>
                <span>${escapeHtml(row.name)}</span>
                <span>${row.points} pts</span>
              </section>`
          )
          .join("")}
      </div>`;
    } catch (error) {
      content.innerHTML = showCardError(error.message);
    }
  }

  async function renderValidation() {
    try {
      const rows = await api("/validations/pending");
      if (!rows.length) {
        content.innerHTML = `<section class="card"><p>No pending validations.</p></section>`;
        return;
      }

      content.innerHTML = rows
        .map(
          (row) => `
            <section class="card stack" data-validation-card="${row.validationRecordId}">
              <div class="between">
                <strong>${escapeHtml(row.entityType)}</strong>
                <span class="small muted">Entity ${row.entityId}</span>
              </div>
              <label>Remarks<textarea data-field="remarks" placeholder="Optional remarks"></textarea></label>
              <div class="row">
                <button data-action="validate" data-id="${row.validationRecordId}" data-status="Approved">Approve</button>
                <button class="secondary" data-action="validate" data-id="${row.validationRecordId}" data-status="Rejected">Reject</button>
              </div>
              <div data-field="validation-msg"></div>
            </section>`
        )
        .join("");
    } catch (error) {
      content.innerHTML = showCardError(error.message);
    }
  }

  async function renderReports() {
    try {
      const rows = await api("/reports");
      content.innerHTML = `
        <section class="card stack">
          <h3>Generate Reports</h3>
          <div class="row">
            <button data-action="generate-weekly">Weekly</button>
            <button data-action="generate-monthly">Monthly</button>
            <button data-action="generate-quarterly">Quarterly</button>
          </div>
          <div id="reportActionMsg"></div>
        </section>
        <div class="stack" id="reportRows">
          ${rows.map(reportCard).join("")}
        </div>`;
    } catch (error) {
      content.innerHTML = showCardError(error.message);
    }
  }

  function reportCard(row) {
    return `<section class="card stack">
      <div class="between">
        <strong>${escapeHtml(row.reportType)} #${row.reportRecordId}</strong>
        <span class="pill">${escapeHtml(row.status)}</span>
      </div>
      <div class="small muted">${row.startDate} to ${row.endDate}</div>
      <div class="row">
        <button class="secondary" data-action="export-report" data-id="${row.reportRecordId}">Export</button>
        ${
          row.reportType === "Weekly" && row.status !== "Locked"
            ? `<button data-action="lock-weekly" data-id="${row.reportRecordId}">Lock Weekly</button>`
            : ""
        }
      </div>
    </section>`;
  }

  async function renderProfile() {
    const profile = state.user;
    content.innerHTML = `
      <section class="card stack">
        <h3>${escapeHtml(profile?.name || "")}</h3>
        <div>Email: ${escapeHtml(profile?.email || "")}</div>
        <div>Role: ${escapeHtml(profile?.role || "")}</div>
        <div>Team: ${escapeHtml(String(profile?.teamId || ""))}</div>
      </section>`;
  }

  async function paintView() {
    setNavActive();
    const titles = {
      dashboard: "Dashboard",
      tasks: "Tasks",
      daily: "Daily Update",
      feed: "Feed",
      leaderboard: "Leaderboard",
      validation: "Validation Queue",
      reports: "Reports",
      profile: "Profile"
    };
    viewTitle.textContent = titles[state.view] || "Kudos App";

    switch (state.view) {
      case "tasks":
        return renderTasks();
      case "daily":
        return renderDaily();
      case "feed":
        return renderFeed();
      case "leaderboard":
        return renderLeaderboard();
      case "validation":
        return renderValidation();
      case "reports":
        return renderReports();
      case "profile":
        return renderProfile();
      default:
        return renderDashboard();
    }
  }

  function paint() {
    const authenticated = Boolean(state.token && state.user);
    loginView.classList.toggle("hidden", authenticated);
    appView.classList.toggle("hidden", !authenticated);
    if (authenticated) {
      paintView();
    }
  }

  loginForm.addEventListener("submit", async (event) => {
    event.preventDefault();
    const email = document.getElementById("loginEmail").value.trim();
    const zohoAccessToken = document.getElementById("loginToken").value.trim();

    try {
      const result = await api("/auth/zoho-sso", {
        method: "POST",
        body: { email, zohoAccessToken }
      });
      setSession(result.token, result.user);
      setLoginError("");
      state.view = "dashboard";
      paint();
    } catch (error) {
      setLoginError(error.message);
    }
  });

  logoutBtn.addEventListener("click", () => {
    clearSession();
    state.view = "dashboard";
    paint();
  });

  document.querySelector(".bottom-nav").addEventListener("click", (event) => {
    const button = event.target.closest("button[data-view]");
    if (!button) return;
    state.view = button.dataset.view;
    paintView();
  });

  content.addEventListener("click", async (event) => {
    const action = event.target.dataset.action;
    if (!action) return;

    if (action === "submit-task") {
      const taskId = Number(event.target.dataset.taskId);
      const card = event.target.closest("[data-task-card]");
      const option = card.querySelector('[data-field="option"]').value;
      const remark = card.querySelector('[data-field="remark"]').value;
      const msg = card.querySelector('[data-field="task-msg"]');

      try {
        await api(`/tasks/${taskId}/respond`, { method: "POST", body: { option, remark } });
        msg.className = "success";
        msg.textContent = "Response submitted.";
      } catch (error) {
        msg.className = "error";
        msg.textContent = error.message;
      }
    }

    if (action === "load-task-report") {
      const taskId = Number(event.target.dataset.taskId);
      const card = event.target.closest("[data-task-card]");
      const wrap = card.querySelector('[data-field="task-report"]');
      try {
        const rows = await api(`/tasks/${taskId}/report`);
        if (!rows.length) {
          wrap.innerHTML = `<p class="small muted">No responses yet.</p>`;
          return;
        }

        wrap.innerHTML = `
          <div class="small">
            ${rows
              .map(
                (row) =>
                  `<div>${escapeHtml(row.name)} | ${escapeHtml(row.option)} | ${escapeHtml(row.remark || "")}</div>`
              )
              .join("")}
          </div>`;
      } catch (error) {
        wrap.innerHTML = showCardError(error.message);
      }
    }

    if (action === "validate") {
      const validationId = Number(event.target.dataset.id);
      const status = event.target.dataset.status;
      const card = event.target.closest("[data-validation-card]");
      const remarks = card.querySelector('[data-field="remarks"]').value;
      const msg = card.querySelector('[data-field="validation-msg"]');
      try {
        await api(`/validations/${validationId}/decision`, {
          method: "POST",
          body: { status, remarks }
        });
        msg.className = "success";
        msg.textContent = `${status} successfully.`;
        await renderValidation();
      } catch (error) {
        msg.className = "error";
        msg.textContent = error.message;
      }
    }

    if (action === "generate-weekly") {
      await handleGenerateWeekly();
    }
    if (action === "generate-monthly") {
      await handleGenerateMonthly();
    }
    if (action === "generate-quarterly") {
      await handleGenerateQuarterly();
    }
    if (action === "lock-weekly") {
      const id = Number(event.target.dataset.id);
      await handleLockWeekly(id);
    }
    if (action === "export-report") {
      const id = Number(event.target.dataset.id);
      await handleExport(id);
    }
  });

  async function handleGenerateWeekly() {
    const today = new Date();
    const day = today.getUTCDay() || 7;
    const start = new Date(today);
    start.setUTCDate(today.getUTCDate() - day + 1);
    const end = new Date(start);
    end.setUTCDate(start.getUTCDate() + 6);

    const msg = document.getElementById("reportActionMsg");
    try {
      await api(`/reports/weekly/generate?startDate=${toIso(start)}&endDate=${toIso(end)}`, { method: "POST", body: {} });
      msg.className = "success";
      msg.textContent = "Weekly report generated.";
      await renderReports();
    } catch (error) {
      msg.className = "error";
      msg.textContent = error.message;
    }
  }

  async function handleGenerateMonthly() {
    const now = new Date();
    const msg = document.getElementById("reportActionMsg");
    try {
      await api(`/reports/monthly/generate?year=${now.getUTCFullYear()}&month=${now.getUTCMonth() + 1}`, {
        method: "POST",
        body: {}
      });
      msg.className = "success";
      msg.textContent = "Monthly report generated.";
      await renderReports();
    } catch (error) {
      msg.className = "error";
      msg.textContent = error.message;
    }
  }

  async function handleGenerateQuarterly() {
    const now = new Date();
    const month = now.getUTCMonth() + 1;
    const quarter = Math.floor((month - 1) / 3) + 1;
    const msg = document.getElementById("reportActionMsg");
    try {
      await api(`/reports/quarterly/generate?year=${now.getUTCFullYear()}&quarter=${quarter}`, { method: "POST", body: {} });
      msg.className = "success";
      msg.textContent = "Quarterly report generated.";
      await renderReports();
    } catch (error) {
      msg.className = "error";
      msg.textContent = error.message;
    }
  }

  async function handleLockWeekly(reportId) {
    try {
      await api(`/reports/weekly/${reportId}/submit`, { method: "POST", body: {} });
      await renderReports();
    } catch (error) {
      alert(error.message);
    }
  }

  async function handleExport(reportId) {
    try {
      const artifact = await api(`/reports/${reportId}/export?format=excel`);
      const binary = atob(artifact.base64Content);
      const bytes = new Uint8Array(binary.length);
      for (let i = 0; i < binary.length; i++) {
        bytes[i] = binary.charCodeAt(i);
      }
      const blob = new Blob([bytes], { type: artifact.contentType });
      const url = URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = url;
      link.download = artifact.fileName;
      link.click();
      URL.revokeObjectURL(url);
    } catch (error) {
      alert(error.message);
    }
  }

  function toIso(date) {
    return date.toISOString().slice(0, 10);
  }

  paint();
})();
