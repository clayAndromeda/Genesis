# Genesis.Echos 実装ログ

## 実装日: 2025年12月28日

## プロジェクト概要

ゲーム開発チーム向けの掲示板システム「Genesis.Echos」の実装。
ASP.NET Core + Blazor Server + SQL Server + ASP.NET Core Identity を使用。

---

## フェーズ1: 基盤構築（完了✅）

### 1. プロジェクト構成

#### ソリューション構造
```
Genesis.Echos/
├── Genesis.Echos.sln
├── Genesis.Echos.Main/          # Blazor Server アプリケーション
├── Genesis.Echos.Domain/        # ドメインモデル・エンティティ
├── Genesis.Echos.Infrastructure/ # データアクセス・リポジトリ
└── Genesis.Echos.Tests/         # xUnit テストプロジェクト
```

### 2. NuGetパッケージ

#### Genesis.Echos.Domain
- Microsoft.Extensions.Identity.Stores (10.0.1)

#### Genesis.Echos.Infrastructure
- Microsoft.EntityFrameworkCore.SqlServer (10.0.1)
- Microsoft.EntityFrameworkCore.Tools (10.0.1)
- Microsoft.AspNetCore.Identity.EntityFrameworkCore (10.0.1)

#### Genesis.Echos.Main
- Microsoft.EntityFrameworkCore.Design (10.0.1)

#### Genesis.Echos.Tests
- Shouldly (4.3.0)

### 3. ドメインモデル

#### Enums (`Genesis.Echos.Domain/Enums/`)

**UserRole.cs**
```csharp
public enum UserRole
{
    Member = 0,
    Leader = 1
}
```

**ImportanceLevel.cs**
```csharp
public enum ImportanceLevel
{
    B = 0,  // Low importance
    A = 1,  // Medium importance
    S = 2   // High importance
}
```

#### Entities (`Genesis.Echos.Domain/Entities/`)

1. **ApplicationUser.cs** - ユーザー（IdentityUserを継承）
   - Role: UserRole
   - CreatedAt: DateTime
   - ナビゲーションプロパティ: Posts, Likes, Comments

2. **Post.cs** - 投稿
   - Title, Content, AuthorId
   - CreatedAt, UpdatedAt
   - IsRead, Importance (Leader専用)
   - ナビゲーションプロパティ: Author, PostTags, Likes, Comments

3. **Tag.cs** - タグ
   - Name, Color, CreatedAt
   - ナビゲーションプロパティ: PostTags

4. **PostTag.cs** - 投稿とタグの中間テーブル
   - PostId, TagId (複合主キー)
   - CreatedAt

5. **Like.cs** - いいね
   - PostId, UserId
   - CreatedAt
   - 一意制約: (PostId, UserId)

6. **Comment.cs** - コメント（Leader専用）
   - PostId, AuthorId, Content
   - CreatedAt, UpdatedAt

### 4. Infrastructure層

#### ApplicationDbContext (`Genesis.Echos.Infrastructure/Data/`)

**ApplicationDbContext.cs**
- IdentityDbContext<ApplicationUser>を継承
- DbSet: Posts, Tags, PostTags, Likes, Comments
- リレーション設定:
  - PostTagの複合キー設定
  - Likeの一意制約 (PostId, UserId)
  - カスケード削除の設定
  - 循環参照防止のためNoAction設定

**DbInitializer.cs**
- データベース初期化・シードデータ作成
- デフォルトロール: Leader, Member
- デフォルトユーザー:
  - leader@echos.com / Leader123! (Leader)
  - member@echos.com / Member123! (Member)
