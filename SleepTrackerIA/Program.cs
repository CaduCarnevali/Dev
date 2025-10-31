using Microsoft.EntityFrameworkCore;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

var builder = WebApplication.CreateBuilder(args);

// 1. Adicionar o DbContext com SQLite (para o histórico)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApiDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 2. Política de CORS
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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

// --- ENDPOINTS DE HISTÓRICO (Sem mudança) ---
app.MapGet("/api/sleep", async (ApiDbContext db) =>
{
    return await db.SleepRecords.OrderByDescending(s => s.StartTime).ToListAsync();
});
app.MapPost("/api/sleep", async (ApiDbContext db, SleepRecord record) =>
{
    // Crie um *novo* record com os valores de tempo localizados
    // usando a expressão 'with'. Isso corrige o erro de propriedade "init-only".
    var localizedRecord = record with
    {
        StartTime = record.StartTime.ToLocalTime(),
        EndTime = record.EndTime.ToLocalTime()
    };

    // Adicione o *novo* record ao banco de dados, não o original
    await db.SleepRecords.AddAsync(localizedRecord);
    await db.SaveChangesAsync();

    // Retorne o record que foi salvo
    return Results.Created($"/api/sleep/{localizedRecord.Id}", localizedRecord);
});
app.MapDelete("/api/sleep/{id}", async (ApiDbContext db, int id) =>
{
    var record = await db.SleepRecords.FindAsync(id);
    if (record == null) return Results.NotFound();
    db.SleepRecords.Remove(record);
    await db.SaveChangesAsync();
    return Results.NoContent();
});
// --- FIM DOS ENDPOINTS DE HISTÓRICO ---


// --- ENDPOINTS DA IA (v3 - KAGGLE) ---

// O NOME DO MODELO FOI ATUALIZADO
const string OnnxModelName = "model_v3_kaggle.onnx";

// Helper para criar o tensor de 8 features
static List<NamedOnnxValue> CreateOnnxInput(SimulationInputV3 input)
{
    // Ordem: 'Sleep Duration', 'Quality of Sleep', 'Physical Activity Level', 
    // 'Heart Rate', 'Daily Steps', 'Gender_Num', 'Age', 'Disorder_Num'
    var inputData = new float[]
    {
        input.SleepDuration,
        input.QualityOfSleep,
        input.PhysicalActivityLevel,
        input.HeartRate,
        input.DailySteps,
        input.Gender_Num,
        input.Age,
        input.Disorder_Num
    };

    var tensor = new DenseTensor<float>(inputData, new int[] { 1, 8 }); // 1 linha, 8 features
    return new List<NamedOnnxValue>
    {
        NamedOnnxValue.CreateFromTensor("float_input", tensor)
    };
}

// ENDPOINT DO SIMULADOR (v3)
app.MapPost("/api/simulate", (SimulationInputV3 input) =>
{
    var modelPath = Path.Combine(AppContext.BaseDirectory, OnnxModelName);
    using var session = new InferenceSession(modelPath);

    var inputs = CreateOnnxInput(input);
    using var results = session.Run(inputs);
    var prediction = results.FirstOrDefault()?.AsTensor<float>()?.ToArray();

    if (prediction != null && prediction.Length > 0)
    {
        var stressLevel = Math.Round(prediction[0], 1);
        return Results.Ok(new { StressLevel = stressLevel });
    }
    return Results.Problem("Não foi possível processar a simulação.");
});

// ENDPOINT DE RECOMENDAÇÃO (v3)
app.MapGet("/api/recommendation", () =>
{
    var modelPath = Path.Combine(AppContext.BaseDirectory, OnnxModelName);
    using var session = new InferenceSession(modelPath);

    float bestSleepDuration = 0;
    float bestActivity = 0;
    float bestQuality = 0;
    float minStress = 10.0f; // Queremos minimizar o estresse

    // Inputs padrão (ex: Homem, 35 anos, sem distúrbio)
    // Gender=1 (Male), Age=35, Disorder=0 (None), HeartRate=70, DailySteps=8000
    var baseInput = new SimulationInputV3(0, 0, 0, 70, 8000, 1, 35, 0);

    // Loop 1: Testa Duração do Sono (6h a 9h)
    for (float duration = 6.0f; duration <= 9.0f; duration += 0.5f)
    {
        // Loop 2: Testa Qualidade do Sono (6 a 10)
        for (float quality = 6.0f; quality <= 10.0f; quality += 1.0f)
        {
            // Loop 3: Testa Atividade Física (30min a 90min)
            for (float activity = 30.0f; activity <= 90.0f; activity += 30.0f)
            {
                var currentInput = baseInput with
                {
                    SleepDuration = duration,
                    QualityOfSleep = quality,
                    PhysicalActivityLevel = activity
                };

                var inputs = CreateOnnxInput(currentInput);
                using var results = session.Run(inputs);
                var prediction = results.FirstOrDefault()?.AsTensor<float>()?.ToArray();

                if (prediction != null && prediction.Length > 0)
                {
                    float currentStress = prediction[0];
                    if (currentStress < minStress)
                    {
                        minStress = currentStress;
                        bestSleepDuration = duration;
                        bestQuality = quality;
                        bestActivity = activity;
                    }
                }
            }
        }
    }

    return Results.Ok(new
    {
        // Retorna a melhor combinação encontrada
        SleepDuration = bestSleepDuration,
        QualityOfSleep = bestQuality,
        PhysicalActivityLevel = bestActivity,
        PredictedStress = Math.Round(minStress, 1)
    });
});


// --- FIM DOS ENDPOINTS DA IA ---

app.UseDefaultFiles();
app.UseStaticFiles();

app.Run();

// --- DEFINIÇÕES DE MODELO ---
// Este é o record do formulário "Registrar Sono"
public record SleepRecord(
    int Id,
    DateTime StartTime,
    DateTime EndTime,
    double DurationInHours,
    string? Notes,
    int ProductivityMorning,
    int ProductivityAfternoon,
    int ProductivityNight
);

// Este é o record do SIMULADOR (v3)
public record SimulationInputV3(
    float SleepDuration,
    float QualityOfSleep,
    float PhysicalActivityLevel,
    float HeartRate,
    float DailySteps,
    float Gender_Num, // 0=Female, 1=Male
    float Age,
    float Disorder_Num // 0=None, 1=Insomnia, 2=Sleep Apnea
);

// Contexto do Banco de Dados
public class ApiDbContext : DbContext
{
    public DbSet<SleepRecord> SleepRecords { get; set; }
    public ApiDbContext(DbContextOptions<ApiDbContext> options) : base(options) { }
}