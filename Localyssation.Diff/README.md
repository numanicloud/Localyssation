# Localyssation.Diff

Localyssation用の翻訳ファイルどうしの差分を表示するツールです。

![](assets/Image.png)

## ATLYSSの新バージョンが来たときは

Localyssationには、ATLYSSの英語テキストを `defaultLanguage` に出力する機能を持ちます。ATLYSSの新バージョンが来る前に、現在のバージョンの言語ファイルを控えておきましょう。

その後、新バージョンのATLYSSを起動して新しい言語ファイルを入手したら、以下のように `Localyssation.Diff` を実行します。

```
Localyssation.Diff.exe <古い言語ファイル> <新しい言語ファイル>
```

そうしたら、変更・追加・削除された翻訳キーの一覧が出力されます。これを翻訳作業の出発点にするとよいでしょう。