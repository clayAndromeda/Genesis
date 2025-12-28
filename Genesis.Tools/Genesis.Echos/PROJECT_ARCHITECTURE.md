# Genesis.Echos - プロジェクトアーキテクチャ

最終更新: 2025-12-29

## 目次

1. [プロジェクト概要](#プロジェクト概要)
2. [プロジェクト構造](#プロジェクト構造)
3. [技術スタック](#技術スタック)
4. [アーキテクチャ](#アーキテクチャ)
5. [ドメインモデル](#ドメインモデル)
6. [主要機能](#主要機能)
7. [認証・認可](#認証認可)
8. [今後の拡張](#今後の拡張)

---

## プロジェクト概要

**Genesis.Echos** はゲーム開発チーム向けの掲示板システムです。

### 特徴

- **モダンなアーキテクチャ**: ASP.NET Core 10.0 + Blazor Server
- **完全な認証・認可**: ASP.NET Core Identity + ロールベース
- **3層ユーザーロール**: Admin（管理者）、Leader（リーダー）、Member（通常メンバー）
- **データベース**: Entity Framework Core + SQL Server (Docker)
- **リアルタイムUI**: Blazor Interactive Server コンポーネント

---

## プロジェクト構造

### ソリューション構成

```
Genesis.Echos/
├── Genesis.Echos.sln
├── Genesis.Echos.Main/          # Blazor Server UI
├── Genesis.Echos.Domain/        # エンティティ・Enum
├── Genesis.Echos.Infrastructure/ # DbContext・Migrations
└── Genesis.Echos.Tests/         # xUnit テスト
```

### 依存関係

```
Main → Domain + Infrastructure
Infrastructure → Domain
Tests → Main + Domain + Infrastructure
```

### ディレクトリ詳細

```
Genesis.Echos.Main/
├── Program.cs                    # DI設定・認証設定・起動処理
├── appsettings.json             # 接続文字列・管理者設定
├── Services/
│   ├── PostService.cs           # 投稿CRUD・いいね機能
│   ├── AdminService.cs          # 管理者機能（ユーザー・投稿管理）
│   └── TagService.cs            # タグ取得
└── Components/
    ├── Layout/
    │   ├── MainLayout.razor     # 全体レイアウト（ヘッダー・サイドバー）
    │   └── NavMenu.razor        # サイドバーナビゲーション
    ├── Pages/
    │   ├── Home.razor           # トップページ
    │   ├── Account/
    │   │   ├── Login.razor      # ログイン
    │   │   ├── Logout.razor     # ログアウト
    │   │   └── Register.razor   # アカウント作成
    │   ├── Admin/
    │   │   ├── Users.razor      # ユーザー管理（Admin専用）
    │   │   └── Posts.razor      # 投稿管理（Admin専用）
    │   └── Posts/
    │       ├── Index.razor      # 投稿一覧
    │       ├── Create.razor     # 投稿作成
    │       ├── Detail.razor     # 投稿詳細
    │       └── Edit.razor       # 投稿編集
    └── Shared/
        ├── LikeButton.razor     # いいねボタン（InteractiveServer）
        ├── TagBadge.razor       # タグバッジ
        └── AdminButton.razor    # 管理画面ボタン（InteractiveServer）

Genesis.Echos.Domain/
├── Entities/
│   ├── ApplicationUser.cs       # ユーザー（Identity拡張）
│   ├── Post.cs                  # 投稿
│   ├── Tag.cs                   # タグ
│   ├── PostTag.cs              # 投稿-タグ中間テーブル
│   ├── Like.cs                  # いいね
│   └── Comment.cs              # コメント
└── Enums/
    ├── UserRole.cs             # Member/Leader/Admin
    └── ImportanceLevel.cs      # B/A/S

Genesis.Echos.Infrastructure/
├── Data/
│   ├── ApplicationDbContext.cs  # EF Core DbContext
│   └── DbInitializer.cs        # DB初期化・シードデータ
└── Migrations/                  # EF Coreマイグレーション
```

---

## 技術スタック

### フレームワーク

- **ASP.NET Core**: 10.0
- **Blazor**: Interactive Server (一部ページで使用)
- **Entity Framework Core**: 10.0.1
- **ASP.NET Core Identity**: 認証・認可

### データベース

- **SQL Server 2022** (Docker)
- **接続文字列**: `Server=localhost,1433;Database=GenesisEchos;...`

### NuGetパッケージ

#### Main
- Microsoft.EntityFrameworkCore.Design (10.0.1)

#### Domain
- Microsoft.Extensions.Identity.Stores (10.0.1)

#### Infrastructure
- Microsoft.AspNetCore.Identity.EntityFrameworkCore (10.0.1)
- Microsoft.EntityFrameworkCore.SqlServer (10.0.1)
- Microsoft.EntityFrameworkCore.Tools (10.0.1)

#### Tests
- xunit (2.9.3)
- Shouldly (4.3.0)
- Microsoft.NET.Test.Sdk (17.14.1)

### 起動URL

- **HTTP**: http://localhost:5069

---

## アーキテクチャ

### レイヤー構造

```
┌─────────────────────────────────────┐
│  Presentation Layer                 │
│  (Blazor Components)                │
│  - Pages (Login, Posts, Admin)      │
│  - Shared (LikeButton, TagBadge)    │
└─────────────────┬───────────────────┘
                  │
┌─────────────────▼───────────────────┐
│  Application/Service Layer          │
│  - PostService                      │
│  - TagService                       │
│  - AdminService                     │
└─────────────────┬───────────────────┘
                  │
┌─────────────────▼───────────────────┐
│  Domain Layer                       │
│  - Entities (User, Post, Tag...)    │
│  - Enums (UserRole, Importance...)  │
└─────────────────┬───────────────────┘
                  │
┌─────────────────▼───────────────────┐
│  Infrastructure/Data Layer          │
│  - ApplicationDbContext             │
│  - Migrations                       │
│  - DbInitializer                    │
└─────────────────┬───────────────────┘
                  │
┌─────────────────▼───────────────────┐
│  SQL Server Database                │
└─────────────────────────────────────┘
```

### 設計パターン

#### 依存性注入（DI）

```csharp
// Program.cs
builder.Services.AddDbContext<ApplicationDbContext>();
builder.Services.AddScoped<PostService>();
builder.Services.AddScoped<TagService>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddDefaultIdentity<ApplicationUser>();
```

#### リポジトリパターン（簡易版）

- `PostService`, `TagService`, `AdminService` がリポジトリの役割
- `IServiceScopeFactory` で動的スコープ管理
- EF Core の `DbContext` を直接使用

#### 認可パターン

- `[Authorize]` 属性でページ保護
- `[Authorize(Roles = "Admin")]` でロールベース認可
- `ClaimsPrincipal` からユーザーID取得

---

## ドメインモデル

### ユーザーロール

```csharp
public enum UserRole
{
    Member = 0,  // 通常メンバー（デフォルト）
    Leader = 1,  // リーダー（将来の拡張用）
    Admin = 2    // 管理者
}
```

### 重要度レベル（将来の拡張用）

```csharp
public enum ImportanceLevel
{
    B = 0,  // 低
    A = 1,  // 中
    S = 2   // 高
}
```

### エンティティ

#### ApplicationUser
- ASP.NET Core Identity の `IdentityUser` を継承
- `Role`: UserRole (Member/Leader/Admin)
- `CreatedAt`: 作成日時
- ナビゲーション: Posts, Likes, Comments

#### Post
- 投稿の基本情報（Title, Content, AuthorId）
- タイムスタンプ（CreatedAt, UpdatedAt）
- 将来の拡張: IsRead, Importance（Leader専用）
- ナビゲーション: Author, PostTags, Likes, Comments

#### Tag
- タグ情報（Name, Color）
- デフォルトで5種類のタグが初期化される
- ナビゲーション: PostTags

#### PostTag（中間テーブル）
- 投稿とタグの多対多リレーション
- 複合主キー: (PostId, TagId)

#### Like
- いいね情報（PostId, UserId）
- 一意制約: (PostId, UserId) → 1ユーザー1投稿1いいね
- ナビゲーション: Post, User

#### Comment
- コメント情報（PostId, AuthorId, Content）
- 将来の拡張用（現在UIなし）
- ナビゲーション: Post, Author

### データベース設計の特徴

- **複合主キー**: PostTag (PostId, TagId)
- **一意制約**: Like (PostId, UserId)
- **カスケード削除**: Post削除時にComments/Likes/PostTagsも削除
- **循環参照防止**: User削除時はNoAction

---

## 主要機能

### 実装済み機能

#### Phase 1: 基盤構築 ✅
- プロジェクト構造
- Entity Framework Core + SQL Server (Docker)
- ASP.NET Core Identity設定
- ドメインモデル
- マイグレーション・シードデータ

#### Phase 2: 投稿CRUD ✅
- PostService（作成・読取・更新・削除）
- 投稿一覧・詳細・作成・編集ページ
- バリデーション（Title: 200文字、Content: 5000文字）
- 作成者チェック（編集・削除）

#### Phase 3: いいね機能＋認証 ✅
- いいね機能（ToggleLikeAsync, HasUserLikedAsync）
- LikeButtonコンポーネント（InteractiveServer）
- ログイン/ログアウトページ
- 認証状態のカスケード設定

#### Phase 4: タグ機能 ✅
- TagService（タグ取得）
- 投稿作成・編集時のタグ選択（複数選択可）
- TagBadgeコンポーネント
- 投稿一覧・詳細でのタグ表示
- デフォルトタグ: アイデア/バグ報告/改善提案/質問/その他

#### Phase 5: アカウント作成 ✅
- 新規ユーザー登録ページ
- 自動的にMemberロール付与
- 登録後の自動ログイン
- MainLayoutでの認証UI改善

#### Phase 6: 管理者機能 ✅
- AdminService（ユーザー・投稿管理）
- ユーザー管理画面（ロール変更・削除）
- 投稿管理画面（投稿削除）
- 管理者ボタン（AdminButton）
- appsettings.jsonで管理者メール設定
- ログイン時の自動Admin昇格

### サービス層の主要メソッド

#### PostService
- `GetAllPostsAsync()`: 投稿一覧取得
- `GetPostByIdAsync(int)`: 投稿詳細取得
- `CreatePostAsync(Post, List<int>)`: 投稿作成（タグ付き）
- `UpdatePostAsync(Post, userId, List<int>)`: 投稿更新
- `DeletePostAsync(int, userId)`: 投稿削除
- `DeletePostAsAdminAsync(int)`: 管理者権限での投稿削除
- `ToggleLikeAsync(int, userId)`: いいねトグル
- `HasUserLikedAsync(int, userId)`: いいね状態確認

#### AdminService
- `GetAllUsersAsync()`: 全ユーザー取得
- `ChangeUserRoleAsync(userId, UserRole)`: ロール変更（Admin除く）
- `DeleteUserAsync(userId)`: ユーザー削除（Admin除く）
- `DeletePostAsync(postId)`: 投稿削除（管理者権限）

#### TagService
- `GetAllTagsAsync()`: 全タグ取得
- `GetTagByIdAsync(int)`: タグ取得

---

## 認証・認可

### ASP.NET Core Identity設定

- `UserManager<ApplicationUser>`: ユーザー管理
- `SignInManager<ApplicationUser>`: サインイン管理
- `RoleManager<IdentityRole>`: ロール管理
- デフォルトロール: Admin, Leader, Member

### パスワード要件

- 最小長: 8文字
- 大文字・小文字・数字・特殊文字: 必須
- メールアドレス一意性: 必須

### 管理者設定

appsettings.jsonで管理者を設定:

```json
{
  "AdminSettings": {
    "AdminEmails": [
      "admin@example.com"
    ]
  }
}
```

- アプリ起動時にAdminロール昇格
- ログイン時にAdminロール昇格（CheckAndPromoteToAdmin）

### デフォルトデータ

#### ロール
- Admin, Leader, Member

#### タグ
- アイデア (#0d6efd)
- バグ報告 (#dc3545)
- 改善提案 (#198754)
- 質問 (#ffc107)
- その他 (#6c757d)

### 認証フロー

1. `/Account/Login` でメール・パスワード入力
2. `SignInManager.PasswordSignInAsync()` で検証
3. 管理者メール設定チェック→必要に応じてAdmin昇格
4. 成功時: `/posts` へリダイレクト
5. 失敗時: エラーメッセージ表示

### Blazor認証の注意点

#### レンダリングモード
- **認証ページ（Login/Logout/Register）**: `@rendermode` なし（静的サーバーレンダリング）
  - 理由: ASP.NET Core IdentityがHTTPクッキーを設定するため
- **インタラクティブコンポーネント（LikeButton/AdminButton）**: `@rendermode InteractiveServer`
  - 理由: クリックイベント等のリアルタイム処理が必要
- **レイアウト（MainLayout）**: `@rendermode` 不可
  - 理由: レイアウトには`@rendermode`を指定できない
  - 対策: インタラクティブな部分を別コンポーネント（AdminButton）に分離

#### 認証状態のカスケード
- `Routes.razor` で `<CascadingAuthenticationState>` 使用
- `AuthorizeRouteView` で `[Authorize]` 属性をサポート
- InteractiveServerコンポーネントは新しいサーキットを作成するため、親から認証状態を受け取れない
  - 解決策: 親で `AuthorizeView` チェック→子で機能実装

---

## 今後の拡張

### 未実装機能

#### Leader専用機能（Phase 7候補）
- 既読管理（IsReadフラグ）
- 重要度設定（ImportanceLevel）
- コメント機能のUI実装
- Leaderダッシュボード

#### 一般機能拡張
- 検索機能（タイトル・本文）
- ページネーション
- ソート機能（日付・いいね数）
- ユーザープロフィール編集
- 通知機能
- 添付ファイル
- Markdown対応
- 下書き機能

#### 技術的改善
- パフォーマンス最適化（キャッシング）
- テストカバレッジ向上
- CI/CD パイプライン
- Docker Compose（アプリ全体のコンテナ化）
- 構造化ログ（Serilog）
- 監視・アラート

---

## トラブルシューティング

詳細は [TROUBLESHOOTING.md](TROUBLESHOOTING.md) を参照

### 主なトピック
- SQL Serverコンテナ管理
- データベースリセット
- Blazor認証エラー（5つの主要エラー）
- レンダリングモード選択ガイドライン
- Entity Framework Coreエラー

---

## 絶対パス

- ソリューション: `/Users/user/src/projects/Genesis/Genesis.Tools/Genesis.Echos/Genesis.Echos.sln`
- Program.cs: `/Users/user/src/projects/Genesis/Genesis.Tools/Genesis.Echos/Genesis.Echos.Main/Program.cs`
- DbContext: `/Users/user/src/projects/Genesis/Genesis.Tools/Genesis.Echos/Genesis.Echos.Infrastructure/Data/ApplicationDbContext.cs`
