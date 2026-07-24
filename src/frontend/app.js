const LANCAMENTOS = "http://localhost:5001";
const CONSOLIDADO = "http://localhost:5002";

const els = {
  healthGrid: document.getElementById("healthGrid"),
  entriesBody: document.getElementById("entriesBody"),
  balancesBody: document.getElementById("balancesBody"),
  outboxBody: document.getElementById("outboxBody"),
  eventsBody: document.getElementById("eventsBody"),
  entryForm: document.getElementById("entryForm"),
  formMsg: document.getElementById("formMsg"),
  btnRefresh: document.getElementById("btnRefresh"),
};

els.entryForm.date.value = new Date().toISOString().slice(0, 10);

function money(value) {
  return Number(value).toLocaleString("pt-BR", { style: "currency", currency: "BRL" });
}

function fmtDate(value) {
  if (!value) return "—";
  return String(value).slice(0, 10);
}

function fmtDateTime(value) {
  if (!value) return "—";
  return new Date(value).toLocaleString("pt-BR");
}

async function getJson(url) {
  const response = await fetch(url);
  if (!response.ok) throw new Error(`${response.status} ${url}`);
  if (response.status === 204) return null;
  return response.json();
}

function setMsg(text, ok) {
  els.formMsg.hidden = !text;
  els.formMsg.textContent = text;
  els.formMsg.className = `msg ${ok ? "ok" : "err"}`;
}

function renderHealth(lancamentos, consolidado) {
  const pending = lancamentos?.pendingOutboxCount ?? "—";
  const pills = [
    { ok: !!lancamentos, text: lancamentos ? `Lançamentos: Healthy · outbox ${pending}` : "Lançamentos: offline" },
    { ok: !!consolidado, text: consolidado ? "Consolidado: Healthy" : "Consolidado: offline" },
  ];

  els.healthGrid.innerHTML = pills
    .map((p) => `<div class="pill ${p.ok ? "ok" : "err"}">${p.text}</div>`)
    .join("");
}

function renderEntries(items) {
  if (!items?.length) {
    els.entriesBody.innerHTML = `<tr><td colspan="4">Nenhum lançamento ainda.</td></tr>`;
    return;
  }

  els.entriesBody.innerHTML = items
    .map((item) => {
      const type = item.type ?? item.Type;
      const css = String(type).toLowerCase() === "debit" ? "debit" : "credit";
      return `<tr>
        <td><span class="badge ${css}">${type}</span></td>
        <td>${money(item.amount ?? item.Amount)}</td>
        <td>${fmtDate(item.date ?? item.Date)}</td>
        <td>${item.description ?? item.Description ?? ""}</td>
      </tr>`;
    })
    .join("");
}

function renderBalances(items) {
  if (!items?.length) {
    els.balancesBody.innerHTML = `<tr><td colspan="4">Aguardando projeção do consolidado...</td></tr>`;
    return;
  }

  els.balancesBody.innerHTML = items
    .map((item) => `<tr>
      <td>${fmtDate(item.date ?? item.Date)}</td>
      <td>${money(item.totalCredits ?? item.TotalCredits)}</td>
      <td>${money(item.totalDebits ?? item.TotalDebits)}</td>
      <td><strong>${money(item.balance ?? item.Balance)}</strong></td>
    </tr>`)
    .join("");
}

function renderOutbox(items) {
  if (!items?.length) {
    els.outboxBody.innerHTML = `<tr><td colspan="3">Outbox vazio.</td></tr>`;
    return;
  }

  els.outboxBody.innerHTML = items
    .map((item) => {
      const status = item.status ?? item.Status ?? (item.processedAt ? "Published" : "Pending");
      const css = String(status).toLowerCase() === "pending" ? "pending" : "published";
      return `<tr>
        <td><span class="badge ${css}">${status}</span></td>
        <td>${fmtDateTime(item.createdAt ?? item.CreatedAt)}</td>
        <td><div class="payload" title="${(item.payload ?? item.Payload ?? "").replaceAll('"', "&quot;")}">${item.payload ?? item.Payload ?? ""}</div></td>
      </tr>`;
    })
    .join("");
}

function renderEvents(items) {
  if (!items?.length) {
    els.eventsBody.innerHTML = `<tr><td colspan="2">Nenhum evento processado ainda.</td></tr>`;
    return;
  }

  els.eventsBody.innerHTML = items
    .map((item) => `<tr>
      <td><code>${item.eventId ?? item.EventId}</code></td>
      <td>${fmtDateTime(item.processedAt ?? item.ProcessedAt)}</td>
    </tr>`)
    .join("");
}

async function refresh() {
  let lancamentosHealth = null;
  let consolidadoHealth = null;

  try { lancamentosHealth = await getJson(`${LANCAMENTOS}/health`); } catch { /* offline */ }
  try { consolidadoHealth = await getJson(`${CONSOLIDADO}/health`); } catch { /* offline */ }
  renderHealth(lancamentosHealth, consolidadoHealth);

  try {
    const entries = await getJson(`${LANCAMENTOS}/entries`);
    renderEntries(entries);
  } catch {
    els.entriesBody.innerHTML = `<tr><td colspan="4">Falha ao carregar lançamentos.</td></tr>`;
  }

  try {
    const balances = await getJson(`${CONSOLIDADO}/balances`);
    renderBalances(balances);
  } catch {
    els.balancesBody.innerHTML = `<tr><td colspan="4">Falha ao carregar saldos.</td></tr>`;
  }

  try {
    const outbox = await getJson(`${LANCAMENTOS}/admin/outbox`);
    renderOutbox(outbox);
  } catch {
    els.outboxBody.innerHTML = `<tr><td colspan="3">Falha ao carregar outbox.</td></tr>`;
  }

  try {
    const events = await getJson(`${CONSOLIDADO}/admin/processed-events`);
    renderEvents(events);
  } catch {
    els.eventsBody.innerHTML = `<tr><td colspan="2">Falha ao carregar eventos.</td></tr>`;
  }
}

els.entryForm.addEventListener("submit", async (event) => {
  event.preventDefault();
  const data = new FormData(els.entryForm);
  const payload = {
    type: data.get("type"),
    amount: Number(data.get("amount")),
    date: data.get("date"),
    description: data.get("description"),
  };

  try {
    const response = await fetch(`${LANCAMENTOS}/entries`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload),
    });

    if (!response.ok) {
      const err = await response.json().catch(() => ({}));
      throw new Error(err.error || `HTTP ${response.status}`);
    }

    setMsg("Lançamento registrado. Outbox → Kafka → consolidado em alguns segundos.", true);
    await refresh();
    setTimeout(refresh, 4000);
  } catch (error) {
    setMsg(error.message || "Falha ao registrar.", false);
  }
});

els.btnRefresh.addEventListener("click", refresh);
refresh();
setInterval(refresh, 5000);
