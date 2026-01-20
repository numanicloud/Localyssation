# Dev 笔记

开发者笔记文档。每次更新一个二级标题，每个开发者占一个三级标题。

## 072025.a4-0.1.0

### TowardtheStars

- Transpiler 部分的翻译变化很大，建议整个重写为用 post 模式修改的

## 112025.a1

### NumAniCloud

ブランチ戦略。
- 機能開発の際は feature ブランチを作成し、機能が完成したら develop と main にマージする。
    - feature ブランチを作成する際、 main からではなく develop から派生する。
- `README.md` などの独自の変更をするときは main ブランチにコミットする。
- origin へプルリクエストを出す場合は develop ブランチを提出する。