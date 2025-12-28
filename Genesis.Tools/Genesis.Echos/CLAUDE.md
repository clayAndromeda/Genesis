# Genesis.Echos - Claude作業ガイド

このドキュメントは、Claude CodeがGenesis.Echosプロジェクトで作業する際の重要なルール・パターン・制約をまとめたものです。

---

## 基本方針

### コミュニケーション
- **回答は日本語を使用すること**

### 設計思想
- **将来的な機能拡張に耐えられるアーキテクチャ・設計を意識する**
  - SOLID原則に基づく設計
  - 関心の分離（Separation of Concerns）
  - 疎結合な設計
  - 拡張性を考慮した実装

---

## プロジェクト概要

Genesis.Echosは、ゲーム開発チーム向けの掲示板システムです。

### 技術スタック
- **フレームワーク**: ASP.NET Core 10.0 + Blazor Server
- **データベース**: SQL Server 2022 (Docker)
- **ORM**: Entity Framework Core 10.0
- **認証**: ASP.NET Core Identity
- **テスト**: xUnit + Shouldly

### アーキテクチャ
3層アーキテクチャ + ドメイン層

```
Presentation (Main)
    ↓
Service Layer (Main/Services)
    ↓
Domain Layer (Domain)
    ↓
Infrastructure (Infrastructure)
    ↓
Database (SQL Server)
```

---

## プロジェクト構造のルール

### Genesis.Echos.Main
- **役割**: Blazor Server UI層
- **配置するもの**:
  - Blazor Pages（Components/Pages/）
  - Blazor Components（Components/Shared/）
  - Services（Services/）
  - Program.cs（DI設定・認証設定）

### Genesis.Echos.Domain
- **役割**: ドメインモデル層
- **配置するもの**:
  - エンティティ（Entities/）
  - Enum（Enums/）
- **禁止事項**: ビジネスロジックは含めない（純粋なデータモデルのみ）

### Genesis.Echos.Infrastructure
- **役割**: データアクセス層
- **配置するもの**:
  - ApplicationDbContext（Data/）
  - Migrations（Migrations/）
  - DbInitializer（Data/）
- **禁止事項**: ビジネスロジックは含めない（データアクセスのみ）

### Genesis.Echos.Tests
- **役割**: テストプロジェクト
- **フレームワーク**: xUnit + Shouldly

---

## Blazor開発の重要ルール

### レンダリングモード

#### 1. 認証ページ（Login/Logout/Register）
```razor
@page "/Account/Login"
<!-- @rendermode は指定しない（静的サーバーレンダリング） -->
```

**理由**: ASP.NET Core IdentityがHTTPクッキーを設定するため、ストリーミングレスポンス（InteractiveServer）と互換性がない

#### 2. インタラクティブコンポーネント（LikeButton/AdminButton等）
```razor
@rendermode InteractiveServer

<button @onclick="HandleClick">クリック</button>
```

**理由**: クリックイベント等のリアルタイム処理が必要なため

**適用対象**:
- いいねボタン（LikeButton.razor）
- 管理画面ボタン（AdminButton.razor）
- その他、ユーザー操作に応答する必要があるコンポーネント

#### 3. レイアウト（MainLayout等）
```razor
@inherits LayoutComponentBase
<!-- @rendermode は指定できない -->
```

**制約**: レイアウトには`@rendermode`を指定できない

**対策**: インタラクティブな部分を別コンポーネントに分離
- 例: MainLayoutに管理画面ボタンを配置する場合、AdminButtonコンポーネントとして分離

### ナビゲーション

#### InteractiveServerページでのナビゲーション
**禁止**: `<a href>`リンクの使用
```razor
<!-- ❌ これは使わない -->
<a href="/admin/users">ユーザー管理</a>
```

**推奨**: NavigationManagerの使用
```razor
<!-- ✅ これを使う -->
<span @onclick='() => NavigationManager.NavigateTo("/admin/users")'>
    ユーザー管理
</span>

@code {
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
}
```

**理由**: InteractiveServerモードで`<a href>`を使用すると、新しいサーキットが作成され、認証状態が失われる

### 認証状態のカスケード

#### 問題
InteractiveServerコンポーネントは新しいサーキットを作成するため、親から認証状態のカスケードパラメータを受け取れない

#### 解決策
親で`AuthorizeView`チェック、子で機能実装

```razor
<!-- 親（MainLayout.razor） -->
<AuthorizeView Roles="Admin">
    <Authorized>
        <AdminButton />
    </Authorized>
</AuthorizeView>

<!-- 子（AdminButton.razor） -->
@rendermode InteractiveServer

<button @onclick="NavigateToAdmin">管理画面</button>

@code {
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    private void NavigateToAdmin()
    {
        NavigationManager.NavigateTo("/admin/users");
    }
}
```

**ポイント**:
- 認証チェックは親（非インタラクティブ）で実施
- ボタンクリック等の機能は子（インタラクティブ）で実装

---

## コーディング規約

### C# スタイル
- **Nullable参照型**: 有効（プロジェクト全体で有効化済み）
- **暗黙的なusing**: 使用する
- **record型**: 使用しない（classを使用）

