
// URLs da API
const API_BASE_URL = 'https://localhost:7155';
const API_URL = `${API_BASE_URL}/api/records`;      
const PREDICT_URL = `${API_BASE_URL}/api/predict`; 
const RECOMMEND_URL = `${API_BASE_URL}/api/recommendation`;
const SIMULATE_URL = `${API_BASE_URL}/api/simulate`;

// Elementos do DOM (Registro)
const form = document.getElementById('sleep-form');
const tableBody = document.getElementById('history-body');
const startTimeInput = document.getElementById('start-time');
const endTimeInput = document.getElementById('end-time');
const notesInput = document.getElementById('notes');

// Navegação
const registerLink = document.getElementById('register-link');
const dashboardLink = document.getElementById('dashboard-link');
const simulatorLink = document.getElementById('simulator-link'); 
const registerSection = document.getElementById('register-sleep-section');
const dashboardSection = document.getElementById('dashboard-section');
const simulatorSection = document.getElementById('simulator-section'); 
const menuToggle = document.getElementById('menu-toggle');
const sidebar = document.getElementById('sidebar');

// Sliders (Registro)
const prodManhaInput = document.getElementById('prod-manha');
const prodTardeInput = document.getElementById('prod-tarde');
const prodNoiteInput = document.getElementById('prod-noite');
const prodManhaValue = prodManhaInput.nextElementSibling;
const prodTardeValue = prodTardeInput.nextElementSibling;
const prodNoiteValue = prodNoiteInput.nextElementSibling;

// Elementos do Simulador (NOVOS)
const simulatorForm = document.getElementById('simulator-form');
const simStartTime = document.getElementById('sim-start-time');
const simEndTime = document.getElementById('sim-end-time');
const simDayOfWeek = document.getElementById('sim-day-of-week');
const simulatorResults = document.getElementById('simulator-results');
const simResTotal = document.getElementById('sim-res-total');
const simResMorning = document.getElementById('sim-res-morning');
const simResAfternoon = document.getElementById('sim-res-afternoon');
const simResNight = document.getElementById('sim-res-night');

// UI Geral
const notification = document.getElementById('notification');
let myChart = null;

// --- Funções de UI (Navegação, Sliders) ---
function showSection(sectionToShow) {
    // Esconde todas
    registerSection.classList.remove('active-section');
    registerSection.classList.add('hidden-section');
    dashboardSection.classList.remove('active-section');
    dashboardSection.classList.add('hidden-section');
    simulatorSection.classList.remove('active-section');
    simulatorSection.classList.add('hidden-section');

    // Mostra a escolhida
    sectionToShow.classList.remove('hidden-section');
    sectionToShow.classList.add('active-section');

    // Atualiza links da sidebar
    registerLink.classList.remove('active');
    dashboardLink.classList.remove('active');
    simulatorLink.classList.remove('active');

    if (sectionToShow === registerSection) {
        registerLink.classList.add('active');
    } else if (sectionToShow === dashboardSection) {
        dashboardLink.classList.add('active');
    } else if (sectionToShow === simulatorSection) {
        simulatorLink.classList.add('active');
    }
}
registerLink.addEventListener('click', (e) => {
    e.preventDefault();
    showSection(registerSection);
    sidebar.classList.remove('open');
});
dashboardLink.addEventListener('click', (e) => {
    e.preventDefault();
    showSection(dashboardSection);
    loadHistory();
    loadRecommendation();
    sidebar.classList.remove('open');
});
simulatorLink.addEventListener('click', (e) => { 
    e.preventDefault();
    showSection(simulatorSection);
    sidebar.classList.remove('open');
});
menuToggle.addEventListener('click', () => {
    sidebar.classList.toggle('open');
});
function updateSliderValue(inputElement, valueElement) {
    valueElement.textContent = inputElement.value;
}
prodManhaInput.addEventListener('input', () => updateSliderValue(prodManhaInput, prodManhaValue));
prodTardeInput.addEventListener('input', () => updateSliderValue(prodTardeInput, prodTardeValue));
prodNoiteInput.addEventListener('input', () => updateSliderValue(prodNoiteInput, prodNoiteValue));
updateSliderValue(prodManhaInput, prodManhaValue);
updateSliderValue(prodTardeInput, prodTardeValue);
updateSliderValue(prodNoiteInput, prodNoiteValue);


// --- Funções de Notificação ---
function showNotification(message, type = 'success') {
    notification.textContent = message;
    notification.classList.remove('success', 'error');
    notification.classList.add(type);
    notification.classList.add('show');
    setTimeout(() => {
        hideNotification();
    }, 3000);
}
function hideNotification() {
    notification.classList.remove('show');
}


