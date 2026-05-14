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

  function isManager() {
    const role = state.user?.role || "";
    return role === "Manager" || role === "Admin";
  }

  function setNavActive() {
    document.querySelectorAll(".bottom-nav button").forEach((btn) => {
      btn.classList.toggle("active", btn.dataset.view === state.view);
      if (btn.classList.contains("mgr-only")) {
        btn.classList.toggle("hidden", !isManager());
      }
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
        <button class="secondary" data-action="export-report" data-id="${row.reportRecordId}" title="Download as Excel (.xlsx)">Export XLSX</button>
        <button class="secondary" data-action="view-narrative" data-id="${row.reportRecordId}" title="AI summary of this report">Narrative</button>
        ${
          row.reportType === "Monthly"
            ? `<button class="secondary" data-action="export-pptx" data-id="${row.reportRecordId}" title="Download as PowerPoint (.pptx)">Export PPTX</button>`
            : ""
        }
        ${
          row.reportType === "Weekly" && row.status !== "Locked"
            ? `<button data-action="lock-weekly" data-id="${row.reportRecordId}">Lock Weekly</button>`
            : ""
        }
      </div>
    </section>`;
  }

  // ── P9B: Smart Inbox ─────────────────────────────────────────────────────

  async function loadInboxCounts() {
    try {
      const [pending, active] = await Promise.all([
        api("/inbox-tasks/pending"),
        api("/inbox-tasks")
      ]);
      const total = (pending?.length || 0) + (active?.length || 0);
      const badge = document.getElementById("inboxBadge");
      if (badge) {
        badge.textContent = total;
        badge.classList.toggle("hidden", total === 0);
      }
    } catch (_) {}
  }

  async function renderInbox() {
    content.innerHTML = `<div class="card" style="text-align:center;padding:24px">Loading inbox…</div>`;
    try {
      const [pending, active, completed] = await Promise.all([
        api("/inbox-tasks/pending"),
        api("/inbox-tasks"),
        api("/inbox-tasks/completed")
      ]);

      const total = pending.length + active.length;
      const badge = document.getElementById("inboxBadge");
      if (badge) { badge.textContent = total; badge.classList.toggle("hidden", total === 0); }

      content.innerHTML = `<div class="stack">

        ${pending.length > 0 ? `
        <div class="section-title">Needs Confirmation (${pending.length})</div>
        ${pending.map(t => inboxCard(t, "pending")).join("")}` : ""}

        <div class="section-title">Active Tasks (${active.length})</div>
        ${active.length === 0
          ? `<div class="card" style="color:#51697f;text-align:center;padding:16px">No active inbox tasks</div>`
          : active.map(t => inboxCard(t, "active")).join("")}

        ${completed.length > 0 ? `
        <div class="section-title">Completed (last 30 days)</div>
        ${completed.slice(0, 5).map(t => inboxCard(t, "done")).join("")}` : ""}

      </div>`;
    } catch (err) {
      content.innerHTML = showCardError(err.message);
    }
  }

  function inboxCard(t, mode) {
    const priorityColor = { Low: "gray", Medium: "blue", High: "orange", Critical: "red" };
    const stateColor    = { Active: "blue", InProgress: "orange", Completed: "green",
                             PendingConfirmation: "gray", Dismissed: "gray" };
    const pColor = priorityColor[t.priority] || "gray";
    const sColor = stateColor[t.state] || "gray";
    const due = t.dueAtUtc ? `<span class="small muted">Due ${new Date(t.dueAtUtc).toLocaleDateString("en-IN")}</span>` : "";

    const actions = mode === "pending" ? `
      <div class="row" style="margin-top:8px">
        <button data-action="inbox-confirm" data-id="${t.inboxTaskId}">Confirm</button>
        <button class="secondary" data-action="inbox-dismiss" data-id="${t.inboxTaskId}">Dismiss</button>
      </div>` : mode === "active" ? `
      <div class="row" style="margin-top:8px">
        ${t.state === "Active"
          ? `<button class="secondary" data-action="inbox-start" data-id="${t.inboxTaskId}">Start</button>`
          : ""}
        <button data-action="inbox-complete" data-id="${t.inboxTaskId}">Mark Done</button>
      </div>` : "";

    return `<div class="card stack" style="gap:6px">
      <div class="between">
        <span class="chip ${pColor}">${escapeHtml(t.priority)}</span>
        <span class="chip ${sColor}">${escapeHtml(t.state)}</span>
      </div>
      <div style="font-size:14px;font-weight:600;color:#143049">${escapeHtml(t.extractedTaskText)}</div>
      <div class="row">
        <span class="small muted">${escapeHtml(t.sourceChannel)} · ${escapeHtml(t.sourceSender)}</span>
        ${due}
        ${t.category !== "FollowUp" ? `<span class="pill">${escapeHtml(t.category)}</span>` : ""}
      </div>
      ${actions}
    </div>`;
  }

  // ── P10: Team Health Dashboard ───────────────────────────────────────────

  let billingChart = null;

  async function renderHealthDashboard() {
    content.innerHTML = `<div class="card" style="text-align:center;padding:24px">Loading team health…</div>`;
    try {
      const h = await api("/dashboard/team-health");

      const engagementColor = h.engagementScore >= 80 ? "#16a34a"
                            : h.engagementScore >= 60 ? "#ea580c" : "#dc2626";

      const maxTrend = Math.max(...(h.weeklyTrend.map(t => t.count)), 1);

      content.innerHTML = `
        <div class="stack">

          <!-- Stat cards row -->
          <div class="stat-grid">
            <div class="stat-card ${h.participationPct >= 80 ? "green" : h.participationPct >= 60 ? "orange" : "red"}">
              <div class="stat-value">${h.participationPct}%</div>
              <div class="stat-label">Participation today</div>
            </div>
            <div class="stat-card ${h.blockedTickets.length === 0 ? "green" : "red"}">
              <div class="stat-value">${h.blockedTickets.length}</div>
              <div class="stat-label">Blocked tickets</div>
            </div>
            <div class="stat-card ${h.pendingAchievements + h.pendingSales === 0 ? "green" : "orange"}">
              <div class="stat-value">${h.pendingAchievements + h.pendingSales}</div>
              <div class="stat-label">Pending validations</div>
            </div>
            <div class="stat-card ${h.overdueActionItems === 0 ? "blue" : "red"}">
              <div class="stat-value">${h.openActionItems}</div>
              <div class="stat-label">Open action items</div>
            </div>
          </div>

          <!-- Engagement score + billing donut -->
          <div class="grid">
            <div class="card">
              <div class="section-title">Team Engagement Score</div>
              <div class="engagement-ring">
                <canvas id="engagementChart" width="160" height="160"></canvas>
                <div class="ring-label">
                  <div class="ring-score" style="color:${engagementColor}">${h.engagementScore}</div>
                  <div class="ring-sub">/ 100</div>
                </div>
              </div>
            </div>
            <div class="card">
              <div class="section-title">Billing Type Breakdown</div>
              <div class="chart-wrap">
                <canvas id="billingChart" height="160"></canvas>
              </div>
            </div>
          </div>

          <!-- Weekly participation trend -->
          <div class="card">
            <div class="section-title">Daily Participation — Last 7 Days</div>
            <div class="trend-bar-wrap" id="trendBars">
              ${h.weeklyTrend.map(t => {
                const pct = Math.round((t.count / h.totalTeam) * 100);
                const heightPx = Math.max(4, Math.round((t.count / maxTrend) * 52));
                const dayLabel = new Date(t.date).toLocaleDateString("en-IN", { weekday: "short" });
                return `<div class="trend-bar-col" title="${dayLabel}: ${t.count}/${h.totalTeam} (${pct}%)">
                  <div class="trend-bar" style="height:${heightPx}px"></div>
                  <div class="trend-bar-label">${dayLabel}</div>
                </div>`;
              }).join("")}
            </div>
          </div>

          <!-- Missing submitters -->
          ${h.missingNames.length ? `
          <div class="card">
            <div class="section-title">Missing Today (${h.missingNames.length})</div>
            <div class="chip-list">
              ${h.missingNames.map(n => `<span class="chip orange">${escapeHtml(n)}</span>`).join("")}
            </div>
          </div>` : `
          <div class="card">
            <div class="section-title">Submissions</div>
            <div class="chip green" style="display:inline-block">All ${h.totalTeam} members submitted today</div>
          </div>`}

          <!-- Blocked tickets -->
          ${h.blockedTickets.length ? `
          <div class="card">
            <div class="section-title">Blocked Tickets</div>
            <div class="stack">
              ${h.blockedTickets.map(t => `
                <div class="between">
                  <span class="chip red">${escapeHtml(t.ticketNumber)}</span>
                  <span class="small muted" style="flex:1;margin-left:8px">${escapeHtml(t.description)}</span>
                </div>`).join("")}
            </div>
          </div>` : ""}

          <!-- Pending validations breakdown -->
          ${h.pendingAchievements + h.pendingSales > 0 ? `
          <div class="card">
            <div class="section-title">Pending Validations</div>
            <div class="chip-list">
              ${h.pendingAchievements > 0 ? `<span class="chip orange">${h.pendingAchievements} achievements</span>` : ""}
              ${h.pendingSales > 0 ? `<span class="chip orange">${h.pendingSales} sales enquiries</span>` : ""}
            </div>
          </div>` : ""}

        </div>`;

      // Engagement doughnut (ring chart)
      if (billingChart) { billingChart.destroy(); billingChart = null; }

      new Chart(document.getElementById("engagementChart"), {
        type: "doughnut",
        data: {
          datasets: [{
            data: [h.engagementScore, 100 - h.engagementScore],
            backgroundColor: [engagementColor, "#e2e8f0"],
            borderWidth: 0,
            circumference: 360
          }]
        },
        options: {
          cutout: "72%",
          plugins: { legend: { display: false }, tooltip: { enabled: false } },
          animation: { duration: 600 }
        }
      });

      // Billing type donut
      const billingLabels = h.billingBreakdown.map(b => b.type);
      const billingData   = h.billingBreakdown.map(b => b.count);
      const billingColors = ["#1e6ea7","#16a34a","#ea580c","#7c3aed","#0891b2"];

      billingChart = new Chart(document.getElementById("billingChart"), {
        type: "doughnut",
        data: {
          labels: billingLabels,
          datasets: [{
            data: billingData,
            backgroundColor: billingColors.slice(0, billingLabels.length),
            borderWidth: 2,
            borderColor: "#fff"
          }]
        },
        options: {
          plugins: {
            legend: { position: "bottom", labels: { font: { size: 11 }, padding: 8 } }
          },
          animation: { duration: 600 }
        }
      });

      // P16: load nudge feed and append below the health dashboard
      await renderNudgeFeed();

    } catch (err) {
      content.innerHTML = showCardError(err.message);
    }
  }

  // ── P16: Smart Nudges feed ────────────────────────────────────────────────

  async function loadNudgeCounts() {
    if (!isManager()) return;
    try {
      const counts = await api("/nudges/counts");
      const badge = document.getElementById("nudgeBadge");
      if (badge) {
        if (counts.total > 0) {
          badge.textContent = counts.total;
          badge.classList.remove("hidden");
        } else {
          badge.classList.add("hidden");
        }
      }
    } catch (_) { /* non-critical */ }
  }

  async function renderNudgeFeed() {
    const nudgeContainer = document.createElement("div");
    nudgeContainer.id = "nudgeFeed";
    nudgeContainer.innerHTML = `<div class="section-title" style="margin-top:18px">Smart Nudges</div>
      <div class="card" style="text-align:center;padding:16px;color:#51697f;font-size:13px">Loading nudges…</div>`;
    content.querySelector(".stack").appendChild(nudgeContainer);

    try {
      const n = await api("/nudges");

      const badge = document.getElementById("nudgeBadge");
      if (badge) {
        if (n.totalCount > 0) { badge.textContent = n.totalCount; badge.classList.remove("hidden"); }
        else badge.classList.add("hidden");
      }

      if (n.totalCount === 0) {
        nudgeContainer.innerHTML = `
          <div class="section-title" style="margin-top:18px">Smart Nudges</div>
          <div class="card nudge-clear">
            <span style="font-size:18px">✅</span>
            <span>No nudges — all items are on track!</span>
          </div>`;
        return;
      }

      let html = `<div class="section-title" style="margin-top:18px">Smart Nudges <span class="nudge-badge-inline">${n.totalCount}</span></div>`;

      if (n.staleEnquiries.length > 0) {
        html += `<div class="card nudge-card">
          <div class="nudge-card-header orange">
            <span class="nudge-icon">📭</span>
            <strong>Stale Sales Enquiries (${n.staleEnquiries.length})</strong>
            <span class="nudge-meta">Pending &gt; 7 days</span>
          </div>
          <div class="stack" style="gap:6px;margin-top:8px">
            ${n.staleEnquiries.map(e => `
              <div class="nudge-row">
                <span class="chip orange">${escapeHtml(e.clientName)}</span>
                <span class="nudge-tech muted">${escapeHtml(e.technology)}</span>
                <span class="nudge-age">${e.daysOld}d old</span>
                <span class="muted small">${escapeHtml(e.salesCoordinator)}</span>
              </div>`).join("")}
          </div>
        </div>`;
      }

      if (n.blockedTickets.length > 0) {
        html += `<div class="card nudge-card">
          <div class="nudge-card-header red">
            <span class="nudge-icon">🚧</span>
            <strong>Blocked Ticket Streaks (${n.blockedTickets.length})</strong>
            <span class="nudge-meta">Blocked &ge; 3 days</span>
          </div>
          <div class="stack" style="gap:6px;margin-top:8px">
            ${n.blockedTickets.map(b => `
              <div class="nudge-row">
                <span class="chip red">${escapeHtml(b.ticketNumber)}</span>
                <span class="nudge-tech">${escapeHtml(b.userName)}</span>
                <span class="nudge-age">${b.blockedDays}d blocked</span>
                <span class="muted small">${escapeHtml(b.projectCode)}</span>
              </div>`).join("")}
          </div>
        </div>`;
      }

      if (n.pendingAchievements.length > 0) {
        html += `<div class="card nudge-card">
          <div class="nudge-card-header blue">
            <span class="nudge-icon">🏆</span>
            <strong>Overdue Pending Achievements (${n.pendingAchievements.length})</strong>
            <span class="nudge-meta">Awaiting validation &gt; 5 days</span>
          </div>
          <div class="stack" style="gap:6px;margin-top:8px">
            ${n.pendingAchievements.map(a => `
              <div class="nudge-row">
                <span class="chip blue">${escapeHtml(a.category)}</span>
                <span class="nudge-tech">${escapeHtml(a.title)}</span>
                <span class="nudge-age">${a.daysOld}d pending</span>
                <span class="muted small">${escapeHtml(a.userName)}</span>
              </div>`).join("")}
          </div>
        </div>`;
      }

      nudgeContainer.innerHTML = html;
    } catch (err) {
      nudgeContainer.innerHTML = `<div class="section-title" style="margin-top:18px">Smart Nudges</div>
        <div class="card" style="color:#8b1f1b;padding:12px;font-size:13px">Could not load nudges: ${escapeHtml(err.message)}</div>`;
    }
  }

  // ── P11: Compliance Heatmap ───────────────────────────────────────────────

  async function renderHeatmap() {
    const today = new Date();
    const end   = toIso(today);
    const start30 = new Date(today); start30.setDate(today.getDate() - 29);
    const start = toIso(start30);

    content.innerHTML = `
      <div class="stack">
        <div class="card">
          <div class="date-range-row">
            <label>From<input type="date" id="hmStart" value="${start}" /></label>
            <label>To<input type="date" id="hmEnd"   value="${end}"   /></label>
            <button id="hmLoad" style="align-self:flex-end">Load</button>
          </div>
        </div>
        <div id="hmResult"></div>
      </div>`;

    document.getElementById("hmLoad").addEventListener("click", () => {
      const s = document.getElementById("hmStart").value;
      const e = document.getElementById("hmEnd").value;
      loadHeatmap(s, e);
    });

    loadHeatmap(start, end);
  }

  async function loadHeatmap(startDate, endDate) {
    const wrap = document.getElementById("hmResult");
    wrap.innerHTML = `<div class="card" style="text-align:center;padding:16px">Loading…</div>`;
    try {
      const data = await api(`/daily-updates/compliance-heatmap/range?startDate=${startDate}&endDate=${endDate}`);
      if (!data.users.length) {
        wrap.innerHTML = `<div class="card"><p class="muted">No team members found.</p></div>`;
        return;
      }

      // Build date headers — show only weekdays abbreviated
      const headerCells = data.dates.map(d => {
        const dt  = new Date(d);
        const dow = dt.getUTCDay();
        const isWeekend = dow === 0 || dow === 6;
        const label = isWeekend ? "" : dt.toLocaleDateString("en-IN", { day: "numeric", month: "short" });
        return `<th title="${d}" style="font-size:9px;writing-mode:vertical-rl;transform:rotate(180deg);height:48px;padding:2px 1px">${label}</th>`;
      }).join("");

      const bodyRows = data.users.map(u => {
        const cells = u.days.map(d => {
          if (d.isWeekend) return `<td><div class="hm-cell weekend"></div></td>`;
          const cls  = d.submitted ? (d.status === "Blocked" ? "blocked" : "submitted") : "missing";
          const tip  = `${d.date}: ${d.submitted ? d.status : "Not submitted"}`;
          return `<td><div class="hm-cell ${cls}" title="${tip}"></div></td>`;
        }).join("");
        return `<tr>
          <td class="name-col" title="${escapeHtml(u.name)}">${escapeHtml(u.name)}</td>
          ${cells}
        </tr>`;
      }).join("");

      wrap.innerHTML = `
        <div class="card">
          <div class="section-title">Submission Heatmap — ${startDate} to ${endDate}</div>
          <div class="heatmap-wrap">
            <table class="heatmap-table">
              <thead><tr><th class="name-col">Member</th>${headerCells}</tr></thead>
              <tbody>${bodyRows}</tbody>
            </table>
          </div>
          <div class="heatmap-legend">
            <div class="heatmap-legend-item">
              <div class="heatmap-legend-box" style="background:#16a34a"></div>Submitted
            </div>
            <div class="heatmap-legend-item">
              <div class="heatmap-legend-box" style="background:#dc2626"></div>Blocked
            </div>
            <div class="heatmap-legend-item">
              <div class="heatmap-legend-box" style="background:#e2e8f0"></div>Missing
            </div>
            <div class="heatmap-legend-item">
              <div class="heatmap-legend-box" style="background:transparent;border:1px dashed #cbd5e1"></div>Weekend
            </div>
          </div>
        </div>`;
    } catch (err) {
      wrap.innerHTML = showCardError(err.message);
    }
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
      health:    "Team Health",
      heatmap:   "Compliance Heatmap",
      inbox:     "Smart Inbox",
      tasks:     "Tasks",
      daily:     "Daily Update",
      feed:      "Feed",
      leaderboard: "Leaderboard",
      validation: "Validation Queue",
      reports:   "Reports",
      profile:   "Profile"
    };
    viewTitle.textContent = titles[state.view] || "Kudos App";

    switch (state.view) {
      case "health":
        return renderHealthDashboard();
      case "heatmap":
        return renderHeatmap();
      case "inbox":
        return renderInbox();
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
      loadNudgeCounts();
      loadInboxCounts();
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
    if (action === "export-pptx") {
      const id = Number(event.target.dataset.id);
      await handleExport(id, "pptx");
    }
    if (action === "view-narrative") {
      const id = Number(event.target.dataset.id);
      const btn = event.target;
      btn.disabled = true;
      btn.textContent = "Loading…";
      try {
        const data = await api(`/reports/${id}/narrative`);
        showNarrativeModal(data);
      } finally {
        btn.disabled = false;
        btn.textContent = "Narrative";
      }
    }
    // ── P9B: Inbox actions ──────────────────────────────────────────────────
    if (action === "inbox-confirm") {
      const id = Number(event.target.dataset.id);
      await api(`/inbox-tasks/${id}/confirm`, {
        method: "POST",
        body: { category: "FollowUp", priority: "Medium", dueAtUtc: null }
      });
      await renderInbox();
    }
    if (action === "inbox-dismiss") {
      const id = Number(event.target.dataset.id);
      await api(`/inbox-tasks/${id}/dismiss`, { method: "POST" });
      await renderInbox();
    }
    if (action === "inbox-start") {
      const id = Number(event.target.dataset.id);
      await api(`/inbox-tasks/${id}/state`, { method: "PUT", body: { state: "InProgress" } });
      await renderInbox();
    }
    if (action === "inbox-complete") {
      const id = Number(event.target.dataset.id);
      await api(`/inbox-tasks/${id}/complete`, {
        method: "POST",
        body: { includeInWeeklyReport: false, weeklyReportCategory: null }
      });
      await renderInbox();
      loadInboxCounts();
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

  async function handleExport(reportId, format = "xlsx") {
    try {
      const artifact = await api(`/reports/${reportId}/export?format=${format}`);
      const binary = atob(artifact.base64Content);
      const bytes = new Uint8Array(binary.length);
      for (let i = 0; i < binary.length; i++) bytes[i] = binary.charCodeAt(i);
      const blob = new Blob([bytes], { type: artifact.contentType });
      const url  = URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = url;
      link.download = artifact.fileName;
      link.click();
      URL.revokeObjectURL(url);
    } catch (error) {
      alert(error.message);
    }
  }

  function showNarrativeModal(data) {
    const existing = document.getElementById("narrativeModal");
    if (existing) existing.remove();

    const overlay = document.createElement("div");
    overlay.id = "narrativeModal";
    overlay.style.cssText = "position:fixed;inset:0;background:rgba(0,0,0,.5);z-index:9999;display:flex;align-items:center;justify-content:center;padding:1rem";

    const box = document.createElement("div");
    box.style.cssText = "background:#fff;border-radius:12px;padding:1.5rem;max-width:640px;width:100%;max-height:80vh;overflow-y:auto;box-shadow:0 8px 32px rgba(0,0,0,.3)";

    const ts = new Date(data.generatedAt).toLocaleString();
    box.innerHTML = `
      <h3 style="margin:0 0 .75rem">Report Narrative</h3>
      <p style="line-height:1.6;margin:0 0 1rem">${escHtml(data.narrative)}</p>
      <div class="small muted" style="margin-bottom:1rem">Generated ${ts}${data.isAiGenerated ? " · AI-generated" : " · Rule-based"}</div>
      <button id="narrativeClose" class="primary">Close</button>`;

    overlay.appendChild(box);
    document.body.appendChild(overlay);

    document.getElementById("narrativeClose").onclick = () => overlay.remove();
    overlay.onclick = (e) => { if (e.target === overlay) overlay.remove(); };
  }

  function escHtml(str) {
    return String(str).replace(/&/g,"&amp;").replace(/</g,"&lt;").replace(/>/g,"&gt;");
  }

  function toIso(date) {
    return date.toISOString().slice(0, 10);
  }

  paint();
  loadNudgeCounts();
  loadInboxCounts();

  // ── P20: Register service worker ─────────────────────────────────────────
  if ("serviceWorker" in navigator) {
    navigator.serviceWorker.register("/sw.js", { scope: "/" })
      .then((reg) => {
        // Check for updates every time the app is opened
        reg.update();
        reg.addEventListener("updatefound", () => {
          const newWorker = reg.installing;
          if (!newWorker) return;
          newWorker.addEventListener("statechange", () => {
            if (newWorker.state === "installed" && navigator.serviceWorker.controller) {
              // New version available — show a subtle toast
              const toast = document.createElement("div");
              toast.style.cssText =
                "position:fixed;bottom:80px;left:50%;transform:translateX(-50%);" +
                "background:#17324a;color:#fff;padding:10px 18px;border-radius:8px;" +
                "font-size:13px;z-index:999;box-shadow:0 2px 8px rgba(0,0,0,.2)";
              toast.innerHTML =
                'Update available. <button onclick="location.reload()" ' +
                'style="background:#1e6ea7;border:none;color:#fff;border-radius:6px;' +
                'padding:4px 10px;margin-left:8px;cursor:pointer;font:inherit">Refresh</button>';
              document.body.appendChild(toast);
              setTimeout(() => toast.remove(), 12000);
            }
          });
        });
      })
      .catch((err) => console.warn("SW registration failed:", err));

    // Notify user when they go offline / come back online
    window.addEventListener("offline", () => {
      showConnectionBanner("You are offline — changes will sync when reconnected.", "#ea580c");
    });
    window.addEventListener("online", () => {
      showConnectionBanner("Back online.", "#16a34a");
      loadNudgeCounts();
    });
  }
})();

function showConnectionBanner(msg, color) {
  const existing = document.getElementById("connBanner");
  if (existing) existing.remove();
  const banner = document.createElement("div");
  banner.id = "connBanner";
  banner.style.cssText =
    `position:fixed;top:0;left:0;right:0;background:${color};color:#fff;` +
    "text-align:center;padding:8px;font-size:13px;font-weight:600;z-index:999;";
  banner.textContent = msg;
  document.body.prepend(banner);
  setTimeout(() => banner.remove(), 5000);
}
