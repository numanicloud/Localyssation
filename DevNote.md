# Dev 笔记

开发者笔记文档。每次更新一个二级标题，每个开发者占一个三级标题。

## 072025.a4-0.1.0

### TowardtheStars

- Transpiler 部分的翻译变化很大，建议整个重写为用 post 模式修改的

## 112025.a1

### NumAniCloud

ブランチ戦略。
- 機能開発の際はfeatureブランチを作成し、機能が完成したらmainにマージする。
- originへプルリクエストを出す場合はfeatureブランチを提出する。mainでは `README.md` や `CHANGELOG.md` を独自に更新したいため、mainを提出しないこと。
- originへマージされたfeatureブランチは削除する。それまでは削除しない。