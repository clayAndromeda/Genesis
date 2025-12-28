# Genesis.Echos トラブルシューティングガイド

このドキュメントでは、Genesis.Echosの開発・運用中に遭遇する可能性のあるエラーと解決策をまとめています。

---

## SQL Serverコンテナの管理

### コンテナ起動確認
```bash
docker ps | grep genesis-sqlserver
```

### コンテナ停止
```bash
docker stop genesis-sqlserver
```

### コンテナ再起動
```bash
docker start genesis-sqlserver
```

### コンテナ削除
```bash
docker stop genesis-sqlserver
docker rm genesis-sqlserver
```

### コンテナの再作成
```bash
docker run -e "ACCEPT_EULA=Y" \
  -e "MSSQL_SA_PASSWORD=YourStrong@Passw0rd" \
  -p 1433:1433 \
  --name genesis-sqlserver \
  -d mcr.microsoft.com/mssql/server:2022-latest
```

---

## データベースのリセット

### データベース削除
```bash
dotnet ef database drop \
  --project Genesis.Echos.Infrastructure \
  --startup-project Genesis.Echos.Main
```

### マイグレーション再適用
```bash
dotnet ef database update \
  --project Genesis.Echos.Infrastructure \
  --startup-project Genesis.Echos.Main
```

### 新しいマイグレーションの作成
```bash
dotnet ef migrations add MigrationName \
  --project Genesis.Echos.Infrastructure \
  --startup-project Genesis.Echos.Main
```

---

## Blazor認証関連のエラー

### エラー1: Authorization requires a cascading parameter

**エラーメッセージ**:
```
System.InvalidOperationException: Authorization requires a cascading parameter of type Task<AuthenticationState>
```

**症状**:
- `AuthorizeView`や`[Authorize]`を使用した時にエラーが発生

**原因**:
- `Routes.razor`に`<CascadingAuthenticationState>`が設定されていない

**解決策**:
`Routes.razor`に`<CascadingAuthenticationState>`を追加

```razor
<CascadingAuthenticationState>
    <Router AppAssembly="@typeof(Program).Assembly">
        <Found Context="routeData">
            <AuthorizeRouteView RouteData="@routeData" DefaultLayout="@typeof(Layout.MainLayout)">
                <NotAuthorized>
                    <p>認証が必要です。<a href="/Account/Login">ログイン</a>してください。</p>
                </NotAuthorized>
            </AuthorizeRouteView>
        </Found>
    </Router>
</CascadingAuthenticationState>
```

---

### エラー2: Cannot pass the parameter 'Body'

**エラーメッセージ**:
```
System.InvalidOperationException: Cannot pass the parameter 'Body' to component 'MainLayout' with rendermode 'InteractiveServerRenderMode'
```

**症状**:
- レイアウトコンポーネントにrendermode設定時にエラーが発生

**原因**:
- MainLayout等のレイアウトコンポーネントに`@rendermode InteractiveServer`を設定している

**解決策**:
- レイアウトコンポーネント（MainLayout.razor、NavMenu.razor）は常に静的レンダリング
- `@rendermode`ディレクティブを削除

**理由**:
- レイアウトコンポーネントは`Body`パラメータを受け取る特別なコンポーネント
- InteractiveServerモードでは`Body`パラメータを正しく渡せない

---

### エラー3: Headers are read-only, response has already started

**エラーメッセージ**:
```
System.InvalidOperationException: Headers are read-only, response has already started.
```

**症状**:
- ログインボタンを押すと「Headers are read-only」エラーが発生

**原因**:
- 認証ページ（Login.razor/Logout.razor）に`@rendermode InteractiveServer`を設定している
- SignInManager/SignOutAsyncがHTTPクッキーを設定できない

**詳細説明**:
- InteractiveServerモードはSignalRでストリーミングレスポンスを開始
- レスポンス開始後はHTTPヘッダー（クッキー含む）を変更できない
- ASP.NET Core Identityは認証クッキーをヘッダーに設定する必要がある

**解決策**:
1. Login.razorとLogout.razorから`@rendermode InteractiveServer`を削除
2. `Navigation.NavigateTo`に`forceLoad: true`を追加して、認証後にページ全体を再読み込み

```csharp
// ログイン成功後
Navigation.NavigateTo("/posts", forceLoad: true);

// ログアウト後
Navigation.NavigateTo("/", forceLoad: true);
```

---

### エラー4: フォームバリデーションが動作しない

**エラーメッセージ**:
- 「メールアドレスは必須です」等のバリデーションエラーが誤って表示される

**症状**:
- 入力しているのに「〜は必須です」エラーが表示される
- フォーム送信時にデータがバインドされない

**原因**:
- .NET 8以降では、静的サーバーレンダリングモードでフォームデータをバインドするために`[SupplyParameterFromForm]`属性が必要

**解決策**:
```csharp
[SupplyParameterFromForm]
private LoginModel? model { get; set; }

protected override void OnInitialized()
{
    model ??= new();
}
```

**重要**:
- `OnInitialized()`または`OnInitializedAsync()`でmodelを初期化する必要がある
- 初期化しないとNullReferenceExceptionが発生する

---

### エラー5: The POST request does not specify which form is being submitted

**エラーメッセージ**:
```
The POST request does not specify which form is being submitted. Consider specifying a value for the 'name' attribute.
```

**原因**:
- .NET 8以降では、1ページに複数のフォームがある可能性を考慮して`FormName`が必須

**解決策**:
全`EditForm`に`FormName`属性を追加

```razor
<EditForm Model="@model" OnValidSubmit="HandleSubmit" FormName="CreatePostForm">
    <!-- フォーム内容 -->
</EditForm>
```

