# リファクタ候補メモ

更新日: 2026-09-22

## 優先方針

承認済みエフェクトの実戦確認・デプロイ後に、既存の呼び出し口と見た目を保ったまま責務を分ける。範囲・移動・パッシブ計算はUnity UIから独立させ、まず自動検証で挙動を固定する。

## 候補と順序

### 1. 戦闘ルールの回帰テスト

- 剣・槍・銃・竜の対象選択と効果範囲、動物の2マス移動、盾・鎧・壁、数字バフを自動確認できる盤面テストを用意する。
- 既存挙動を固定してから責務分割を始める。
- 完了: `BattleRuleRegression` で剣の扇形、槍の2マス、隣接範囲、動物移動、数字パッシブの主要式をバッチ実行できるようにした。

### 2. BattleManagerの責務分割

- 盤面生成、ターン進行、移動、対象選択、戦闘ログ、VFX、勝敗処理が集中している。
- BattleManagerを既存の呼び出し口として保ちつつ、まずVFX管理、次に移動・盤面操作、ターン進行を独立させる。
- 一度に全面書き換えず、各抽出ごとにWebGL確認する。
- 完了（第1段階）: 一時VFXの登録・破棄・一括クリーンアップを `BattleVfxRegistry` に分離。範囲と移動ルールも `BattleManager` 外へ移した。

### 3. スキル実行経路の整理

- SkillExecutorにはJSON駆動、旧来分岐、専用処理が併存している。
- 成功/失敗と実際に発生した効果を結果データとして返し、演出と効果処理を分離する。
- 未対応効果や対象不在が成功扱いにならないことをテストする。
- 完了（第1段階）: `SkillExecutionResult` を追加し、既存のbool APIを維持しながら失敗理由を返せるようにした。未対応データ駆動効果は実行前に失敗として扱う。

### 4. 対象範囲の単一定義化

- 選択可能範囲、実効果範囲、ハイライト、VFX配置が別々に計算されている。
- 共通の範囲定義を使い、実戦闘と模擬フィールドで同一ロジックを共有する。
- TargetingService、Pathfinding、BattleManager間の盤面参照も整理する。
- 完了: `BattleRangeService` が選択範囲・効果範囲・直線・剣の扇形を一元管理し、戦闘表示側はその結果を使う。

### 5. BattleCharacterの状態と表示の分離

- ステータス、ダメージ計算、カウンター、状態異常、数字バフ、UI表示、ヒット演出が混在している。
- 計算と戦闘状態を先に分離し、GameObject/UI表示は段階的に残す。
- 完了（第1段階）: HP、攻撃・防御、スタン、数字パッシブ、死亡状態を `BattleCharacterState` に移し、`BattleCharacter` はUIと演出の窓口として維持した。

### 6. データ同期の検証

- キャラクター・施設・ステージのJSON、ScriptableObject、Database間に同期手順がある。
- まずID重複、参照欠落、未同期を検出するEditor検証を作り、移行の安全性を確認してから単一ソース化を検討する。
- 完了: `GameDataValidator` でJSON、Database、Resources用CharacterDatabaseのID同期、重複、ステージの参照欠落を検証する。

### 7. セーブ・UI・音声の整理

- GameManager、PlayerInventory、FacilityManagerに分散するPlayerPrefsを保存サービスへ集約する。
- UnityUIRuntimeThemeの全画面走査と、各画面スクリプトのレイアウト責務を整理する。
- GameAudioのロード・キャッシュ・音量設定を確認し、画面UIから独立させる。
- 完了（保存）: `PlayerProgressStore` にPlayerPrefsアクセスを集約し、既存キーを維持したままGameManager、PlayerInventory、FacilityManagerから利用する。
- 継続候補: 画面レイアウトと音量設定の責務分割は、UI改修と同じタイミングで扱う。

## 各段階の完了条件

- 既存ルールの自動テストが通る。
- Unity生成C#プロジェクトのビルドが通る。
- WebGLビルドとPlaywrightのスマホ・PC確認が通る。
- 変更したスキルは実戦画面または模擬フィールドで効果範囲とVFXを確認する。

## 次の着手順

1. 新規スキルは `BattleRangeService` と `SkillExecutionResult` を通して追加する。
2. `BattleRuleRegression` と `GameDataValidator` をビルド前に実行する。
3. 次のUI改修でBattleCharacterの表示処理とGameAudioの設定UIをさらに分ける。
