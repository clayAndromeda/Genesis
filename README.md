# Genesis

Unityでゲーム開発を円滑に進めるための基盤詰め合わせ

## Goals

- Unityで新たにゲームを作る時、最初に参考にしたくなるような機能を詰め込む
- 野心的な新機能ではなく、地に足のついたゲーム開発の現場で役に立つような機能をたくさん作る
- 可能な限り、あらゆるものをC#で実装する。Unity/C#が書けるのなら、誰だって使えるものを作る。C#大統一理論。
- 省メモリ、低負荷。常にパフォーマンスを意識した実装をする。

## プロジェクト構成

### Genesis.Client

Unityプロジェクト

### Genesis.Server

MagicOnionを使った、Unityが利用すRPCサーバー

### Genesis.Shared

Genesis.ClientとGenesis.Sharedで共有する型や実装を置くポロジェクト

### Genesis.Tools

Genesisプロジェクトで実装した、Client / Unityに関連しないツール類。

### Genesis.Docs

ドキュメント置き場
