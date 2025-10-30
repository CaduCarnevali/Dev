# 🧠 Sistema de Análise de Produtividade Diária

Um projeto desenvolvido para **analisar o horário de maior produtividade** de um usuário com base em seus hábitos de sono e percepção de desempenho ao longo do dia.

---

## 📋 Funcionalidade Principal

O sistema calcula o **pico de concentração estimado** para o dia, considerando os dados informados pelo usuário:

- ⏰ **Hora que dormiu**
- ⏰ **Hora que acordou**
- 🌅 **Produtividade percebida pela manhã (1 a 10)**
- 🌇 **Produtividade percebida à tarde (1 a 10)**
- 🌃 **Produtividade percebida à noite (1 a 10)**

Com base nesses dados, o sistema identifica **qual período do dia** o usuário tende a apresentar **melhor desempenho cognitivo**.

---

## 🧩 Estrutura do Projeto

| Camada | Tecnologias |
|:-------|:-------------|
| **Front-end** | HTML, CSS, JavaScript, Bootstrap |
| **Back-end** | C# |
| **Banco de Dados** | SQLite *(ou outro, ainda a definir)* |
| **Arquitetura** | MVC (Model-View-Controller) |

---

## ⚙️ Infraestrutura e Deploy

| Recurso | Descrição |
|:--------|:-----------|
| **Controle de versão** | Git + GitHub |
| **Hospedagem** | Render *(a definir configuração final)* |
| **Banco de dados persistente** | SQLite (com script de inserção inicial, caso o banco seja recriado) |

---

## 🧮 Funcionamento do Sistema

1. O usuário preenche o formulário com os horários e níveis de produtividade.  
2. O sistema armazena os dados no banco.  
3. O algoritmo calcula a **média ponderada de produtividade** entre manhã, tarde e noite.  
4. O sistema exibe o **pico de produtividade estimado** (por exemplo: “Seu melhor período de foco é pela manhã”).  

---

## 🧱 Estrutura de Pastas (sugestão)

