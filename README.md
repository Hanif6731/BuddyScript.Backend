# BuddyScript — Backend

ASP.NET Core 9 REST API for BuddyScript, a social feed platform. Handles authentication, post management, comments, reactions, and image serving.

**Live API:** proxied via `https://buddyscript.xchanze.com/api`  
**Frontend repo:** https://github.com/Hanif6731/BuddyScript.Client

---

## Tech Stack

| Layer | Choice |
|---|---|
| Framework | ASP.NET Core 9 (Minimal Hosting) |
| ORM | Entity Framework Core 10 |
| Database | SQL Server 2022 (Docker) |
| Auth | JWT (HttpOnly cookie) + Google OAuth |
| Image processing | SixLabors.ImageSharp |
| Containerisation | Docker + Docker Compose |
| CI/CD | GitHub Actions → self-hosted Windows runner |

---

## Architecture

```
Controllers  →  Services  →  Repositories  →  DbContext (EF Core)
                    ↓
              InputSanitizer (XSS strip)
```

- **Repository pattern** — all DB access goes through `IPostRepository`, `ICommentRepository`, `ILikeRepository`, `IUserRepository`.
- **Service layer** — business logic, transactions, sanitisation.
- **DTOs** — all inputs/outputs use typed DTOs with data annotations; no entity objects are exposed.
- **Polymorphic likes** — `Like` table uses `EntityId + EntityType (Post=0, Comment=1)` rather than two separate FK columns. Reaction type (Like / Love / Haha / Wow / Sad) is stored alongside.

---

## Key Design Decisions

### Authentication
- JWT stored in an **HttpOnly, Secure, SameSite=None** cookie — inaccessible to JavaScript, immune to XSS token theft.
- Google OAuth (`/api/Auth/google-login`) provisions a local account on first sign-in.
- Passwords hashed with **BCrypt** (work factor 12).
- Strong password policy enforced both client-side and via `[StrongPassword]` validation attribute on the backend.

### Security
| Concern | Mitigation |
|---|---|
| XSS (stored) | `InputSanitizer` strips all HTML/script tags on every text input before DB write |
| CSRF | SameSite=None cookie + CORS allow-list |
| DoS / brute-force | ASP.NET Core rate limiter: `auth` (5/15 min), `writes` (30/min), `reads` (150/min) |
| Oversized uploads | Kestrel `MaxRequestBodySize = 15 MB`; Nginx `client_max_body_size 15m` |
| SQL injection | Fully parameterised via EF Core; no raw SQL |

### Performance at Scale
- **Indexes** on `Posts(IsPublic, CreatedAt DESC)`, `Likes(EntityId, EntityType)`, `Likes(UserId, EntityId, EntityType)` UNIQUE, `Comments(PostId)`.
- Likes loaded **per-batch** with a single `WHERE EntityId IN (...)` query rather than N+1 joins — a single page load issues exactly **3 DB queries** regardless of post count.
- `AsSplitQuery()` on the feed query avoids cartesian explosion from multi-level includes.
- All repositories return `IQueryable<T>` — callers compose filters/pagination before materialisation.
- Images stored as compressed JPEG (`Quality=80`, max width 1200px) in the DB; served via `/api/Feed/image/{id}`.

### Database Migrations
EF Core migrations run **automatically on startup** (`db.Database.Migrate()`), so deployments are zero-touch.

---

## Running Locally

### Prerequisites
- .NET 9 SDK
- Docker Desktop (for the SQL Server container)

### 1. Start the database
```bash
# from BuddyScript.Backend/
docker compose -f docker-compose.db.yml up -d
```

### 2. Configure secrets
```bash
cp appsettings.Development.json.example appsettings.Development.json
# edit appsettings.Development.json — fill in Jwt:Key, Google OAuth credentials
```

### 3. Run
```bash
dotnet run
# API available at http://localhost:9384
```

---

## Environment Variables (Production)

Set as GitHub environment secrets under the `production` environment:

| Variable | Description |
|---|---|
| `PROD_DB_CONNECTION` | SQL Server connection string (`Server=db,1433;...`) |
| `PROD_JWT_KEY` | JWT signing key (≥ 32 chars) |
| `PROD_JWT_ISSUER` | JWT issuer string |
| `PROD_JWT_AUDIENCE` | JWT audience string |
| `PROD_JWT_COOKIE_NAME` | Cookie name (e.g. `buddyscript_auth`) |
| `PROD_GOOGLE_CLIENT_ID` | Google OAuth client ID |
| `PROD_GOOGLE_CLIENT_SECRET` | Google OAuth client secret |

See `.env.example` for the full list.

---

## API Endpoints

| Method | Path | Auth | Description |
|---|---|---|---|
| POST | `/api/Auth/register` | ❌ | Register new user |
| POST | `/api/Auth/login` | ❌ | Email/password login |
| POST | `/api/Auth/google-login` | ❌ | Google OAuth login |
| POST | `/api/Auth/logout` | ❌ | Clear auth cookie |
| GET | `/api/Auth/getcurrentuser` | ✅ | Current user info |
| GET | `/api/Feed/posts` | ✅ | Paginated feed |
| POST | `/api/Feed/posts` | ✅ | Create post (multipart) |
| GET | `/api/Feed/image/{id}` | ✅ | Serve post image |
| GET | `/api/Interactions/comments/{postId}` | ✅ | Top-level comments |
| GET | `/api/Interactions/replies/{commentId}` | ✅ | Replies for comment |
| POST | `/api/Interactions/comment` | ✅ | Add comment/reply |
| POST | `/api/Interactions/like` | ✅ | Toggle reaction |
| GET | `/api/Interactions/likers/{entityId}/{entityType}` | ✅ | Who liked an entity |
| GET | `/api/Settings` | ✅ | User settings |
| PATCH | `/api/Settings/theme` | ✅ | Update theme |

---

## Deployment

GitHub Actions workflow (`.github/workflows/deploy.yml`) triggers on push to `main`:
1. Writes secrets to `.env`
2. Runs `docker compose down && docker compose up -d --build`
3. Cleans up `.env`

All containers run with `restart: unless-stopped` — they auto-recover after host reboots or Docker daemon restarts.
