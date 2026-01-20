# Localssation開発メモ

## これから開発を始める人へ

NumAniCloudより：
筆者はあなたの行動力を尊敬します。困ったときにドキュメントが役に立てば幸いです。Moddingは危険と不確実さが伴い、セーブデータのバックアップがとても重要であることを再確認し、始めましょう。

### プロジェクト設定

`Source/Localyssation.csproj` を編集し、あなたの環境に合った設定にする必要がある。さもないと、ビルドに必要なライブラリへの参照が足りない。

|環境変数名|意味|
|-|-|
|$(ATLYSS)|`ATLYSS.exe` があるATLYSSゲームフォルダ|
|$(ATLYSS_BEPINEX)|BepInExに含まれるdllがあるフォルダ|
|$(ATLYSS_BEPINEX_PLUGINS)|BepInExがロードするプラグインを配置するフォルダ|

もしかすると、EasySettingsのインストール場所も編集する必要があるかもしれない（そうならないよう準備しておいてほしい）：
```xml
<!-- Mod assemblies. -->
<ItemGroup>
    <Reference Include="EasySettings">
        <!-- ↓ここに EasySettings.dll へのパスを書く -->
        <HintPath>$(ATLYSS_BEPINEX_PLUGINS)\EasySettings.dll</HintPath>
        <Private>False</Private>
    </Reference>
</ItemGroup>
```

### リリースビルドの性質

設定により、DebugビルドとReleaseビルドとでは性質が異なる。Releaseビルドでは：

- 最適化されたdllが作成される。
- `Localyssation.LogDebug(object)` はログを出力しない。ユーザー配布に適する。
    - `Localyssation.logger.LogDebug(object)` ではログを出力するので注意。
- ビルド時に配布用のzipを作成する。`dist` フォルダに作られる。

## BepInEx について

BepInExはUnityゲームのアセンブリを変更することでMODの開発を可能にするライブラリ。
本体ゲームの特定のメソッドが呼ばれる直前・直後に呼ばれるメソッドを定義する事ができる。
あるいは、ILを直接読み取り、置換することで実行される処理を書き換える事ができる。

## TIPS

必要があれば、実行時のスタックの状態を以下のように書き表すことにする。
この例はスタックのトップに "hoge" があり、その下に向かって 0, 9, go(GameObject) と続くことを意味する。

```
GameObject go
int 9
int 0
string "hoge" [top]
```

あるいは、 `GameObject go; int 9; int 0; string "hoge" [top]`

### 🐈‍⬛スタックトップの文字列を翻訳する

以下のような本体ゲームのILを考える。

```
ldstr "fix me modder"
call void GameManager::ConsumeString(string)
```

この例では `GameManager.ConsumeString` に "fix me modder" という文字列を渡して処理をしているようだ。
Localyssation は翻訳補助MODであるため、 `ConsumeString` に実際に渡される文字列を置き換えたいケースが想像に難くない。
こういう場合は、以下のような HarmonyTranspiler を書けば良い。

```csharp
[HarmonyTranspiler]
public static IEnumerable<CodeInstruction> MyTranspiler(IEnumerable<CodeInstruction> instructions)
{
    return new CodeMatcher(instructions)
        .MatchForward(true,
            new CodeMatch(OpCodes.LdStr, "fix me modder"))
        .Advance(1)
        .InsertAndAdvance(
            Transpilers.EmitDelegate<Func<string, string>>(original =>
            {
                retrun "translated string";
            })
        )
        .InstructionEnumeration();
}
```

こうすると、パッチ後のILは以下のようになる：

```
ldstr "fix me modder"
call string TranspilerClass::<MyTranspiler>.g_EmittedMethod(string)  // 名前はいろいろ仮
call void GameManager::ConsumeString(string)
```

このILでは、"fix me modder" がスタックに乗った後、デリゲートがすぐにそれを消費し、新しく "translated string" をスタックに乗せる。
`GameManager.ConsumeString` はスタックトップが置き換わったことに気づかず、変換された値を通常通りに使用することになり、翻訳MODとしての役割は果たされる。

### 🐈‍⬛EmitDelegateとcall命令

dotnet ILでのメソッド呼び出しである `call` 命令では、評価スタックのトップから順に、メソッドの最後の引数から逆順に対応する。

つまり、 `LdStr` 命令がマッチした直後に以下のInstructionsを挿入すると、引数はその次の表のように並ぶ。

```csharp
var instructions = [
    new CodeInstruction(OpCodes.Ldarg_0),  // ldarg.0 とは this のこと(Weapon型と仮定)
    new CodeInstruction(OpCodes.Ldarg_1),   // int型と仮定
    Transpiler.EmitDelegate<Func<string, Weapon, int, string>>(
        (original, weapon, num) => $"{weapon.Name} x{num}")
]
```

|delegate|arg0|arg1|arg2|return|
|-|-|-|-|-|
||topの2つ前|topの1つ前|top|スタックに積み直される|

### 🐈‍⬛CodeMatcherのプログラムカウンタ

Harmonyの `CodeMatcher` は内部的にプログラムカウンタを持っており、ILの特定の位置を指している。
これを理解すれば、`InsertAndAdvance` などを呼んだときの命令の挿入位置を制御できる。

以下は `MatchForward` の呼び出し方の一例である：

```csharp
var m = new CodeMatcher(instructions)
    .MatchForward(true,
        new CodeMatch(OpCodes.Ldfld),
        new CodeMatch(OpCodes.Stloc_0)
    );
// この時点での m のプログラムカウンタの位置に興味がある

// ↓このCodeInstructionsはどこに挿入されるか？
m.InsertAndAdvance(new CodeInstruction(OpCodes.LdStr, "hoge"));
```

ここで `MatchForward` が成功した場合、`m` のプログラムカウンタは `Stloc_0` の位置を指す。
即ち、 `ldstr "hoge"` が挿入されるのは `Stloc_0` の直前ということになる。

そういうわけで、覚えておくべきことは以下の通りである。

- `MatchForward(bool useEnd, ...)` においての `useEnd` フラグに応じて、マッチ後のカウンタの位置が異なる
    - `true` の場合、最後の要素の直前を指す
    - `false` の場合、最初の要素の直前を指す
- `useEnd=true` であっても、最後の要素の直後までカウンタを進めることはできない。`CodeMatcher.Advance(1)` を追加で呼び出す必要がある。