### Serviceクラスのパターン
```csharp
public class PostService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PostService> _logger;

    public PostService(IServiceScopeFactory scopeFactory, ILogger<PostService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<List<Post>> GetAllPostsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // データアクセス処理
        _logger.LogInformation("取得完了");
        return posts;
    }
}
```

**重要なポイント**:
- `IServiceScopeFactory`を使用して動的にスコープを作成
- `ILogger<T>`で必ずログを記録
- 例外ハンドリングとログ記録を実装

---

## データベース作業

### マイグレーション作成
```bash
dotnet ef migrations add MigrationName \
  --project Genesis.Echos.Infrastructure \
  --startup-project Genesis.Echos.Main
```

### マイグレーション適用
```bash
dotnet ef database update \
  --project Genesis.Echos.Infrastructure \
  --startup-project Genesis.Echos.Main
```

### シードデータ
- DbInitializer.csで管理
- ロール（Admin/Leader/Member）
- タグ（アイデア/バグ報告/改善提案/質問/その他）
- 管理者の自動昇格（appsettings.json）

### 管理者設定
appsettings.jsonで管理者メールを設定:
```json
{
  "AdminSettings": {
    "AdminEmails": [
      "admin@example.com"
    ]
  }
}
```

---

## ドキュメント更新ルール

### 新機能リリース時
以下のドキュメントを**必要に応じて**更新すること:

1. **PROJECT_ARCHITECTURE.md**
   - プロジェクト全体の構造・俯瞰
   - 新しいサービス・コンポーネントの追加
   - アーキテクチャの変更

2. **IMPLEMENTATION.md**
   - 実装ログをPhase単位で記録
   - 技術的な学び・解決した課題

3. **README.md**
   - 機能概要
   - 使い方
   - セットアップ手順

### ドキュメント作成の原則
- **古い情報は削除**し、常に最新状態を保つ
- **1ファイルごとの細かい解説は不要**、全体の構造がわかる粒度でまとめる
- **プロジェクトを俯瞰**できる内容にする

---

## トラブルシューティング

### トラブル発生時の対応
1. **トラブルの内容と解決方法をTROUBLESHOOTING.mdに追記**
2. エラーメッセージ、原因、解決策を明記
3. 将来同じエラーに遭遇した時のための記録

### よくあるトラブル
- Blazor認証エラー → TROUBLESHOOTING.md参照
- SQL Serverコンテナ管理 → TROUBLESHOOTING.md参照
- Entity Framework Coreエラー → TROUBLESHOOTING.md参照

---

## コミットルール

### 基本原則
- **ユーザーの指示があるまでコミットしない**
- 大きな機能追加時に未コミットの作業がある場合、**コミットを促す**

### コミット手順
```bash
# 1. 全変更をステージング
git add .

# 2. コミット（Claude Code署名付き）
git commit -m "feat: <機能概要>

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>"
```

### コミットメッセージフォーマット
- `feat:` - 新機能追加
- `fix:` - バグ修正
- `docs:` - ドキュメント更新
- `refactor:` - リファクタリング
- `test:` - テスト追加

---

## テストルール

### ビルド確認
```bash
cd Genesis.Echos.Main
dotnet build
```

**必須**: ビルド成功を確認してから作業完了とすること

### 動作確認
- 主要機能は実際に動作確認を行う
- ログイン/ログアウト
- CRUD操作
- いいね機能
- 管理画面（Admin）

### テストコード
- xUnit + Shouldlyを使用
- 新機能追加時はテストコードも追加推奨

---

## よくある作業パターン

### 新しいServiceの追加
1. `Genesis.Echos.Main/Services/`にServiceクラス作成
2. `IServiceScopeFactory`と`ILogger<T>`を注入
3. `Program.cs`でDI登録（`builder.Services.AddScoped<T>()`）

### 新しいページの追加
1. `Genesis.Echos.Main/Components/Pages/`にRazorファイル作成
2. 必要に応じて分離コードビハインド（.razor.cs）作成
3. `@page`ディレクティブでルート指定
4. 認証が必要な場合`[Authorize]`属性追加

### 新しいエンティティの追加
1. `Genesis.Echos.Domain/Entities/`にエンティティクラス作成
2. `ApplicationDbContext.cs`にDbSet追加
3. リレーション設定（`OnModelCreating`）
4. マイグレーション作成・適用

---

## 参考情報

### ドキュメント
- [PROJECT_ARCHITECTURE.md](PROJECT_ARCHITECTURE.md) - プロジェクト全体構造
- [IMPLEMENTATION.md](IMPLEMENTATION.md) - 実装ログ
- [TROUBLESHOOTING.md](TROUBLESHOOTING.md) - トラブルシューティング
- [README.md](README.md) - プロジェクト概要

### 重要なパス
- ソリューション: `/Users/user/src/projects/Genesis/Genesis.Tools/Genesis.Echos/Genesis.Echos.sln`
- Main: `Genesis.Echos.Main/`
- Domain: `Genesis.Echos.Domain/`
- Infrastructure: `Genesis.Echos.Infrastructure/`

### 起動URL
- HTTP: http://localhost:5069