**FormName命名規則**:
- 各ページで一意の名前を使用
- 例: `LoginForm`, `RegisterForm`, `CreatePostForm`, `EditPostForm`

---

## レンダリングモード選択のガイドライン

正しいレンダリングモードを選択することで、上記のエラーの多くを回避できます。

| コンポーネントタイプ | 推奨レンダリングモード | 理由 |
|-------------------|-------------------|------|
| レイアウト（MainLayout, NavMenu） | 静的（rendermodeなし） | Bodyパラメータを受け取るため |
| 認証ページ（Login, Logout, Register） | 静的（rendermodeなし） | HTTPクッキー操作が必要なため |
| 表示のみのページ（Home） | 静的（rendermodeなし） | インタラクティブ機能が不要 |
| インタラクティブな機能（LikeButton） | InteractiveServer | ボタンクリック等のイベント処理 |
| リアルタイム更新が必要なページ | InteractiveServer | SignalR経由でサーバーと通信 |

### 静的サーバーレンダリング（デフォルト）

**使用すべき場所**:
- レイアウトコンポーネント（MainLayout.razor、NavMenu.razor）
- 認証ページ（Login.razor、Logout.razor、Register.razor）
- シンプルな表示ページ（Home.razor）

**特徴**:
- サーバーで1回レンダリングして完全なHTMLを返す
- JavaScriptなしで動作
- HTTPリクエスト/レスポンスの標準的な動作
- SignInManager等のHTTPクッキー操作と互換性あり

### InteractiveServerレンダリング

**使用すべき場所**:
- インタラクティブな機能が必要なコンポーネント（LikeButton.razor）
- リアルタイム更新が必要なページ（投稿一覧、投稿詳細）

**特徴**:
- SignalR経由でサーバーとリアルタイム通信
- ストリーミングレスポンス
- ボタンクリック等のイベントハンドリングが可能
- HTTPヘッダーは変更不可（レスポンス開始後）

**使い方**:
```razor
@rendermode InteractiveServer

<button @onclick="HandleClick">クリック</button>

@code {
    private void HandleClick()
    {
        // イベント処理
    }
}
```

---

## Entity Framework Core関連のエラー

### マイグレーションが適用されない

**症状**:
- アプリケーション起動時にテーブルが存在しないエラー

**解決策**:
```bash
# マイグレーション状態を確認
dotnet ef migrations list \
  --project Genesis.Echos.Infrastructure \
  --startup-project Genesis.Echos.Main

# マイグレーションを適用
dotnet ef database update \
  --project Genesis.Echos.Infrastructure \
  --startup-project Genesis.Echos.Main
```

### 循環参照エラー

**症状**:
- JSON シリアライズ時に循環参照エラーが発生

**原因**:
- ナビゲーションプロパティの双方向参照

**解決策**:
- 必要なデータのみを`.Include()`で読み込む
- DTOを使用してデータ転送
- JSON シリアライザ設定で循環参照を無視

```csharp
// Program.cs
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });
```

---

## Docker関連のエラー

### SQL Serverコンテナが起動しない

**症状**:
- `docker ps`でコンテナが表示されない

**原因**:
- メモリ不足
- ポート1433が既に使用されている

**解決策**:
```bash
# ポート使用状況を確認
lsof -i :1433

# コンテナログを確認
docker logs genesis-sqlserver

# メモリ設定を確認（Docker Desktopの設定）
# 推奨: 4GB以上
```

### コンテナが勝手に停止する

**解決策**:
```bash
# コンテナのログを確認
docker logs genesis-sqlserver

# コンテナを再起動ポリシー付きで作成
docker run -e "ACCEPT_EULA=Y" \
  -e "MSSQL_SA_PASSWORD=YourStrong@Passw0rd" \
  -p 1433:1433 \
  --name genesis-sqlserver \
  --restart unless-stopped \
  -d mcr.microsoft.com/mssql/server:2022-latest
```

---

## パフォーマンス関連

### ページ読み込みが遅い

**原因**:
- N+1問題（関連データの遅延読み込み）

**解決策**:
```csharp
// 悪い例（N+1問題）
var posts = await context.Posts.ToListAsync();
foreach (var post in posts)
{
    var author = post.Author; // 各投稿ごとにクエリ実行
}

// 良い例（Eager Loading）
var posts = await context.Posts
    .Include(p => p.Author)
    .Include(p => p.Likes)
    .Include(p => p.PostTags)
        .ThenInclude(pt => pt.Tag)
    .ToListAsync();
```

---

## 開発環境のトラブル

### ビルドエラー: SDK not found

**解決策**:
```bash
# .NET SDKのバージョン確認
dotnet --version

# .NET 10.0がインストールされていることを確認
dotnet --list-sdks
```

### Hot Reloadが動作しない

**解決策**:
```bash
# アプリケーションを停止して再起動
# または
dotnet watch run --project Genesis.Echos.Main
```

---

## 認証情報

開発環境でのデフォルト認証情報:

### テストアカウント（初期シードデータ削除後は自分で作成）
- アカウント登録: `/Account/Register`
- 全ユーザーに自動的にMemberロールが付与される

### SQL Server
- ホスト: localhost
- ポート: 1433
- ユーザー名: sa
- パスワード: YourStrong@Passw0rd

---

## 参考リンク

- [ASP.NET Core Identity](https://learn.microsoft.com/aspnet/core/security/authentication/identity)
- [Entity Framework Core](https://learn.microsoft.com/ef/core/)
- [Blazor](https://learn.microsoft.com/aspnet/core/blazor/)
- [Blazor Rendering Modes](https://learn.microsoft.com/aspnet/core/blazor/components/render-modes)
- [Docker SQL Server](https://learn.microsoft.com/sql/linux/quickstart-install-connect-docker)