// --- Funções da API (Registro) ---
form.addEventListener('submit', async (e) => {
    e.preventDefault();

    const submitButton = form.querySelector('button[type="submit"]');
    const originalButtonText = submitButton.textContent;
    submitButton.disabled = true;
    submitButton.textContent = 'Salvando...';
    hideNotification();

    const newRecord = {
        startTime: startTimeInput.value,
        endTime: endTimeInput.value,
        notes: notesInput.value,
        productivityMorning: parseInt(prodManhaInput.value),
        productivityAfternoon: parseInt(prodTardeInput.value),
        productivityNight: parseInt(prodNoiteInput.value),
    };

    try {
        const response = await fetch(API_URL, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(newRecord),
        });

        if (response.ok) {
            showNotification('Registro salvo com sucesso!', 'success');
            form.reset();
            prodManhaInput.value = 3; updateSliderValue(prodManhaInput, prodManhaValue);
            prodTardeInput.value = 3; updateSliderValue(prodTardeInput, prodTardeValue);
            prodNoiteInput.value = 3; updateSliderValue(prodNoiteInput, prodNoiteValue);

            await handlePrediction(newRecord);

            setTimeout(() => {
                showSection(dashboardSection);
                loadHistory();
            }, 2000);
        } else {
            const errorText = await response.text();
            showNotification(`Erro ao salvar registro: ${errorText}`, 'error');
        }
    } catch (error) {
        console.error('Erro na rede (Salvar):', error);
        showNotification('Erro de conexão com o servidor.', 'error');
    } finally {
        submitButton.disabled = false;
        submitButton.textContent = originalButtonText;
    }
});


// --- Funções da IA (Predição, Recomendação, Simulação) ---
async function handlePrediction(record) {
    try {
        const response = await fetch(PREDICT_URL, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(record),
        });
        if (response.ok) {
            const predictionData = await response.json();
            updatePredictionUI(predictionData);
        } else {
            console.error('Erro ao buscar predição da IA.');
        }
    } catch (error) {
        console.error('Erro na rede (Predição):', error);
    }
}
function updatePredictionUI(prediction) {
    formatPredictionTag('pred-manha', prediction.productivityMorning);
    formatPredictionTag('pred-tarde', prediction.productivityAfternoon);
    formatPredictionTag('pred-noite', prediction.productivityNight);
}
function formatPredictionTag(elementId, value) {
    const element = document.getElementById(elementId);
    if (!element) return;
    let label = 'Baixa';
    let cssClass = 'low';
    if (value >= 4.0) {
        label = 'Alta';
        cssClass = 'high';
    } else if (value >= 2.5) {
        label = 'Média';
        cssClass = 'medium';
    }
    element.classList.remove('high', 'medium', 'low');
    element.classList.add(cssClass);
    element.textContent = `${label} (Previsto: ${value})`;
}
async function loadRecommendation() {
    try {
        const response = await fetch(RECOMMEND_URL);
        if (response.ok) {
            const data = await response.json();
            document.getElementById('rec-start').textContent = data.sleepAt || '-';
            document.getElementById('rec-end').textContent = data.wakeAt || '-';
        } else {
            console.error('Erro ao buscar recomendação.');
        }
    } catch (error) {
        console.error('Erro na rede (Recomendação):', error);
    }
}

// NOVO: Event Listener do Formulário do Simulador
simulatorForm.addEventListener('submit', async (e) => {
    e.preventDefault();
    hideNotification();

    const submitButton = simulatorForm.querySelector('button[type="submit"]');
    const originalButtonText = submitButton.textContent;
    submitButton.disabled = true;
    submitButton.textContent = 'Simulando...';

    // 1. Converter "HH:MM" para float (ex: "22:30" -> 22.5)
    const timeToFloat = (timeString) => {
        const [hours, minutes] = timeString.split(':').map(parseFloat);
        return hours + (minutes / 60);
    };

    const payload = {
        startHour: timeToFloat(simStartTime.value),
        endHour: timeToFloat(simEndTime.value),
        dayOfWeek: parseInt(simDayOfWeek.value) // 0=Seg, 1=Ter, ...
    };

    try {
        const response = await fetch(SIMULATE_URL, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload),
        });

        if (response.ok) {
            const results = await response.json();
            // ex: { productivityMorning: 4.5, totalScore: 8.5, ... }
            simResTotal.textContent = results.totalScore;
            simResMorning.textContent = results.productivityMorning;
            simResAfternoon.textContent = results.productivityAfternoon;
            simResNight.textContent = results.productivityNight;

            simulatorResults.style.display = 'block'; // Mostra o card de resultado
        } else {
            const errorText = await response.text();
            showNotification(`Erro na simulação: ${errorText}`, 'error');
            simulatorResults.style.display = 'none'; // Esconde resultados
        }
    } catch (error) {
        console.error('Erro na rede (Simulador):', error);
        showNotification('Erro de conexão com o servidor.', 'error');
        simulatorResults.style.display = 'none'; // Esconde resultados
    } finally {
        submitButton.disabled = false;
        submitButton.textContent = originalButtonText;
    }
});


