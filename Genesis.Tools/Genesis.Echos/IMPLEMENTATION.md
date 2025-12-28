# Genesis.Echos 実装ログ

最終更新: 2025-12-29

## プロジェクト概要

ゲーム開発チーム向け掲示板システム「Genesis.Echos」
- **技術スタック**: ASP.NET Core 10.0 + Blazor Server + SQL Server + ASP.NET Core Identity
- **アーキテクチャ**: 3層（Presentation/Service/Infrastructure）+ Domain層
- **認証**: ASP.NET Core Identity（Member/Leader/Admin ロール）

---

## 実装完了フェーズ

### Phase 1: 基盤構築 ✅

**実施内容:**
- プロジェクト構造作成（Main/Domain/Infrastructure/Tests）
- NuGetパッケージ導入（EF Core, Identity, xUnit）
- ドメインモデル定義（User, Post, Tag, Like, Comment, PostTag）
- Enum定義（UserRole, ImportanceLevel）
- ApplicationDbContext作成（リレーション設定・制約定義）
- DbInitializer作成（ロール・タグのシードデータ）
- マイグレーション実行
- Docker SQL Server起動

**成果:**
- データベーススキーマ確立
- Identity統合完了
- シードデータ自動投入

---

### Phase 2: 投稿CRUD機能 ✅

**実施内容:**
- PostService作成（GetAll/GetById/Create/Update/Delete）
- 投稿一覧ページ（Index.razor）
- 投稿詳細ページ（Detail.razor）
- 投稿作成ページ（Create.razor）
- 投稿編集ページ（Edit.razor）
- バリデーション実装（Title: 200文字, Content: 5000文字）
- 作成者チェック（編集・削除時）
- Bootstrap 5ベースのUI

**成果:**
- 基本的な投稿管理機能完成
- CRUD操作の完全実装

---

### Phase 3: いいね機能＋認証 ✅

**実施内容:**
- PostServiceにいいね機能追加（ToggleLikeAsync, HasUserLikedAsync）
- LikeButtonコンポーネント作成（InteractiveServer）
- ログインページ作成（Login.razor）
- ログアウトページ作成（Logout.razor）
- Routes.razor更新（CascadingAuthenticationState, AuthorizeRouteView）
- MainLayoutにログイン状態表示
- FormName追加（EditForm）

**成果:**
- いいね機能動作確認
- 認証フロー確立
- Blazor認証パターン理解

**重要な学び:**
- 認証ページは`@rendermode`なしで実装（IdentityがHTTPクッキー使用のため）
- InteractiveServerコンポーネントは認証状態のカスケード受け取り不可
- レイアウトには`@rendermode`指定不可

---

### Phase 4: タグ機能 ✅

**実施内容:**
- TagService作成（GetAll/GetById）
- PostServiceのタグ対応拡張（Include, CreatePostAsync/UpdatePostAsyncにtagIds追加）
- TagBadgeコンポーネント作成
- 投稿作成・編集ページにタグ選択UI追加
- 投稿一覧・詳細にタグ表示追加

**成果:**
- タグ選択・表示機能完成
- 投稿とタグの多対多リレーション実装

---

### Phase 5: アカウント作成＋認証UI改善 ✅

**実施内容:**
- Register.razor/Register.razor.cs作成
- 全ユーザーにMemberロール自動付与
- 登録後の自動ログイン
- MainLayout.razorにログイン・登録ボタン追加
- NavMenuからログイン・登録リンク削除
- DbInitializerのテストユーザー作成削除

**成果:**
- ユーザー登録機能完成
- 認証UIの改善

---

### Phase 6: 管理者機能 ✅

**実施内容:**
- UserRoleにAdmin追加（Member=0, Leader=1, Admin=2）
- AdminService作成（GetAllUsers/ChangeUserRole/DeleteUser/DeletePost）
- PostServiceにDeletePostAsAdminAsync追加
- Users.razor/Users.razor.cs作成（ユーザー管理画面）
- Posts.razor/Posts.razor.cs作成（投稿管理画面）
- AdminButton.razorコンポーネント作成（InteractiveServer）
- MainLayout.razorに管理画面ボタン追加
- appsettings.jsonにAdminSettings追加
- DbInitializerに管理者自動昇格処理追加
- Login.razor.csにCheckAndPromoteToAdmin追加

