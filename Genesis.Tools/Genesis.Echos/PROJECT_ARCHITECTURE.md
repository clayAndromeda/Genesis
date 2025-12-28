# Genesis.Echos プロジェクト - アーキテクチャドキュメント

最終更新: 2025-12-28

## 目次

1. [プロジェクト構造](#1-プロジェクト構造)
2. [技術スタック](#2-技術スタック)
3. [アーキテクチャパターン](#3-アーキテクチャパターン)
4. [主要なコンポーネント](#4-主要なコンポーネント)
5. [現在の機能状態](#5-現在の機能状態)
6. [今後の拡張候補](#6-今後の拡張候補)

---

## 1. プロジェクト構造

### ソリューション構成

Genesis.Echos プロジェクトは、4つのメインプロジェクトで構成されています：

#### プロジェクト一覧

| プロジェクト | タイプ | フレームワーク | 用途 |
|------------|--------|---------------|------|
| **Genesis.Echos.Main** | Web (ASP.NET Core) | .NET 10.0 | Blazor Server UI レイヤー |
| **Genesis.Echos.Domain** | Class Library | .NET 10.0 | エンティティ・Enum 定義 |
| **Genesis.Echos.Infrastructure** | Class Library | .NET 10.0 | データアクセス・DbContext・Migrations |
| **Genesis.Echos.Tests** | Test Project | .NET 10.0 | xUnit テスト |

#### プロジェクト間の依存関係

```
Genesis.Echos.Main
├── Genesis.Echos.Domain
│   └── Microsoft.Extensions.Identity.Stores
└── Genesis.Echos.Infrastructure
    ├── Genesis.Echos.Domain
    ├── Microsoft.AspNetCore.Identity.EntityFrameworkCore
    ├── Microsoft.EntityFrameworkCore.SqlServer
    └── Microsoft.EntityFrameworkCore.Tools

Genesis.Echos.Tests
├── Genesis.Echos.Main
├── Genesis.Echos.Domain
└── Genesis.Echos.Infrastructure
```

### ディレクトリ構成

```
Genesis.Echos/
├── Genesis.Echos.sln
├── Genesis.Echos.Main/
│   ├── Program.cs                    # DI設定・認証設定
│   ├── appsettings.json              # 接続文字列・ログ設定
│   ├── Services/
│   │   └── PostService.cs            # ビジネスロジック
│   ├── Components/
│   │   ├── App.razor                 # ルーティング・認証ラッパー
│   │   ├── Routes.razor              # ルートハンドラー
│   │   ├── Layout/
│   │   │   ├── MainLayout.razor
│   │   │   ├── NavMenu.razor
│   │   │   └── ReconnectModal.razor
│   │   ├── Pages/
│   │   │   ├── Home.razor
│   │   │   ├── Account/
│   │   │   │   ├── Login.razor
│   │   │   │   └── Logout.razor
│   │   │   └── Posts/
│   │   │       ├── Index.razor       # 投稿一覧
│   │   │       ├── Create.razor      # 投稿作成
│   │   │       ├── Detail.razor      # 投稿詳細
│   │   │       └── Edit.razor        # 投稿編集
│   │   └── Shared/
│   │       └── LikeButton.razor      # 共有コンポーネント
│   └── wwwroot/
├── Genesis.Echos.Domain/
│   ├── Entities/
│   │   ├── ApplicationUser.cs
│   │   ├── Post.cs
│   │   ├── Like.cs
│   │   ├── Comment.cs
│   │   ├── Tag.cs
│   │   └── PostTag.cs
│   └── Enums/
│       ├── UserRole.cs
│       └── ImportanceLevel.cs
├── Genesis.Echos.Infrastructure/
│   ├── Data/
│   │   ├── ApplicationDbContext.cs
│   │   └── DbInitializer.cs
│   └── Migrations/
└── Genesis.Echos.Tests/
```

---

## 2. 技術スタック

### コアフレームワーク

- **ASP.NET Core**: 10.0
- **Blazor**: Interactive Server (Razor Components)
- **ランタイム**: .NET 10.0

### データベース

- **DBMS**: SQL Server (Docker)
- **ORM**: Entity Framework Core 10.0.1
- **認証**: ASP.NET Core Identity

### NuGetパッケージ

#### Genesis.Echos.Domain
```xml
Microsoft.Extensions.Identity.Stores v10.0.1
```

#### Genesis.Echos.Infrastructure
```xml
Microsoft.AspNetCore.Identity.EntityFrameworkCore v10.0.1
Microsoft.EntityFrameworkCore.SqlServer v10.0.1
Microsoft.EntityFrameworkCore.Tools v10.0.1
```

#### Genesis.Echos.Tests
```xml
Microsoft.NET.Test.Sdk v17.14.1
xunit v2.9.3
Shouldly v4.3.0
```

### データベース接続

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=GenesisEchos;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

### 起動URL

- **HTTP**: http://localhost:5069
- **HTTPS**: https://localhost:7219

---

## 3. アーキテクチャパターン

### レイヤー構造

```
┌─────────────────────────────────────┐
│  Presentation Layer                 │
│  (Blazor Components)                │
└─────────────────┬───────────────────┘
                  │
┌─────────────────▼───────────────────┐
│  Application/Service Layer          │
│  (PostService)                      │
└─────────────────┬───────────────────┘
                  │
┌─────────────────▼───────────────────┐
│  Domain Layer                       │
│  (Entities, Enums)                  │
└─────────────────┬───────────────────┘
                  │
┌─────────────────▼───────────────────┐
│  Infrastructure/Data Layer          │
│  (DbContext, Migrations)            │
└─────────────────┬───────────────────┘
                  │
┌─────────────────▼───────────────────┐
│  SQL Server Database                │
└─────────────────────────────────────┘
```

### 実装されているパターン

#### 1. 依存性注入 (DI)

Program.cs で以下のサービスを登録：

```csharp
// Scoped
services.AddDbContext<ApplicationDbContext>();
services.AddScoped<PostService>();

// Identity Services (Transient)
services.AddDefaultIdentity<ApplicationUser>();
```

#### 2. データアクセスパターン

- **DbContext**: `ApplicationDbContext` (IdentityDbContext 継承)
- **Entity Framework Core** でデータ操作
- **IServiceScopeFactory** を使用した動的スコープ管理

#### 3. 認可パターン

- `[Authorize]` 属性によるページ/コンポーネント保護
- `ClaimsPrincipal` から `UserId` を抽出
- ユーザーロール (`UserRole` enum) 検証

#### 4. ビジネスロジック

- **PostService** クラスで集約
- 作成者チェック (CRUD操作時)
- いいね重複防止 (一意制約)
- エラーハンドリング・ログ記録

### データベース設計の特徴

#### 複合主キー・一意制約

- **PostTag**: `(PostId, TagId)` の複合主キー
- **Like**: `(PostId, UserId)` の一意制約 → 1ユーザー1投稿に対して1いいね

#### カスケード削除設定

- **Post削除時**: Comments/Likes/PostTags 全て自動削除
- **User削除時**: NoAction (アプリケーションレベルで管理)

---

## 4. 主要なコンポーネント

### ドメインモデル

#### ApplicationUser.cs

```csharp
public class ApplicationUser : IdentityUser
{
    public UserRole Role { get; set; } = UserRole.Member;
    public DateTime CreatedAt { get; set; }

    // Navigation Properties
    public ICollection<Post> Posts { get; set; }
    public ICollection<Like> Likes { get; set; }
    public ICollection<Comment> Comments { get; set; }
}
```

#### Post.cs

```csharp
public class Post
{
    public int Id { get; set; }
    public string Title { get; set; }           // 最大200文字
    public string Content { get; set; }         // 最大5000文字
    public string AuthorId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Leader専用フィールド (未実装)
    public bool IsRead { get; set; }
    public ImportanceLevel? Importance { get; set; }

    // Navigation Properties
    public ApplicationUser Author { get; set; }
    public ICollection<PostTag> PostTags { get; set; }
    public ICollection<Like> Likes { get; set; }
    public ICollection<Comment> Comments { get; set; }
}
```

#### Like.cs

```csharp
public class Like
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public string UserId { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation Properties
    public Post Post { get; set; }
    public ApplicationUser User { get; set; }
}
```

#### Comment.cs

```csharp
public class Comment
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public string AuthorId { get; set; }
    public string Content { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation Properties
    public Post Post { get; set; }
    public ApplicationUser Author { get; set; }
}
```

#### Tag.cs

```csharp
public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Color { get; set; }  // HEX色コード
    public DateTime CreatedAt { get; set; }

    // Navigation Properties
    public ICollection<PostTag> PostTags { get; set; }
}
```

#### PostTag.cs (中間テーブル)

```csharp
public class PostTag
{
    public int PostId { get; set; }
    public int TagId { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation Properties
    public Post Post { get; set; }
    public Tag Tag { get; set; }
}
```

#### Enums

```csharp
public enum UserRole
{
    Member = 0,   // 通常ユーザー
    Leader = 1    // リーダー
}

public enum ImportanceLevel
{
    B = 0,  // 低
    A = 1,  // 中
    S = 2   // 高
}
```

### サービス層

#### PostService.cs

主要メソッド:

| メソッド | 説明 | 戻り値 |
|---------|------|--------|
| `GetAllPostsAsync()` | すべての投稿取得 (作成者・いいね数含む) | `List<Post>` |
| `GetPostByIdAsync(int)` | 投稿詳細取得 (作成者・いいね・コメント含む) | `Post` |
| `CreatePostAsync(Post)` | 新規投稿作成 | `Post` |
| `UpdatePostAsync(Post, userId)` | 投稿更新（作成者チェック） | `Post` |
| `DeletePostAsync(int, userId)` | 投稿削除（作成者チェック） | `bool` |
| `ToggleLikeAsync(int, userId)` | いいねの追加/削除 | `void` |
| `HasUserLikedAsync(int, userId)` | ユーザーのいいね状態確認 | `bool` |

**実装特性:**
- `IServiceScopeFactory` で動的スコープ管理
- `ILogger<PostService>` でエラー・情報ログ記録
- 作成者チェック（UpdatePost/DeletePost）
- 例外ハンドリング・ロギング

### UIコンポーネント

#### Layout

- **MainLayout.razor**: サイドバー + メインコンテンツ
- **NavMenu.razor**: ナビゲーションメニュー
- **ReconnectModal.razor**: Blazor Server 再接続モーダル

#### Shared

- **LikeButton.razor**: いいねボタン (再利用可能)
  - パラメータ: `PostId`, `LikeCount`, `IsLiked`
  - イベント: `OnLikeChanged`
  - UI: ハートアイコン + 数値

#### Pages

| ページ | パス | 認証 | 説明 |
|--------|------|------|------|
| Home | `/` | 不要 | ようこそメッセージ |
| Login | `/Account/Login` | 不要 | ログインフォーム |
| Logout | `/Account/Logout` | 不要 | ログアウト画面 |
| Index | `/posts` | 必須 | 投稿一覧 |
| Create | `/posts/create` | 必須 | 投稿作成 |
| Detail | `/posts/{id}` | 必須 | 投稿詳細 |
| Edit | `/posts/{id}/edit` | 必須 | 投稿編集 (作成者のみ) |

---

## 5. 現在の機能状態

### 実装済み機能

#### Phase 1: 基盤構築 ✅

- プロジェクト構造（Main, Domain, Infrastructure, Tests）
- Entity Framework Core + SQL Server（Docker）
- ASP.NET Core Identity設定
- ドメインモデル（Post, Tag, Like, Comment, ApplicationUser）
- データベースマイグレーション
- シードデータ（テストユーザー・タグ）

#### Phase 2: 投稿CRUD機能 ✅

- PostServiceの作成（CRUD操作）
- 投稿一覧・詳細・作成・編集ページ
- Bootstrap 5ベースのUI
- バリデーション実装

#### Phase 3: いいね機能＋認証 ✅

- いいね機能（ToggleLikeAsync, HasUserLikedAsync）
- LikeButtonコンポーネント（InteractiveServer）
- ログイン/ログアウトページ
- 認証状態のカスケード設定
- レンダリングモードの最適化

### 認証・認可

#### ASP.NET Core Identity 設定

- **ユーザー管理**: `UserManager<ApplicationUser>`
- **サインイン管理**: `SignInManager<ApplicationUser>`
- **ロール管理**: `RoleManager<IdentityRole>`
- **デフォルトロール**: "Leader", "Member"

#### パスワード要件

- 最小長: 8文字
- 大文字必須
- 小文字必須
- 数字必須
- 特殊文字必須
- メールアドレス一意性: 必須

#### デフォルトユーザー

| 役割 | メールアドレス | パスワード |
|-----|---------------|-----------|
| Leader | leader@echos.com | Leader123! |
| Member | member@echos.com | Member123! |

#### 認証フロー

1. ユーザーが `/Account/Login` にアクセス
2. メール・パスワード入力
3. `SignInManager.PasswordSignInAsync()` で検証
4. 成功時: 指定URL へリダイレクト (デフォルト: `/posts`)
5. 失敗時: エラーメッセージ表示

### 検証・エラーハンドリング

#### フォーム検証

- `EditForm` + `DataAnnotationsValidator` 使用
- `ValidationSummary` 表示
- `ValidationMessage` (フィールド単位)

#### 制限事項

- **Title**: 必須・最大200文字
- **Content**: 必須・最大5000文字

#### エラーメッセージ

- ログイン失敗: "メールアドレスまたはパスワードが正しくありません"
- 投稿作成/更新失敗: 例外メッセージ表示
- 権限エラー: "この投稿を編集する権限がありません"

### データベース初期化

**DbInitializer.cs で実行:**

1. マイグレーション実行
2. ロール作成: "Leader", "Member"
3. デフォルトユーザー作成
4. デフォルトタグ作成 (5個)

**デフォルトタグ:**

| タグ名 | 色 | Bootstrap Class |
|--------|----|-----------------|
| アイデア | 青 | primary |
| バグ報告 | 赤 | danger |
| 改善提案 | 緑 | success |
| 質問 | 黄 | warning |
| その他 | グレー | secondary |

### UI フレームワーク

- **Bootstrap 5** (wwwroot/lib/bootstrap)
- **Bootstrap Icons** (bi クラス)
- **Blazor Server Interactivity**
- **Razor Components** (.razor ファイル)

---

## 6. 今後の拡張候補

### 未実装機能

#### Phase 4: タグ機能 (予定)

- [ ] 投稿へのタグ紐付け
- [ ] タグによる投稿フィルタリング
- [ ] タグの作成・編集・削除
- [ ] タグ一覧ページ

#### Phase 5: リーダー機能 (予定)

- [ ] 既読管理 (IsRead フラグ)
- [ ] 重要度設定 (ImportanceLevel)
- [ ] Leader専用ダッシュボード
- [ ] メンバーの投稿状況確認

#### その他の拡張候補

- [ ] **コメント機能**: 作成・編集・削除
- [ ] **検索機能**: タイトル・本文の全文検索
- [ ] **ページネーション**: 投稿一覧の分割表示
- [ ] **ソート機能**: 日付・いいね数・コメント数でソート
- [ ] **ユーザープロフィール**: プロフィール編集・アバター
- [ ] **通知機能**: いいね・コメント時の通知
- [ ] **添付ファイル**: 画像・ファイルアップロード
- [ ] **マークダウン対応**: 投稿本文のMarkdownレンダリング
- [ ] **下書き機能**: 投稿の一時保存
- [ ] **投稿テンプレート**: よく使う投稿フォーマット
- [ ] **APIエンドポイント**: REST API または GraphQL
- [ ] **モバイル対応**: レスポンシブデザインの強化

### 技術的改善候補

- [ ] **パフォーマンス最適化**: クエリの最適化、キャッシング
- [ ] **テストカバレッジ向上**: ユニットテスト・統合テストの追加
- [ ] **CI/CD パイプライン**: GitHub Actions などの導入
- [ ] **Docker Compose**: アプリケーション全体のコンテナ化
- [ ] **ログ記録**: Serilog などの構造化ログ
- [ ] **監視・アラート**: Application Insights などの導入
- [ ] **セキュリティ強化**: CSRF対策、XSS対策の強化
- [ ] **国際化 (i18n)**: 多言語対応
- [ ] **アクセシビリティ**: WCAG 2.1準拠

---

## 依存関係マトリックス

| プロジェクト | Domain | Infrastructure | Main | Tests |
|-------------|--------|----------------|------|-------|
| Domain | - | - | ✓ | ✓ |
| Infrastructure | ✓ | - | ✓ | ✓ |
| Main | ✓ | ✓ | - | ✓ |
| Tests | ✓ | ✓ | ✓ | - |

---

## まとめ

**Genesis.Echos** はゲーム開発チーム向けの掲示板システムで、以下の特徴があります：

- **モダンなアーキテクチャ**: ASP.NET Core 10.0 + Blazor Server
- **完全な認証・認可**: ASP.NET Core Identity + ロールベース (Member/Leader)
- **データベース**: Entity Framework Core + SQL Server
- **クリーンなレイヤー構造**: Domain → Service → Presentation
- **リアルタイムUI**: Blazor Interactive Server コンポーネント
- **Phase 3 完了**: 基本的な投稿管理・いいね・ログイン機能実装済み
- **拡張性**: Leader専用機能（コメント・重要度）の基盤が実装されているが、UI は未実装

### 設計の品質

- **関心の分離**: ドメイン駆動設計 (DDD) の基本原則に従っています
- **最新技術**: .NET 10.0、Nullable参照型、暗黙的なusing
- **拡張性**: 新機能追加のための基盤が整っています

---

**絶対パス:**
- ソリューション: `/Users/user/src/projects/Genesis/Genesis.Tools/Genesis.Echos/Genesis.Echos.sln`
- Program.cs: `/Users/user/src/projects/Genesis/Genesis.Tools/Genesis.Echos/Genesis.Echos.Main/Program.cs`
- DbContext: `/Users/user/src/projects/Genesis/Genesis.Tools/Genesis.Echos/Genesis.Echos.Infrastructure/Data/ApplicationDbContext.cs`
