async function fetchJSON(url, options) {
  const res = await fetch(url, options);
  if (!res.ok) throw new Error(await res.text());
  return res.json();
}

function levelClass(level) {
  if (level === 'Alta') return 'chip-high';
  if (level === 'Média') return 'chip-medium';
  return 'chip-low';
}

// Dashboard rendering
async function loadDashboard() {
  try {
    const data = await fetchJSON('/api/dashboard/summary');
    const f = data.forecast;
    document.getElementById('chip-morning').innerHTML = `${f.morning.level} (Previsto: ${f.morning.score})`;
    document.getElementById('chip-morning').classList.add(levelClass(f.morning.level));
    document.getElementById('chip-afternoon').innerHTML = `${f.afternoon.level} (Previsto: ${f.afternoon.score})`;
    document.getElementById('chip-afternoon').classList.add(levelClass(f.afternoon.level));
    document.getElementById('chip-night').innerHTML = `${f.night.level} (Previsto: ${f.night.score})`;
    document.getElementById('chip-night').classList.add(levelClass(f.night.level));

    document.getElementById('rec-sleep').innerText = data.recommendation.sleepAt;
    document.getElementById('rec-wake').innerText = data.recommendation.wakeAt;

    await loadTable(1);
  } catch (err) {
    console.error(err);
  }
}

async function loadTable(page) {
  const pageSize = parseInt(document.getElementById('rowsPerPage').value, 10);
  const { items, total } = await fetchJSON(`/api/records?page=${page}&pageSize=${pageSize}`);

  const tbody = document.getElementById('historyBody');
  tbody.innerHTML = '';
  for (const r of items) {
    const tr = document.createElement('tr');
    const date = new Date(r.date).toLocaleDateString();
    tr.innerHTML = `
      <td>${date}</td>
      <td>${r.sleepTime}</td>
      <td>${r.wakeTime}</td>
      <td>${r.productivityMorning}</td>
      <td>${r.productivityAfternoon}</td>
      <td>${r.productivityNight}</td>
    `;
    tbody.appendChild(tr);
  }
  document.getElementById('totalCount').innerText = total;
}

// Register page
async function submitRecord(e) {
  e.preventDefault();
  const dto = {
    sleepTime: document.getElementById('sleepTime').value,
    wakeTime: document.getElementById('wakeTime').value,
    productivityMorning: parseInt(document.getElementById('prodMorning').value, 10),
    productivityAfternoon: parseInt(document.getElementById('prodAfternoon').value, 10),
    productivityNight: parseInt(document.getElementById('prodNight').value, 10),
    date: null // backend assumirá ontem
  };
  await fetchJSON('/api/records', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(dto)
  });
  const okAlert = document.getElementById('saveOk');
  okAlert.classList.remove('d-none');
  setTimeout(() => okAlert.classList.add('d-none'), 3000);
}

function initNavActive() {
  const path = location.pathname;
  if (path.includes('register')) {
    document.getElementById('nav-register').classList.add('active');
  } else {
    document.getElementById('nav-dashboard').classList.add('active');
  }
}

document.addEventListener('DOMContentLoaded', initNavActive);