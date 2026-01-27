# RBAC API – ASP.NET Core (.NET 8)

API REST desenvolvida em C# com foco em autenticação segura e controle de acesso baseado em papéis (RBAC – Role Based Access Control).

Projeto criado com objetivo de estudo avançado e empregabilidade, seguindo padrões comuns utilizados em ambientes corporativos.

---

## 🎯 Objetivo

Construir uma API back-end profissional utilizando ASP.NET Core, com:

- Autenticação via JWT
- Autorização baseada em roles
- Persistência de dados com PostgreSQL
- Estrutura de código organizada e escalável

Este projeto será utilizado como portfólio técnico no GitHub e currículo.

---

## 🧱 Tecnologias Utilizadas

- C# / .NET 8 (ASP.NET Core Web API)
- Entity Framework Core
- PostgreSQL
- JWT (JSON Web Token)
- Swagger (OpenAPI)
- Git / GitHub

---
    
## 🗂 Estrutura do Projeto

---

## ⚙️ Modo InMemory (opção B)

O modo InMemory usa armazenamento em memória e **zera os dados ao reiniciar a API**. Para isso funcionar corretamente:

- O `InMemoryUserStore` é registrado como **Singleton** no DI.
- Os dados ficam em listas **não estáticas**, garantindo reset ao reiniciar.

### Seed de Admin (opcional)

Para criar automaticamente um usuário admin no startup, adicione no `appsettings.Development.json`:

```json
{
  "Seed": {
    "Admin": {
      "Email": "admin@local",
      "Name": "Admin",
      "Password": "Admin123!"
    }
  }
}
```

> Se qualquer campo estiver vazio, o seed é ignorado.

---

## 🔐 Fluxo no Swagger (passo a passo)

1. **Register** (`POST /auth/register`)  
   Crie um usuário e copie o `token` retornado.
2. **Login** (`POST /auth/login`)  
   Faça login com email/senha e copie o `token`.
3. **Authorize**  
   Clique em **Authorize** e cole **apenas o token** (sem `Bearer`).
4. **Me** (`GET /users/me`)  
   Retorna dados do usuário + roles.  
   - Se não estiver autenticado: **401**.  
   - Se o usuário não existir no InMemory: **404**.
5. **Admin Ping** (`GET /admin/ping`)  
   - Usuário comum: **403** (sem role Admin).  
   - Usuário admin: **200**.
