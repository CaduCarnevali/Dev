using Microsoft.EntityFrameworkCore;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SleepTrackerIA.Data;
using SleepTrackerIA.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Adicionar o DbContext com SQLite
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

// Adicionar serviços de API (Swagger/OpenAPI)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddControllers(); // Habilita os serviços de Controller

// 2. Adicionar política de CORS (MUITO IMPORTANTE)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configurar o pipeline de HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 3. Usar a política de CORS
app.UseCors("AllowAll");

// --- INÍCIO DOS ENDPOINTS DA API ---

// GET /api/sleep
app.MapGet("/api/sleep", async (AppDbContext db) =>
{
    return await db.SleepRecords.OrderByDescending(s => s.StartTime).ToListAsync();
});

// POST /api/sleep
//app.MapPost("/api/sleep", async (AppDbContext db, SleepRecord record) =>
//{
//    // Opcional: Recalcular a data no servidor para segurança
//    record.StartTime = record.StartTime.ToLocalTime();
//    record.EndTime = record.EndTime.ToLocalTime();

//    await db.SleepRecords.AddAsync(record);
//    await db.SaveChangesAsync();
//    return Results.Created($"/api/sleep/{record.Id}", record);
//});

// DELETE /api/sleep/{id}
app.MapDelete("/api/sleep/{id}", async (AppDbContext db, int id) =>
{
    var record = await db.SleepRecords.FindAsync(id);
    if (record == null)
    {
        return Results.NotFound();
    }

    db.SleepRecords.Remove(record);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// --- FIM DOS ENDPOINTS DA API ---
app.UseDefaultFiles(); // Procura por index.html
app.UseStaticFiles();  // Serve arquivos da pasta wwwroot

app.MapControllers(); // Mapeia as rotas do RecordsController e DashboardController

// --- ENDPOINT DO SIMULADOR DA IA (NOVO) ---
app.MapPost("/api/simulate", (SimulationInput input) =>
{
    var modelPath = Path.Combine(AppContext.BaseDirectory, "model.onnx");
    using var session = new InferenceSession(modelPath);

    // 1. Preparar as Features

    // Calcular duração (lidando com "virada" do dia, ex: 23:00 -> 07:00)
    float duration;
    if (input.EndHour < input.StartHour)
    {
        duration = (24.0f - input.StartHour) + input.EndHour;
    }
    else
    {
        duration = input.EndHour - input.StartHour;
    }

    var startHour = input.StartHour;
    var endHour = input.EndHour;
    var dayOfWeek = (float)input.DayOfWeek; // 0=Seg, 1=Ter, ...

    // Ordem: 'Duration', 'StartHour', 'EndHour', 'DayOfWeek'
    var inputData = new float[] { duration, startHour, endHour, dayOfWeek };
    var tensor = new DenseTensor<float>(inputData, new int[] { 1, 4 });
    var inputs = new List<NamedOnnxValue>
    {
        NamedOnnxValue.CreateFromTensor("float_input", tensor)
    };

    // 2. Executar o modelo
    using var results = session.Run(inputs);
    var prediction = results.FirstOrDefault()?.AsTensor<float>()?.ToArray();

    if (prediction != null && prediction.Length == 3)
    {
        var resultObject = new
        {
            ProductivityMorning = Math.Round(prediction[0], 1),
            ProductivityAfternoon = Math.Round(prediction[1], 1),
            ProductivityNight = Math.Round(prediction[2], 1),
            TotalScore = Math.Round(prediction[0] + prediction[1], 1) // Um "score" total
        };
        return Results.Ok(resultObject);
    }
    return Results.Problem("Não foi possível processar a simulação.");
});
// --- FIM DO ENDPOINT DO SIMULADOR ---
// --- ENDPOINT DE PREVISÃO DA IA (MODIFICADO) ---
app.MapPost("/api/predict", (SleepRecord input) =>
{
    var modelPath = Path.Combine(AppContext.BaseDirectory, "model.onnx");
    using var session = new InferenceSession(modelPath);

    // 2. Preparar as Features (AGORA SÃO 4)
    var duration = (float)(input.EndTime - input.StartTime).TotalHours;
    var startHour = (float)input.StartTime.Hour + (input.StartTime.Minute / 60.0f);
    var endHour = (float)input.EndTime.Hour + (input.EndTime.Minute / 60.0f);

    // (NOVO) Converter C# DayOfWeek (Sun=0...Sat=6) para Python weekday() (Mon=0...Sun=6)
    int csharpDay = (int)input.StartTime.DayOfWeek;
    int pythonDay = (csharpDay + 6) % 7; // Converte (Dom=0 -> 6), (Seg=1 -> 0), etc.
    var dayOfWeek = (float)pythonDay;

    // O modelo espera [Duration, StartHour, EndHour, DayOfWeek]
    var inputData = new float[] { duration, startHour, endHour, dayOfWeek };

    // MUDAR DE [1, 3] para [1, 4]
    var tensor = new DenseTensor<float>(inputData, new int[] { 1, 4 });

    var inputs = new List<NamedOnnxValue>
    {
        NamedOnnxValue.CreateFromTensor("float_input", tensor)
    };

    // 4. Executar o modelo
    using var results = session.Run(inputs);
    var prediction = results.FirstOrDefault()?.AsTensor<float>()?.ToArray();

    if (prediction != null && prediction.Length == 3)
    {
        var resultObject = new
        {
            ProductivityMorning = Math.Round(prediction[0], 1),
            ProductivityAfternoon = Math.Round(prediction[1], 1),
            ProductivityNight = Math.Round(prediction[2], 1)
        };
        return Results.Ok(resultObject);
    }
    return Results.Problem("Não foi possível processar a previsão.");
});
// --- FIM DO ENDPOINT DE PREVISÃO ---


// --- ENDPOINT DE RECOMENDAÇÃO DA IA (MODIFICADO) ---
//app.MapGet("/api/recommendation", () =>
//{
//    var modelPath = Path.Combine(AppContext.BaseDirectory, "model.onnx");
//    using var session = new InferenceSession(modelPath);

//    float bestStartHour = 0;
//    float bestDuration = 0;
//    float maxProductivity = 0;

//    // Loop 1: Testa horários de dormir (das 21:00 à 01:00 da manhã)
//    for (float startHour = 21.0f; startHour <= 25.0f; startHour += 0.25f) // 25.0f = 1:00 AM
//    {
//        // Loop 2: Testa durações de sono (de 7h a 9h)
//        for (float duration = 7.0f; duration <= 9.0f; duration += 0.25f)
//        {
//            // (NOVO) Loop 3: Testa para todos os dias da semana (0=Seg até 6=Dom)
//            for (float day = 0; day <= 6; day++)
//            {
//                float realStartHour = startHour % 24;
//                float endHour = (startHour + duration) % 24;

//                // MUDADO DE 3 PARA 4 FEATURES
//                var inputData = new float[] { duration, realStartHour, endHour, day };
//                // MUDADO DE [1, 3] PARA [1, 4]
//                var tensor = new DenseTensor<float>(inputData, new int[] { 1, 4 });

//                var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("float_input", tensor) };

//                using var results = session.Run(inputs);
//                var prediction = results.FirstOrDefault()?.AsTensor<float>()?.ToArray();

//                if (prediction != null && prediction.Length == 3)
//                {
//                    float totalProductivity = prediction[0] + prediction[1]; // Otimiza para Manhã+Tarde

//                    if (totalProductivity > maxProductivity)
//                    {
//                        maxProductivity = totalProductivity;
//                        bestStartHour = realStartHour;
//                        bestDuration = duration;
//                    }
//                }
//            }
//        }
//    }

//    // Converter os floats de volta para horários
//    var recommendedStart = TimeSpan.FromHours(bestStartHour);
//    var recommendedEnd = TimeSpan.FromHours((bestStartHour + bestDuration) % 24);

//    return Results.Ok(new
//    {
//        StartTime = $"{recommendedStart.Hours:D2}:{recommendedStart.Minutes:D2}",
//        EndTime = $"{recommendedEnd.Hours:D2}:{recommendedEnd.Minutes:D2}"
//    });
//});

app.MapGet("/api/recommendation", () =>
{
    var modelPath = Path.Combine(AppContext.BaseDirectory, "model.onnx");
    using var session = new InferenceSession(modelPath);

    float bestStartHour = 0;
    float bestDuration = 0;
    float maxProductivity = 0;

    // --- NOVO: Pega o dia de hoje ---
    // DayOfWeek do C# começa com Domingo = 0, Segunda = 1, ..., Sábado = 6
    DayOfWeek csharpDay = DateTime.Now.DayOfWeek;

    // Converte para o formato do seu modelo (Segunda=0, ..., Domingo=6)
    // Se hoje for Domingo (0), vira 6. Senão (ex: Segunda=1), vira 1-1=0.
    float day = (csharpDay == DayOfWeek.Sunday) ? 6 : (int)csharpDay - 1;
    // -------------------------------

    // Loop 1: Testa horários de dormir (das 21:00 à 01:00 da manhã)
    for (float startHour = 21.0f; startHour <= 25.0f; startHour += 0.25f) // 25.0f = 1:00 AM
    {
        // Loop 2: Testa durações de sono (de 7h a 9h)
        for (float duration = 7.0f; duration <= 9.0f; duration += 0.25f)
        {
            // (REMOVIDO) Loop 3: Não precisamos testar todos os dias
            // for (float day = 0; day <= 6; day++)
            // {
            float realStartHour = startHour % 24;
            float endHour = (startHour + duration) % 24;

            // MUDADO DE 3 PARA 4 FEATURES - Agora usando o 'day' de hoje
            var inputData = new float[] { duration, realStartHour, endHour, day };
            var tensor = new DenseTensor<float>(inputData, new int[] { 1, 4 });

            var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("float_input", tensor) };

            using var results = session.Run(inputs);
            var prediction = results.FirstOrDefault()?.AsTensor<float>()?.ToArray();

            if (prediction != null && prediction.Length == 3)
            {
                float totalProductivity = prediction[0] + prediction[1]; // Otimiza para Manhã+Tarde

                if (totalProductivity > maxProductivity)
                {
                    maxProductivity = totalProductivity;
                    bestStartHour = realStartHour;
                    bestDuration = duration;
                }
            }
            // } // Fim do loop 3 removido
        }
    }

    // Converter os floats de volta para horários
    var recommendedStart = TimeSpan.FromHours(bestStartHour);
    var recommendedEnd = TimeSpan.FromHours((bestStartHour + bestDuration) % 24);

    return Results.Ok(new
    {
        // Renomeei para ser mais claro no frontend
        sleepAt = $"{recommendedStart.Hours:D2}:{recommendedStart.Minutes:D2}",
        wakeAt = $"{recommendedEnd.Hours:D2}:{recommendedEnd.Minutes:D2}"
    });
});

app.Run();

// (NOVO) Classe simples para receber os dados da simulação
public record SimulationInput(float StartHour, float EndHour, int DayOfWeek);