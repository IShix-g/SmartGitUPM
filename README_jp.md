![Unity](https://img.shields.io/badge/Unity-2021.3%2B-black)

# SmartGitUPM
Unity Editorで、Gitリポジトリ内のパッケージを効率的に管理するためのプラグイン。

> [!IMPORTANT]
> DISCLAIMER: SmartGitUPM is an open-source service, not an official service provided by Unity Technologies Inc.

![Header](Docs/header.png)

# このプラグインを利用する理由

## アップデートを一目で確認

Unity Package Managerは、Gitで公開されているパッケージのアップデート情報を知る事ができません。
`SmartGitUPM`なら分かりやすいUIで確認できます。

![Update](Docs/update.jpg)

## プライベートリポジトリの更新確認に対応

プライベートリポジトリ(SSL接続)のパッケージに対応。更新確認がプライベートリポジトリでも可能です。

![Update](Docs/private_repo.jpg)

- SSLを設定し、SSL用のURLを指定。例) `git@github.com:IShix-g/SmartGitUPM.git`
- 大きなプライベートリポジトリは、表示に時間がかかるので使用に向きません。
- パブリックリポジトリに大小は関係なく、表示速度は変わりません。
- プライベートリポジトリのインストールはUnity Package Manager(UPM)の機能を利用しています。

## 更新通知

Unity Editorを開く度に、パッケージの更新があれば通知を受け取る事ができます。(通知の停止可)

<img alt="Alert" src="Docs/alert.jpg" width="550"/>

## 内部ではUPMを利用しているので安心

内部では、Unity Package Manager(UPM)を利用しパッケージ管理をしているので安心。
もし、`SmartGitUPM`が気に入らなくなっても削除すればおしまいです。以降は、UPMで通常通り管理できます。

![Update](Docs/upm.jpg)

***

# Getting Started

## Git Urlからインストール

"Unity Editor : Window > Package Manager > Add package from git URL...".

URL: `https://github.com/IShix-g/SmartGitUPM.git?path=Packages/SmartGitUPM`

![Update](Docs/package_manager.png)

## SmartGitUPMを開く

`Unity Editor : Window > Smart Git UPM`

![Update](Docs/open_sgupm.jpg)

## 設定を開く

歯車ボタン、もしくは未設定時のみ表示される設定ボタンをクリック

<img alt="Configure" src="Docs/click_configure.jpg" width="550"/>

## パッケージを設定

| フィールド        | 説明                              | 例                                                                                                                                                                        |
|--------------|---------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Update Notify | Unity Editor起動時にアップデートの通知を受けるか？ | [チェックした場合] アップデートを受け取る                                                                                                                                                   |
| Install Url  | パッケージのインストールURL (https or SSL)  | [https] https://github.com/IShix-g/CMSuniVortex.git?path=Packages/CMSuniVortex<br/>[SSL] git@IShix-g-GitHub:IShix-g/UnityJenkinsBuilder.git?path=Assets/Plugins/Jenkins/ |
| Branch       | メインのブランチを指定                     | main や masterなど                                                                                                                                                          |

<img alt="SGUPM" src="Docs/setting_package2.jpg" width="550"/>

## リロード

設定後、リロードボタンをクリックして完了です。
次回からは、下記タイミングでリロード処理が実行されます。

- Unity Editorを起動した時
- Smart Git UPMを表示した時

<img alt="SGUPM" src="Docs/sgupm.jpg" width="500"/>

***

## 各ボタンの説明

<img alt="Buttons" src="Docs/buttons.jpg" width="500"/>

1. パッケージの設定
2. パッケージのアップデート情報のリロード
3. Unity Package Managerを開く
4. Smart Git UPMのGitHubページを開く
5. Smart Git UPMのバージョン、クリックで最新バージョンがあるか確認します。

## 現在のパッケージの状態表示

<img alt="States" src="Docs/states.jpg" width="500"/>

1. 最新バージョンのインストール済み
2. インストール済み、且つ更新可能な新バージョンあり v1.0.9 (現在) -> v1.0.10 (新)
3. インストール済み、且つバージョンの固定、URLの最後に`#1.0.0`などのバージョンを指定する事で固定可
4. 未インストール

## UPMとの役割分担

SmartGitUPMは、Unity Package Managerが行わないGitのバージョン情報を視覚化したり、更新の通知をするプラグインです。
実際のインストールやアンストールは、Unity Package Managerに依頼します。

<img alt="Role" src="Docs/role.jpg" width="600"/>

## OpenUPMとの違い

### OpenUPM
OpenUPMはオープンソースパッケージのレジストリです。登録されたパッケージをUnity Package Managerを通じて管理できます。パブリックに公開されたオープンソースパッケージを対象としています。

### SmartGitUPM
SmartGitUPMは登録の必要がなく、ユーザーが自身で必要なパッケージを管理します。パブリック、プライベートのパッケージを問わず、独自のインターフェース（UI）で柔軟に管理することが可能です。

_※ ロゴがほぼ一緒ですが、狙った訳ではありません。**「パッケージ = ダンボール = ねこ」** それならダンボールが大好きな猫がダンボールで遊んでいるロゴにしようと作ったら、たまたま同じようなロゴになりました。可愛すぎて変更したくなかったのでしばらくこのままでいかせてください。すみません。お願いします。_

<img alt="Logos" src="Docs/logos.png" width="600"/>