**成果:**
- 管理者機能完成
- ユーザーロール変更・削除
- 投稿削除（管理者権限）
- 設定ファイルベースの管理者管理

**技術的課題と解決:**

1. **管理画面ボタンが動作しない**
   - 原因: MainLayoutに`@rendermode`指定不可
   - 解決: AdminButtonコンポーネントを分離し、そこに`@rendermode InteractiveServer`指定

2. **AdminButtonで認証状態エラー**
   - 原因: InteractiveServerコンポーネントが新しいサーキットを作成し、親から認証状態を受け取れない
   - 解決: 親（MainLayout）で`AuthorizeView Roles="Admin"`チェック→子（AdminButton）で機能実装

3. **管理画面でナビゲーションエラー**
   - 原因: `<a href>`リンクがInteractiveServerモードで新しいサーキット作成
   - 解決: NavigationManagerを使用した`@onclick`ナビゲーションに変更

4. **投稿タイトルリンクでエラー**
   - 原因: 同上
   - 解決: リンクを削除し、通常テキスト表示に変更

---

## 現在の機能一覧

### 認証・認可
- ユーザー登録（自動的にMemberロール付与）
- ログイン/ログアウト
- パスワード要件（8文字以上、大小英字・数字・記号必須）
- ロールベース認可（Member/Leader/Admin）

### 投稿機能
- 投稿一覧表示
- 投稿作成（タグ選択可）
- 投稿詳細表示
- 投稿編集（作成者のみ）
- 投稿削除（作成者 or Admin）
- いいね機能（トグル）

### タグ機能
- タグ選択（投稿作成・編集時）
- タグ表示（投稿一覧・詳細）
- デフォルトタグ5種類

### 管理者機能（Admin専用）
- ユーザー一覧表示
- ユーザーロール変更（Leader ⇄ Member）
- ユーザー削除（Adminは削除不可）
- 投稿一覧表示
- 投稿削除
- appsettings.jsonで管理者メール設定
- ログイン時の自動Admin昇格

---

## データベース

### スキーマ
- **AspNetUsers** (+ Role, CreatedAt)
- **AspNetRoles**
- **AspNetUserRoles**
- Posts
- Tags
- PostTags（複合主キー: PostId, TagId）
- Likes（一意制約: PostId, UserId）
- Comments

### シードデータ
- ロール: Admin, Leader, Member
- タグ: アイデア/バグ報告/改善提案/質問/その他

---

## 設定ファイル

### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=GenesisEchos;..."
  },
  "AdminSettings": {
    "AdminEmails": [
      "admin@example.com"
    ]
  }
}
```

---

## 技術的な学び

### Blazor認証パターン
1. **認証ページ**: `@rendermode`なし（静的サーバーレンダリング）
2. **インタラクティブコンポーネント**: `@rendermode InteractiveServer`
3. **レイアウト**: `@rendermode`指定不可→インタラクティブ部分を子コンポーネントに分離

### 認証状態のカスケード
- `Routes.razor`で`<CascadingAuthenticationState>`使用
- `AuthorizeRouteView`で`[Authorize]`属性サポート
- InteractiveServerコンポーネントは新しいサーキット作成→親から認証状態受け取れない
- 解決策: 親で`AuthorizeView`チェック、子で機能実装

### ナビゲーション
- InteractiveServerページでは`<a href>`でなくNavigationManagerを使用
- `@onclick`イベントで`NavigationManager.NavigateTo()`呼び出し

---

## 今後の拡張候補

### Leader専用機能（Phase 7候補）
- 既読管理（IsReadフラグUI）
- 重要度設定（ImportanceLevelドロップダウン）
- コメント機能のUI実装
- Leaderダッシュボード

### 一般機能
- 検索機能
- ページネーション
- ソート機能
- ユーザープロフィール編集
- 通知機能
- 添付ファイル
- Markdown対応

---

## トラブルシューティング

詳細は [TROUBLESHOOTING.md](TROUBLESHOOTING.md) を参照

---

## 開発環境

- .NET 10.0
- macOS (Apple Silicon)
- Docker Desktop (SQL Server 2022)
- 起動URL: http://localhost:5069