// --- Função de Carregar Histórico (com Gráfico) ---
async function loadHistory() {
    try {
        const response = await fetch(API_URL);
        const data = await response.json();
        const records = data.items;

        tableBody.innerHTML = '';

        if (records.length === 0) {
            tableBody.innerHTML = `<tr><td colspan="9" style="text-align: center; padding: 20px;">Nenhum registro encontrado.</td></tr>`;
            if (myChart) {
                myChart.destroy();
                myChart = null;
            }
            return;
        }

        records.forEach(record => {
            const row = document.createElement('tr');
            const duration = parseFloat(record.durationInHours).toFixed(2);
            row.innerHTML = `
                <td>${new Date(record.startTime).toLocaleDateString()}</td>
                <td>${new Date(record.startTime).toLocaleTimeString()}</td>
                <td>${new Date(record.endTime).toLocaleTimeString()}</td>
                <td>${duration}h</td>
                <td>${record.productivityMorning || '-'}</td>
                <td>${record.productivityAfternoon || '-'}</td>
                <td>${record.productivityNight || '-'}</td>
                <td>${record.notes || ''}</td>
                <td><button onclick="deleteRecord(${record.id})">Excluir</button></td>
            `;
            tableBody.appendChild(row);
        });

        // --- LÓGICA DO GRÁFICO ---
        if (myChart) {
            myChart.destroy();
        }
        const chartRecords = [...records].reverse();
        const labels = chartRecords.map(r => new Date(r.startTime).toLocaleDateString());
        const dataMorning = chartRecords.map(r => r.productivityMorning);
        const dataAfternoon = chartRecords.map(r => r.productivityAfternoon);
        const dataNight = chartRecords.map(r => r.productivityNight);

        const ctx = document.getElementById('productivityChart').getContext('2d');
        myChart = new Chart(ctx, {
            type: 'line',
            data: {
                labels: labels,
                datasets: [
                    { label: 'Prod. Manhã', data: dataMorning, borderColor: '#6a0dad', backgroundColor: 'rgba(106, 13, 173, 0.1)', tension: 0.1, fill: true },
                    { label: 'Prod. Tarde', data: dataAfternoon, borderColor: '#FFC107', backgroundColor: 'rgba(255, 193, 7, 0.1)', tension: 0.1, fill: true },
                    { label: 'Prod. Noite', data: dataNight, borderColor: '#6c757d', backgroundColor: 'rgba(108, 117, 125, 0.1)', tension: 0.1, fill: true }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: true,
                scales: {
                    y: { beginAtZero: true, max: 5, title: { display: true, text: 'Nível de Produtividade (1-5)' } },
                    x: { title: { display: true, text: 'Data do Registro' } }
                }
            }
        });

    } catch (error) {
        console.error('Erro ao carregar histórico:', error);
        tableBody.innerHTML = `<tr><td colspan="9" style="text-align: center; color: red; padding: 20px;">Erro ao carregar dados. Verifique a API.</td></tr>`;
        if (myChart) {
            myChart.destroy();
            myChart = null;
        }
    }
}


// --- Função de Deletar Registro ---
async function deleteRecord(id) {
    hideNotification();
    if (!confirm('Tem certeza que deseja excluir este registro?')) {
        return;
    }
    try {
        const response = await fetch(`${API_URL}/${id}`, {
            method: 'DELETE',
        });
        if (response.ok) {
            showNotification('Registro excluído com sucesso!', 'success');
            loadHistory();
        } else {
            showNotification('Erro ao excluir registro.', 'error');
        }
    } catch (error) {
        console.error('Erro na rede:', error);
        showNotification('Erro de conexão ao excluir.', 'error');
    }
}

// --- Inicialização da página ---
document.addEventListener('DOMContentLoaded', () => {
    // Definir a data/hora atual para os inputs datetime-local
    const now = new Date();
    now.setMinutes(now.getMinutes() - now.getTimezoneOffset());
    const nowString = now.toISOString().slice(0, 16);
    endTimeInput.value = nowString;

    // Define o dia da semana atual no simulador
    const csharpDay = new Date().getDay(); // Dom=0, Seg=1, ...
    const pythonDay = (csharpDay + 6) % 7; // Seg=0, Ter=1, ...
    document.getElementById('sim-day-of-week').value = pythonDay;

    showSection(dashboardSection);
    showSection(registerSection);
    loadRecommendation();
});