- デフォルトタグ:
  - アイデア (#0d6efd)
  - バグ報告 (#dc3545)
  - 改善提案 (#198754)
  - 質問 (#ffc107)
  - その他 (#6c757d)

### 5. アプリケーション設定

#### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=GenesisEchos;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

#### Program.cs の主要設定
- DbContext登録 (SQL Server)
- Identity設定
  - パスワード要件: 8文字以上、大小英字・数字・記号必須
  - ユニークEmail必須
- 認証・認可ミドルウェア
- Cookie設定 (LoginPath, LogoutPath, AccessDeniedPath)
- DbInitializer呼び出し（起動時）

### 6. データベース

#### Docker SQL Server
```bash
docker run -e "ACCEPT_EULA=Y" \
  -e "MSSQL_SA_PASSWORD=YourStrong@Passw0rd" \
  -p 1433:1433 \
  --name genesis-sqlserver \
  -d mcr.microsoft.com/mssql/server:2022-latest
```

#### マイグレーション
```bash
dotnet ef migrations add InitialCreate \
  --project Genesis.Echos.Infrastructure \
  --startup-project Genesis.Echos.Main

dotnet ef database update \
  --project Genesis.Echos.Infrastructure \
  --startup-project Genesis.Echos.Main
```

#### データベーススキーマ

**Identityテーブル**
- AspNetUsers (+ カスタムフィールド: Role, CreatedAt)
- AspNetRoles
- AspNetUserRoles
- AspNetUserClaims
- AspNetUserLogins
- AspNetUserTokens
- AspNetRoleClaims

**アプリケーションテーブル**
- Posts (投稿)
- Tags (タグ)
- PostTags (投稿-タグ 中間)
- Likes (いいね)
- Comments (コメント)

#### インデックス
- AspNetUsers: EmailIndex, UserNameIndex
- Posts: IX_Posts_AuthorId
- Likes: IX_Likes_PostId_UserId (一意), IX_Likes_UserId
- Comments: IX_Comments_PostId, IX_Comments_AuthorId
- PostTags: IX_PostTags_TagId

---

## 動作確認

### アプリケーション起動確認
```bash
dotnet run --project Genesis.Echos.Main/Genesis.Echos.Main.csproj
```

**起動URL**: http://localhost:5069

### シードデータ確認
- ✅ Leaderロール作成成功
- ✅ Memberロール作成成功
- ✅ leader@echos.com ユーザー作成成功
- ✅ member@echos.com ユーザー作成成功
- ✅ タグ5件作成成功

---

## フェーズ2: 投稿CRUD機能（完了✅）

### 1. PostService作成

**場所**: `Genesis.Echos.Main/Services/PostService.cs`

#### 実装メソッド
- `GetAllPostsAsync()` - 全投稿を取得（作成者、いいね数を含む）
- `GetPostByIdAsync(int id)` - ID指定で投稿を取得（作成者、いいね、コメントを含む）
- `CreatePostAsync(Post post)` - 新規投稿作成
- `UpdatePostAsync(Post post, string currentUserId)` - 投稿更新（作成者チェック付き）
- `DeletePostAsync(int id, string currentUserId)` - 投稿削除（作成者チェック付き）

#### 特徴
- Entity Frameworkの`Include`を使用して関連データを効率的に読み込み
- 作成者チェックによる認可
- ログ記録
- 例外ハンドリング

### 2. Blazorページ作成

#### 投稿一覧ページ (`Components/Pages/Posts/Index.razor`)
- URL: `/posts`
- カード形式で全投稿を表示
- 表示内容: タイトル、本文プレビュー、作成者、作成日時、いいね数
- AuthorizeViewで認証状態に応じた「新規投稿」ボタン表示
- 未認証ユーザーには「ログインして投稿」ボタン
- インタラクティブサーバーレンダリング

#### 投稿作成ページ (`Components/Pages/Posts/Create.razor`)
- URL: `/posts/create`
- `[Authorize]`属性で認証必須
- EditFormとDataAnnotationsValidatorでバリデーション
- タイトル（最大200文字）、本文（最大5000文字）
- 送信中の状態表示（スピナー）
- エラーハンドリングとメッセージ表示

#### 投稿詳細ページ (`Components/Pages/Posts/Detail.razor`)
- URL: `/posts/{id:int}`
- 投稿の全内容、作成者、作成日時、更新日時を表示
- 作成者のみ編集・削除ボタン表示
- コメント一覧表示
- JavaScriptの`confirm`で削除確認ダイアログ
- いいね数表示

#### 投稿編集ページ (`Components/Pages/Posts/Edit.razor`)
- URL: `/posts/{id:int}/edit`
- `[Authorize]`属性で認証必須
- 作成者チェック（作成者以外はアクセス不可）
- 既存データをプリフィル
- CreateページとpostMessage同様のバリデーション
- 更新確認メッセージ

### 3. ナビゲーション更新

**NavMenu.razor更新内容**:
- ブランド名を「Genesis.Echos.Main」から「Genesis.Echos」に変更
- Counter/Weatherサンプルページのリンクを削除
- 「投稿」リンク追加（/postsへ）
- AuthorizeViewで認証状態に応じた表示切り替え
  - 認証済み: ユーザー名表示
  - 未認証: ログインリンク表示

### 4. 共通設定

**_Imports.razor更新**:
- `Microsoft.AspNetCore.Components.Authorization`を追加
- AuthorizeView等の認証コンポーネントをグローバルに利用可能に

**Program.cs更新**:
- `PostService`をスコープドサービスとして登録

### 5. ビルド結果

```
ビルドに成功しました。
    0 個の警告
    0 エラー
```

---

## フェーズ3: いいね機能＋認証実装（完了✅）

### 1. PostServiceにいいね機能追加

**場所**: `Genesis.Echos.Main/Services/PostService.cs` (Lines 153-208)

#### 追加メソッド

**ToggleLikeAsync**
```csharp
public async Task<bool> ToggleLikeAsync(int postId, string userId)
{
    var existingLike = await _context.Likes
        .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);

    if (existingLike != null)
    {
        _context.Likes.Remove(existingLike);
        await _context.SaveChangesAsync();
        return false; // unlike
    }
    else
    {
        var like = new Like
        {
            PostId = postId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };
        _context.Likes.Add(like);
        await _context.SaveChangesAsync();
        return true; // liked
    }
}
```
- いいねの追加/削除をトグル
- 既存のいいねがあれば削除、なければ追加
- 戻り値: true（いいね追加）、false（いいね削除）
- ログ記録と例外ハンドリング

**HasUserLikedAsync**
```csharp
public async Task<bool> HasUserLikedAsync(int postId, string userId)
{
    return await _context.Likes
        .AnyAsync(l => l.PostId == postId && l.UserId == userId);
}
```
- ユーザーが投稿にいいねしているかチェック
- `AnyAsync`を使用して効率的に確認

### 2. LikeButtonコンポーネント作成

**場所**: `Genesis.Echos.Main/Components/Shared/LikeButton.razor`

#### 機能
- ハートアイコン表示
  - いいね済み: `bi-heart-fill`（塗りつぶし）+ 赤色ボタン
  - 未いいね: `bi-heart`（空）+ グレーボタン
- いいね数をリアルタイム表示
- クリックでいいねトグル
- 未認証ユーザーは無効化（disabled状態）
- **InteractiveServerレンダリング**（重要）

#### パラメータ
- `PostId`: 投稿ID
- `LikeCount`: いいね数
- `IsLiked`: いいね済みフラグ

#### 実装コード（抜粋）
```razor
@rendermode InteractiveServer

<button class="btn btn-sm @(IsLiked ? "btn-danger" : "btn-outline-secondary")"
        @onclick="HandleLikeToggle"
        disabled="@isDisabled">
    <i class="bi @(IsLiked ? "bi-heart-fill" : "bi-heart")"></i>
    <span class="ms-1">@LikeCount</span>
</button>

@code {
    [Parameter] public int PostId { get; set; }
    [Parameter] public int LikeCount { get; set; }
    [Parameter] public bool IsLiked { get; set; }

    private async Task HandleLikeToggle()
    {
        var wasLiked = await PostService.ToggleLikeAsync(PostId, currentUserId);
        IsLiked = wasLiked;
        LikeCount = wasLiked ? LikeCount + 1 : LikeCount - 1;
    }
}
```

### 3. 投稿ページへのLikeButton追加

#### 投稿一覧ページ (`Components/Pages/Posts/Index.razor`)
- 各投稿カードのフッターにLikeButton配置
- 作成者名の右側に表示
- `<LikeButton PostId="@post.Id" LikeCount="@post.Likes.Count" />`

#### 投稿詳細ページ (`Components/Pages/Posts/Detail.razor`)
- カードフッターにLikeButton配置
- 「一覧に戻る」ボタンの左側に表示
- `<LikeButton PostId="@post.Id" LikeCount="@post.Likes.Count" />`

### 4. _Imports.razor更新

**追加内容**:
```razor
@using Genesis.Echos.Main.Components.Shared
```
- LikeButtonコンポーネントをグローバルに利用可能に

### 5. 認証ページの作成と修正

#### ログインページ作成

**場所**: `Genesis.Echos.Main/Components/Pages/Account/Login.razor`

**重要なポイント**:
- **`@rendermode InteractiveServer`を使用しない**（静的サーバーレンダリング）
- ASP.NET Core IdentityのSignInManagerがHTTPクッキーを設定する必要があるため、ストリーミングレスポンス（InteractiveServer）と互換性がない

**主要実装**:
```razor
@page "/Account/Login"
@using Genesis.Echos.Domain.Entities
@using Microsoft.AspNetCore.Identity
@inject SignInManager<ApplicationUser> SignInManager
@inject UserManager<ApplicationUser> UserManager
@inject NavigationManager Navigation

<EditForm Model="@model" OnValidSubmit="HandleLogin" FormName="LoginForm">
    <DataAnnotationsValidator />
    <ValidationSummary class="text-danger" />

    <div class="mb-3">
        <label for="email" class="form-label">メールアドレス</label>
        <InputText id="email" class="form-control" @bind-Value="model.Email" />
        <ValidationMessage For="@(() => model.Email)" class="text-danger" />
    </div>

    <div class="mb-3">
        <label for="password" class="form-label">パスワード</label>
        <InputText type="password" id="password" class="form-control" @bind-Value="model.Password" />
        <ValidationMessage For="@(() => model.Password)" class="text-danger" />
    </div>

    <button type="submit" class="btn btn-primary">ログイン</button>
</EditForm>

@code {
    [SupplyParameterFromForm]
    private LoginModel? model { get; set; }

    [SupplyParameterFromQuery]
    public string? ReturnUrl { get; set; }

    protected override void OnInitialized()
    {
        model ??= new();
    }

    private async Task HandleLogin()
    {
        if (model == null) return;

        var user = await UserManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            errorMessage = "メールアドレスまたはパスワードが正しくありません。";
            return;
        }

        var result = await SignInManager.PasswordSignInAsync(
            user.UserName!,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: false);

        if (result.Succeeded)
        {
            Navigation.NavigateTo(ReturnUrl ?? "/posts", forceLoad: true);
        }
    }
}
```

**重要な設定**:
1. **`[SupplyParameterFromForm]`**: .NET 8以降でフォームデータをバインドするために必須
2. **`FormName="LoginForm"`**: EditFormに必須（.NET 8以降）
3. **`forceLoad: true`**: 認証後にページ全体を再読み込みして認証状態を反映
4. **`OnInitialized()`**: modelの初期化が必要

#### ログアウトページ作成

**場所**: `Genesis.Echos.Main/Components/Pages/Account/Logout.razor`

**実装**:
```razor
@page "/Account/Logout"
@using Genesis.Echos.Domain.Entities
@using Microsoft.AspNetCore.Identity
@inject SignInManager<ApplicationUser> SignInManager
@inject NavigationManager Navigation

@code {
    private bool isLoggingOut = true;

    protected override async Task OnInitializedAsync()
    {
        await SignInManager.SignOutAsync();
        isLoggingOut = false;

        await Task.Delay(2000);
        Navigation.NavigateTo("/", forceLoad: true);
    }
}
```

**重要**: ログインページと同様、`@rendermode InteractiveServer`を使用しない

### 6. Routes.razor の認証設定

**場所**: `Genesis.Echos.Main/Components/Routes.razor`

**修正内容**:
```razor
<CascadingAuthenticationState>
    <Router AppAssembly="@typeof(Program).Assembly">
        <Found Context="routeData">
            <AuthorizeRouteView RouteData="@routeData" DefaultLayout="@typeof(Layout.MainLayout)">
                <NotAuthorized>
                    <p>認証が必要です。<a href="/Account/Login">ログイン</a>してください。</p>
                </NotAuthorized>
            </AuthorizeRouteView>
            <FocusOnNavigate RouteData="@routeData" Selector="h1" />
        </Found>
        <NotFound>
            <PageTitle>Not found</PageTitle>
            <LayoutView Layout="@typeof(Layout.MainLayout)">
                <p>Sorry, there's nothing at this address.</p>
            </LayoutView>
        </NotFound>
    </Router>
</CascadingAuthenticationState>
```

**重要な変更点**:
1. **`<CascadingAuthenticationState>`**: 全コンポーネントに認証状態をカスケード
2. **`AuthorizeRouteView`**: `RouteView`から変更、認証をサポート
3. **`<NotAuthorized>`**: 認証が必要なページへの未認証アクセス時の表示

### 7. NavMenu.razor の簡素化

**場所**: `Genesis.Echos.Main/Components/Layout/NavMenu.razor`

**修正内容**:
- **`@rendermode InteractiveServer`を削除**
- **`AuthorizeView`を削除**（レイアウトコンポーネントでは認証状態表示を避ける）
- 静的なリンクのみ表示

**理由**: レイアウトコンポーネントに`@rendermode`を設定すると「Cannot pass the parameter 'Body'」エラーが発生

### 8. Home.razor と Index.razor の簡素化

**修正内容**:
- **`@rendermode InteractiveServer`を削除**
- **`AuthorizeView`を削除**
- 常に「新規投稿」ボタンと「ログイン」ボタンを表示

**理由**: ページレベルでInteractiveServerと認証を混在させると複雑化するため、認証が必要な操作は個別のページで制御

### 9. EditFormへのFormName追加

**場所**:
- `Genesis.Echos.Main/Components/Pages/Posts/Create.razor`
- `Genesis.Echos.Main/Components/Pages/Posts/Edit.razor`

**変更内容**:
```razor
<EditForm Model="@model" OnValidSubmit="HandleSubmit" FormName="CreatePostForm">
```

**.NET 8以降で必須**: FormNameを指定しないと「The POST request does not specify which form is being submitted」エラーが発生

### 10. ブラウザテスト結果

**動作確認**:
- ✅ ログインページ表示成功
- ✅ フォームバリデーション動作
- ✅ ログイン成功（leader@echos.com / Leader123!）
- ✅ 認証後に投稿一覧へリダイレクト
- ✅ いいねボタンのトグル動作
- ✅ いいね数のリアルタイム更新
- ✅ ログアウト動作

**アプリケーションURL**: http://localhost:5069

### 11. トラブルシューティング履歴

#### エラー1: Authorization requires cascading parameter
**エラーメッセージ**:
```
System.InvalidOperationException: Authorization requires a cascading parameter of type Task<AuthenticationState>
```

**原因**: `CascadingAuthenticationState`がRoutes.razorに設定されていない

**解決策**: Routes.razorに`<CascadingAuthenticationState>`を追加

---

#### エラー2: Cannot pass the parameter 'Body'
**エラーメッセージ**:
```
System.InvalidOperationException: Cannot pass the parameter 'Body' to component 'MainLayout' with rendermode 'InteractiveServerRenderMode'
```

**原因**: レイアウトコンポーネント（MainLayout.razor）に`@rendermode InteractiveServer`を設定

**解決策**: MainLayout.razorから`@rendermode`を削除（レイアウトコンポーネントは常に静的）

---

#### エラー3: Headers are read-only, response has already started
**エラーメッセージ**:
```
System.InvalidOperationException: Headers are read-only, response has already started.
```

**原因**: ログイン/ログアウトページに`@rendermode InteractiveServer`を設定していたため、SignInManager/SignOutAsyncがHTTPクッキーを設定できない

**詳細説明**:
- InteractiveServerモードはSignalRでストリーミングレスポンスを開始
- レスポンス開始後はHTTPヘッダー（クッキー含む）を変更できない
- ASP.NET Core Identityは認証クッキーをヘッダーに設定する必要がある

**解決策**:
- Login.razorとLogout.razorから`@rendermode InteractiveServer`を削除
- `forceLoad: true`をNavigation.NavigateToに追加して、認証後にページ全体を再読み込み

---

#### エラー4: メールアドレスは必須です（フォームバリデーション失敗）
**症状**: メールアドレスを入力しているのに「メールアドレスは必須です」エラーが表示

**原因**: .NET 8以降では、静的サーバーレンダリングモードでフォームデータをバインドするために`[SupplyParameterFromForm]`属性が必要

**解決策**:
```csharp
[SupplyParameterFromForm]
private LoginModel? model { get; set; }

protected override void OnInitialized()
{
    model ??= new();
}
```

---

#### エラー5: The POST request does not specify which form is being submitted
**原因**: .NET 8以降では、1ページに複数のフォームがある可能性を考慮してFormNameが必須

**解決策**: 全EditFormに`FormName`属性を追加
```razor
<EditForm Model="@model" OnValidSubmit="HandleLogin" FormName="LoginForm">
```

### 12. レンダリングモードの使い分け（重要）

#### 静的サーバーレンダリング（デフォルト）
**使用すべき場所**:
- レイアウトコンポーネント（MainLayout.razor、NavMenu.razor）
- 認証ページ（Login.razor、Logout.razor）
- シンプルな表示ページ（Home.razor）

**特徴**:
- サーバーで1回レンダリングして完全なHTMLを返す
- JavaScriptなしで動作
- HTTPリクエスト/レスポンスの標準的な動作
- SignInManager等のHTTPクッキー操作と互換性あり

#### InteractiveServerレンダリング
**使用すべき場所**:
- インタラクティブな機能が必要なコンポーネント（LikeButton.razor）
- リアルタイム更新が必要なページ（投稿一覧、詳細）

**特徴**:
- SignalR経由でサーバーとリアルタイム通信
- ストリーミングレスポンス
- ボタンクリック等のイベントハンドリングが可能
- HTTPヘッダーは変更不可（レスポンス開始後）

### 13. ビルド結果

```
ビルドに成功しました。
    0 個の警告
    0 エラー
```

---

## フェーズ4: タグ機能（完了✅）

### 1. TagService作成

**場所**: `Genesis.Echos.Main/Services/TagService.cs`

#### 実装メソッド
- `GetAllTagsAsync()` - 全タグを取得（名前順）
- `GetTagByIdAsync(int id)` - IDでタグを取得

#### 実装コード
```csharp
public class TagService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TagService> _logger;

    public async Task<List<Tag>> GetAllTagsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await context.Tags
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<Tag?> GetTagByIdAsync(int id)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await context.Tags
            .FirstOrDefaultAsync(t => t.Id == id);
    }
}
```

### 2. PostServiceのタグ対応拡張

**場所**: `Genesis.Echos.Main/Services/PostService.cs`

#### 変更内容

**GetAllPostsAsync - PostTagsとTagsをInclude**
```csharp
return await context.Posts
    .Include(p => p.Author)
    .Include(p => p.Likes)
    .Include(p => p.PostTags)      // 追加
        .ThenInclude(pt => pt.Tag) // 追加
    .OrderByDescending(p => p.CreatedAt)
    .ToListAsync();
```

**GetPostByIdAsync - PostTagsとTagsをInclude**
```csharp
return await context.Posts
    .Include(p => p.Author)
    .Include(p => p.Likes)
        .ThenInclude(l => l.User)
    .Include(p => p.Comments)
        .ThenInclude(c => c.Author)
    .Include(p => p.PostTags)      // 追加
        .ThenInclude(pt => pt.Tag) // 追加
    .FirstOrDefaultAsync(p => p.Id == id);
```

**CreatePostAsync - タグIDリスト対応**
```csharp
public async Task<Post> CreatePostAsync(Post post, List<int>? tagIds = null)
{
    post.CreatedAt = DateTime.UtcNow;
    context.Posts.Add(post);
    await context.SaveChangesAsync();

    // タグを追加
    if (tagIds != null && tagIds.Any())
    {
        foreach (var tagId in tagIds)
        {
            var postTag = new PostTag
            {
                PostId = post.Id,
                TagId = tagId,
                CreatedAt = DateTime.UtcNow
            };
            context.PostTags.Add(postTag);
        }
        await context.SaveChangesAsync();
    }

    return post;
}
```

**UpdatePostAsync - タグ更新処理追加**
```csharp
public async Task<bool> UpdatePostAsync(Post post, string currentUserId, List<int>? tagIds = null)
{
    // ... 既存の検証 ...

    existingPost.Title = post.Title;
    existingPost.Content = post.Content;
    existingPost.UpdatedAt = DateTime.UtcNow;

    // 既存のタグを削除
    var existingPostTags = await context.PostTags
        .Where(pt => pt.PostId == post.Id)
        .ToListAsync();
    context.PostTags.RemoveRange(existingPostTags);

    // 新しいタグを追加
    if (tagIds != null && tagIds.Any())
    {
        foreach (var tagId in tagIds)
        {
            var postTag = new PostTag
            {
                PostId = post.Id,
                TagId = tagId,
                CreatedAt = DateTime.UtcNow
            };
            context.PostTags.Add(postTag);
        }
    }

    await context.SaveChangesAsync();
    return true;
}
```

### 3. TagBadgeコンポーネント作成

**場所**: `Genesis.Echos.Main/Components/Shared/TagBadge.razor`

#### 実装内容
```razor
<span class="badge me-1" style="background-color: @Color; color: white;">
    @Name
</span>

@code {
    [Parameter]
    public string Name { get; set; } = string.Empty;

    [Parameter]
    public string Color { get; set; } = "#6c757d";
}
```

#### 特徴
- シンプルな再利用可能コンポーネント
- タグの色をインラインスタイルで適用
- Bootstrapの`badge`クラスを使用

### 4. 投稿作成・編集ページにタグ選択追加

#### 投稿作成ページ (`Components/Pages/Posts/Create.razor.cs`)

**追加コード**:
```csharp
[Inject] private TagService TagService { get; set; } = default!;
private List<Tag> availableTags = new();
private List<int> selectedTagIds = new();

protected override async Task OnInitializedAsync()
{
    availableTags = await TagService.GetAllTagsAsync();
}

private void ToggleTag(int tagId)
{
    if (selectedTagIds.Contains(tagId))
        selectedTagIds.Remove(tagId);
    else
        selectedTagIds.Add(tagId);
}

// HandleValidSubmit内
await PostService.CreatePostAsync(post, selectedTagIds);
```

**UI (`Components/Pages/Posts/Create.razor`)**:
```razor
<div class="mb-3">
    <label class="form-label">タグ</label>
    <div class="border rounded p-3">
        @foreach (var tag in availableTags)
        {
            <div class="form-check form-check-inline">
                <input class="form-check-input" type="checkbox" id="tag-@tag.Id"
                       checked="@selectedTagIds.Contains(tag.Id)"
                       @onchange="@(() => ToggleTag(tag.Id))" />
                <label class="form-check-label" for="tag-@tag.Id">
                    <TagBadge Name="@tag.Name" Color="@tag.Color" />
                </label>
            </div>
        }
    </div>
</div>
```

#### 投稿編集ページ (`Components/Pages/Posts/Edit.razor.cs`)

**追加コード**:
```csharp
[Inject] private TagService TagService { get; set; } = default!;
private List<Tag> availableTags = new();
private List<int> selectedTagIds = new();

protected override async Task OnInitializedAsync()
{
    availableTags = await TagService.GetAllTagsAsync();
    post = await PostService.GetPostByIdAsync(Id);

    if (post != null && currentUserId == post.AuthorId)
    {
        isAuthor = true;
        model.Title = post.Title;
        model.Content = post.Content;
        // 既存タグを選択状態にする
        selectedTagIds = post.PostTags?.Select(pt => pt.TagId).ToList() ?? new List<int>();
    }
}

// HandleValidSubmit内
var success = await PostService.UpdatePostAsync(post, currentUserId, selectedTagIds);
```

**UI**: Create.razorと同様のチェックボックスUI

### 5. 投稿一覧・詳細にタグ表示追加

#### 投稿一覧ページ (`Components/Pages/Posts/Index.razor`)

**追加コード**:
```razor
@if (post.PostTags != null && post.PostTags.Any())
{
    <div class="mb-2">
        @foreach (var postTag in post.PostTags)
        {
            <TagBadge Name="@postTag.Tag.Name" Color="@postTag.Tag.Color" />
        }
    </div>
}
```

#### 投稿詳細ページ (`Components/Pages/Posts/Detail.razor`)

**追加コード**:
```razor
@if (post.PostTags != null && post.PostTags.Any())
{
    <div class="mb-3">
        @foreach (var postTag in post.PostTags)
        {
            <TagBadge Name="@postTag.Tag.Name" Color="@postTag.Color" />
        }
    </div>
}
```

### 6. Program.cs更新

**追加内容**:
```csharp
builder.Services.AddScoped<TagService>();
```

### 7. ビルド結果

```
ビルドに成功しました。
    0 個の警告
    0 エラー

成功!   -失敗:     0、合格:     1、スキップ:     0、合計:     1
```

### 8. 機能概要

#### 実装された機能
- ✅ タグ一覧取得機能（TagService）
- ✅ 投稿作成時のタグ選択（複数選択可能）
- ✅ 投稿編集時のタグ更新（既存タグの事前選択）
- ✅ 投稿一覧でのタグ表示
- ✅ 投稿詳細でのタグ表示
- ✅ TagBadge再利用可能コンポーネント

#### タグ更新の仕組み
投稿編集時のタグ更新は「削除してから追加」パターンを採用:
1. 既存のPostTagレコードを全削除
2. 選択されたタグIDで新しいPostTagレコードを作成
3. シンプルで理解しやすい実装

---

## 次のステップ

### フェーズ5: アカウント作成機能（完了✅）

**実装内容**:

1. **Register.razor/Register.razor.cs作成**
   - アカウント新規登録ページ
   - 全ユーザーにMemberロールを自動付与
   - 登録後に自動ログイン

2. **DbInitializer更新**
   - テストアカウント作成処理を削除
   - ロールとタグの初期化のみ実施

3. **認証UI改善**
   - ログイン・登録ボタンをヘッダーに移動
   - ログイン状態に応じた表示切り替え（MainLayout.razor）
   - サイドバー（NavMenu）からログイン・登録リンクを削除
   - ホームページの認証状態対応

### フェーズ6: リーダー機能（将来）

**実装予定**:

1. **PostServiceにリーダー専用メソッド追加**
   - `MarkAsReadAsync(int postId)` - 既読マーク
   - `SetImportanceAsync(int postId, ImportanceLevel level)` - 重要度設定
   - `AddCommentAsync(Comment comment)` - コメント追加

2. **ImportanceBadgeコンポーネント作成**
   - S（赤）、A（黄）、B（灰色）のバッジ
   - 重要度に応じたアイコン表示

3. **投稿詳細にリーダーセクション追加**
   - `[Authorize(Roles = "Leader")]`で制御
   - 既読チェックボックス
   - 重要度ドロップダウン
   - コメント一覧・追加フォーム

4. **投稿一覧でのフィルタリング強化**
   - 既読/未読フィルター（Leader専用）
   - 重要度フィルター（Leader専用）

---

## 技術情報

### 開発環境
- .NET 10.0
- macOS (Apple Silicon)
- Docker Desktop for Mac
- SQL Server 2022 (Dockerコンテナ)

### ポート
- アプリケーション: 5069 (HTTP)
- SQL Server: 1433

### 認証情報
- Leader: leader@echos.com / Leader123!
- Member: member@echos.com / Member123!
- SQL Server SA: sa / YourStrong@Passw0rd

---

## トラブルシューティング

### SQL Serverコンテナの管理

**コンテナ起動確認**
```bash
docker ps | grep genesis-sqlserver
```

**コンテナ停止**
```bash
docker stop genesis-sqlserver
```

**コンテナ再起動**
```bash
docker start genesis-sqlserver
```

**コンテナ削除**
```bash
docker stop genesis-sqlserver
docker rm genesis-sqlserver
```

### データベースのリセット

```bash
# データベース削除
dotnet ef database drop --project Genesis.Echos.Infrastructure --startup-project Genesis.Echos.Main

# マイグレーション再適用
dotnet ef database update --project Genesis.Echos.Infrastructure --startup-project Genesis.Echos.Main
```

### Blazor認証関連のエラー

#### "Authorization requires a cascading parameter"エラー
**症状**: `AuthorizeView`や`[Authorize]`を使用した時にエラー

**解決策**: `Routes.razor`に`<CascadingAuthenticationState>`を追加
```razor
<CascadingAuthenticationState>
    <Router AppAssembly="@typeof(Program).Assembly">
        ...
    </Router>
</CascadingAuthenticationState>
```

#### "Headers are read-only"エラー（ログイン時）
**症状**: ログインボタンを押すと「Headers are read-only, response has already started」エラー

**原因**: 認証ページ（Login.razor/Logout.razor）に`@rendermode InteractiveServer`を設定

**解決策**:
1. Login.razorとLogout.razorから`@rendermode InteractiveServer`を削除
2. `Navigation.NavigateTo`に`forceLoad: true`を追加

#### フォームバリデーションが動作しない
**症状**: 入力しているのに「〜は必須です」エラーが表示される

**原因**: .NET 8以降では`[SupplyParameterFromForm]`属性が必要

**解決策**:
```csharp
[SupplyParameterFromForm]
private LoginModel? model { get; set; }

protected override void OnInitialized()
{
    model ??= new();
}
```

#### "Cannot pass the parameter 'Body'"エラー
**症状**: レイアウトコンポーネントにrendermode設定時にエラー

**原因**: MainLayout等のレイアウトコンポーネントに`@rendermode`を設定

**解決策**: レイアウトコンポーネントは常に静的レンダリング（rendermodeを削除）

#### "The POST request does not specify which form is being submitted"エラー
**原因**: .NET 8以降では`EditForm`に`FormName`が必須

**解決策**:
```razor
<EditForm Model="@model" OnValidSubmit="HandleSubmit" FormName="CreatePostForm">
```

### レンダリングモード選択のガイドライン

| コンポーネントタイプ | 推奨レンダリングモード | 理由 |
|-------------------|-------------------|------|
| レイアウト（MainLayout, NavMenu） | 静的（rendermodeなし） | Bodyパラメータを受け取るため |
| 認証ページ（Login, Logout） | 静的（rendermodeなし） | HTTPクッキー操作が必要なため |
| 表示のみのページ（Home） | 静的（rendermodeなし） | インタラクティブ機能が不要 |
| インタラクティブな機能 | InteractiveServer | ボタンクリック等のイベント処理 |
| リアルタイム更新が必要 | InteractiveServer | SignalR経由でサーバーと通信 |

---

## 参考リンク

- [ASP.NET Core Identity](https://learn.microsoft.com/aspnet/core/security/authentication/identity)
- [Entity Framework Core](https://learn.microsoft.com/ef/core/)
- [Blazor](https://learn.microsoft.com/aspnet/core/blazor/)
