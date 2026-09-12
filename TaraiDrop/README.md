# Mashiro Tarai Drop

プレイヤーがエリアへ入ると、頭上から全員に見えるたらいが落ちるギミックです。
衝突時に全員へ効果音を鳴らし、片付け後は再入場で何度でも発動します。

## できること

- 入ったプレイヤーの頭上へたらいを落とす。
- たらいの位置を `VRC Object Sync` で全員に共有する。
- 落下中の追加発動を無視し、同時に出るたらいを1個に制限する。
- 最初の衝突で効果音を全員に再生し、指定時間後にたらいを隠す。

## セットアップ

1. 進入エリアの GameObject に `Collider` を付け、**Is Trigger** をオンにします。
2. 同じ GameObject に `Add Component > MashiroTheater > Mashiro Tarai Drop` を追加します。
3. たらいの GameObject に `Rigidbody`、非 Trigger の `Collider`、`VRC Object Sync` を追加します。
4. たらいに `Add Component > MashiroTheater > Mashiro Tarai Collision Relay` を追加し、**Controller** に進入エリアを指定します。
5. `Audio Source` を置いて効果音を設定します。全員に同じ音量で聞こえるよう **Spatial Blend** を `0`、**Play On Awake** をオフにします。
6. 進入エリア側の **Tarai**、**Tarai Rigidbody**、**Impact Audio** をそれぞれ割り当てます。

## インスペクタ項目

| 項目 | 説明 |
| --- | --- |
| Tarai | 落下させるたらいの GameObject |
| Tarai Rigidbody | たらいに付けた Rigidbody |
| Height Above Head | 頭上から落とす高さ（既定: 2m） |
| Hide Delay | 衝突してから隠すまでの秒数（既定: 1秒） |
| Impact Audio | Spatial Blendを0にした効果音用 Audio Source |
| Controller | Collision Relay から参照する進入エリア |

## API リファレンス

| メソッド | 用途 | 呼び出し種別 |
| --- | --- | --- |
| `HandleTaraiCollision()` | 所有者の衝突をコントローラーへ通知する | Collision Relayからの内部連携用（配線不要） |
| `PlayImpactAndScheduleHide()` | 全員で効果音を再生し、片付けを予約する | ネットワーク内部連携用（配線不要） |
| `HideTarai()` | たらいを隠して再発動可能にする | 遅延イベント用（配線不要） |

## 注意点

- 落下中に別のプレイヤーが入った場合、その入場は無視されます。一度エリア外へ出て、たらいが消えてから入り直してください。
- Boothで購入したモデルは、たらいの GameObject の見た目として子に配置してください。
- 同期確認は複数クライアントで行ってください。衝突判定と位置同期は、たらいの所有者が担当します。
