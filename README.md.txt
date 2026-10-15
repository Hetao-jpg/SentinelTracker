# 哨兵综合症监控 SentinelTracker

**作者**：核桃
**最新版本**：1.2.1
**兼容游戏**：Probably Stolen 0.46D
**兼容环境**：MelonLoader 7.4 及以上

> v1.2.0 新增：可选开启“真品免疫宁 -1 损伤池”，默认关闭。
> v1.1.0 起，面板严格对齐游戏代码事实：
> 哨兵**不掉血**，只有“天数计时器 + 只增不减的损伤池”，
> 死亡判据是 **池 ≥ 20**，与天数阶段无关。

## 安装

把 `SentinelTracker.dll` 放进游戏目录的 `Mods/` 文件夹，启动游戏即可。

## 做什么

按 **F3** 打开一个悬浮监控窗口，实时显示哨兵（Sentinel）综合症状态：

- 距离上次打针的天数（`daysSinceSentinelInjection`）
- 损伤池当前值与 `pool / 20` 进度条（`sentinelPool`）
- 当前所处阶段（安全 / 提示 / 警告 / 重损 / 濒死）
- **距哨兵崩溃还有几晚**（按当前涨速模拟）
- 打针建议与“最迟打针”提示
- 免疫宁成色效果速查
- **真品免疫宁 -1 损伤池**（可选，cfg 开关，默认关闭）

窗口**可拖动、可缩放、可调透明度**，位置和尺寸都会自动保存。

## 基本用法

### 打开 / 关闭

| 操作 | 说明 |
|------|------|
| 按 `F3` | 开关面板（按键可在 cfg 里改） |
| 点右上角 `×` | 关闭面板 |

### 窗口操作

| 操作 | 效果 |
|------|------|
| 拖动标题栏 | 移动窗口位置（松开鼠标时自动保存） |
| 拖动右下角 `◢` 手柄 | 调整窗口尺寸（松开鼠标时自动保存） |
| 修改 cfg `PanelWidth` / `PanelHeight` | 精确设定窗口尺寸（重启生效） |

尺寸限制：**300×240 ~ 900×800**。

## 状态说明（对齐原版机制）

| 阶段 | 天数 | 池增量/晚 | 颜色 | 说明 |
|------|------|-----------|------|------|
| 安全期 | 0~5 | +0 | 绿 | 完全无事件 |
| 提示期 | 6 | +0 | 黄 | 弹 `health_sentinel_mild`，尚未伤身 |
| 警告期 | 7~9 | +1 | 橙 | 池开始累积 |
| 重损期 | 10~13 | +2 | 红 | 累积加速，可能出现 `collapse_advanced` |
| 濒死期 | 14+ | +3 | 深红 | 累积最快，可能出现 `collapse_final` |

**死亡判据是 `sentinelPool >= 20`，与天数无关**：
即使刚打完针、天数为 0，只要池已经 ≥20，当晚结算仍会直接 `ExecuteGameOver("sentinel")`。

## 关键机制速查

- **池只增不减**：全代码未找到任何清零 / 扣减 `sentinelPool` 的路径。
- **打针只重置天数**：`ConsumeImmunivax` 只写 `daysSinceSentinelInjection`，不碰池。
- **不打针时间线**：选哨兵且一支针不打，第 16 晚必死。
- **安全节奏**：约每 5 天打一支真品针，池永远停在 0。
- **哨兵不掉血**：游戏里哨兵的唯一后果是涨池 → 写夜间日志 → 池到 20 崩死。
- **真品 -1 池（mod 新增，可选）**：开启 `ReducePoolOnGenuineInject` 后，
-  每注射一支**真品**免疫宁，损伤池 -1（下限 0）。过期、假货、空针不减。
-  该功能默认关闭，开启后与“池只增不减”的原版规则不同。
### 免疫宁成色

| 成色 | 天数结果 | 备注 |
|------|----------|------|
| 真品 `genuineInjector` | → 0 | 最优，重新获得约 5 晚零事件窗口 |
| 过期 `expiredInjector` | → 3 | 固定倒退到第 3 天 |
| 假货 `counterfeitInjector` | → 3/4/5 随机 | 效果不稳 |
| 其它 / 空 | 不变 | 无效操作，只播体感文案 |

**原版任何针都不降 `sentinelPool`。**
开启 mod 的 `ReducePoolOnGenuineInject` 后，真品每次 -1（见上）。

## ## 配置项（共 11 项

| 配置项 | 默认 | 说明 |
|--------|------|------|
| ShowUI | ✅ 开 | 是否默认显示面板 |
| ToggleKey | `F3` | 面板开关按键 |
| PanelOpacity | 0.9 | 面板背景透明度（0.1~1.0） |
| TextOpacity | 1.0 | 面板文字透明度（0.3~1.0） |
| HandleOpacity | 0.6 | 缩放手柄透明度（0.05~1.0） |
| PanelWidth | 360 | 面板宽度（像素） |
| PanelHeight | 380 | 面板高度（像素） |
| **ShowOnlyWhenSentinel** | ❌ 关 | 非哨兵存档隐藏监控内容 |
| **ShowImmunivaxHint** | ✅ 开 | 显示免疫宁成色速查 |
| **ReducePoolOnGenuineInject** | ❌ 关 | 真品免疫宁每次 -1 损伤池（mod 新增；直接改 cfg 文件需重启，游戏内管理器修改可即时生效） |
| UIScale | 0 | UI 缩放。0=自动按屏幕高度缩放（1080p=1.0，1440p≈1.25，4K≈2.0），手动 0.5~3.0。修改后立即生效。 |

> **面板位置**不在 cfg 里 —— 玩家拖动即可，位置存到 `PlayerPrefs`，跨存档共享。

### 透明度分离设计

面板的**背景**和**文字**透明度是**独立配置**的：

- 背景调透 → 不挡游戏画面
- 文字保持清晰 → 信息看得见
- 缩放手柄也是独立透明度 → 背景透明时手柄仍显眼

**推荐配置**（想要“文字浮在游戏画面上”效果）：

```ini
PanelOpacity = 0.4
TextOpacity = 1.0
HandleOpacity = 0.8
```

## 技术点

- **IMGUI 悬浮窗**：`OnGUI` 绘制，`GUI.depth = 33000` 保证在游戏 UI 之上
- **延迟初始化样式**：`GUI.skin` 不能在 `OnInitializeMelon` 里取（会拿到 null），必须延迟到 `OnGUI` 内首次调用
- **手柄用 `Input` 驱动**：不依赖 IMGUI 事件，兼容手柄模拟鼠标
- **不使用 `GUI.DrawTexture`**：游戏本身没用到它，IL2CPP linker 会把它 strip 掉，调用会报 `Method unstripping failed`。改用 `GUI.Button` + `◢` 字符绘制手柄，进度条用等宽字符绘制
- **坐标换算**：`Input.mousePosition` 原点在左下角、IMGUI 屏幕坐标原点在左上角，需要翻转 y
- **`GUI.depth` / `GUI.color` 的 try-finally 恢复**：`GUI.Window` 内部抛异常时不恢复会污染全局状态
- **哨兵判定双重回退**：优先 `HealthData.IsSentinel()`，异常时回退到手动遍历 `startingPerks`
- **`ShowOnlyWhenSentinel` 时跳过整个窗口**：避免出现“只剩标题栏、没有关闭按钮”的空白窗口
- **Harmony Postfix 挂钩**：通过 `HarmonyLib.Harmony` 对
-  `HealthData.ConsumeImmunivax` 打 Postfix，实现“真品 -1 池”。
-  真品判定走 `ItemFeature.realCondition.identifier == "genuineInjector"`，
-  与反汇编中 `ConsumeImmunivax` 的判定路径完全一致。
## 已知限制

- **面板必须在游戏内进图后才有数据**：主菜单下显示“等待进入游戏或加载存档...”
- **面板位置全局共享，不随存档区分**：它是 UI，不是游戏内容。换档后位置不会重置
- **透明度范围限制**：背景 0.1~1.0、文字 0.3~1.0、手柄 0.05~1.0，防止调成完全看不见
- **窗口尺寸限制**：300×240 ~ 900×800，防止调成无法操作的大小
- **内容块居中布局**：内容宽度上限 320px，窗口拉大时内容块居中，四周均匀留白
- **免疫宁成色提示为静态文案**：未读取库存，只作为速查参考
- **死亡倒计时按“今晚睡前”起算**：面板不区分玩家是否已入睡，显示的“N 晚后崩溃”是 OnSleep 结算后的预期
- **`ReducePoolOnGenuineInject` 改动需重启**：MelonPreferences 启动时读取，
-  改 cfg 后必须重启游戏；该项在面板上有独立提示行
## 排障

### 面板打不开

1. 按 `F3` 有反应吗？打开 `UserData/MelonPreferences.cfg` 检查 `ToggleKey` 是否被改成别的键
2. `Latest.log` 里应有 `[哨兵综合症监控] v1.1.0 已加载，按 F3 开关面板` —— 没有就是 DLL 没被加载

### 面板显示“等待进入游戏”

正常。**必须进游戏读档后**才有数据。主菜单、读档中、切场景时都会显示这句。

### 面板显示“未选择哨兵 perk”

正常。当前存档没有 `sentinel` 起始 perk，哨兵机制不会运行。
如果不想看到这个提示，可在 cfg 里把 `ShowOnlyWhenSentinel` 设为 `true`，面板会直接隐藏。

### `Latest.log` 里刷 `DrawResizeHandle 异常: Method unstripping failed`

v1.0.1 已修复（改用 `GUI.Button`）。若仍出现，检查 DLL 是否为最新版本。

### 面板位置 / 尺寸不生效

1. **位置**：存在 `PlayerPrefs` 里，改 cfg 无效。用鼠标拖动即可
2. **尺寸**：改 cfg 的 `PanelWidth` / `PanelHeight` 后**重启游戏**才生效

### 修改 cfg 后不生效

MelonLoader 读取 cfg 是**启动时**。改完 cfg 需要重启游戏。

## 更新日志

### v1.2.0（当前版本）

**新增：真品免疫宁 -1 损伤池（可选）**

- 新增 Harmony 补丁，Postfix 挂 `HealthData.ConsumeImmunivax`
- 精确判定真品：`ItemFeature.realCondition.identifier == "genuineInjector"`，
 与反汇编中原版判定路径一致；读不到时用“days 从非 0 变 0”兜底
- 每注射一支真品，`sentinelPool -= 1`（池已为 0 时不减）
- 过期 / 假货 / 空针不减池，行为与原版一致
- 新增配置项 `ReducePoolOnGenuineInject`（默认关闭）
- 面板在开启该功能时显示“★ 真品免疫宁：每次 -1 损伤池”
- 配置项总数 9 → 10，面板默认高度 340 → 360 以容纳新增信息

 ### v1.1.0

**新增：机制对齐与预警**

- 新增 `IsSentinel` 判定，非哨兵存档显示“未选择哨兵 perk”，不再误显示“安全期”
- 新增 `pool / 20` 文本进度条
- 新增**死亡倒计时**：按当前天数与池值模拟到 `pool >= 20` 的夜数
- 新增“最迟打针 / 池不可减”建议文案
- 新增免疫宁成色速查（真品 → 0，过期 → 3，假货 → 3/4/5，任何针不降池）
- 阶段文案修正：删除“轻微掉血 / 加速死亡 / 极易暴毙”，改为“池 +0/+1/+2/+3 / 晚”
- 阶段边界修正：第 6 天单列为“提示期”，不再归入“安全期”
- 新增配置项：`ShowOnlyWhenSentinel`、`ShowImmunivaxHint`
- 面板默认尺寸调整为 360×340，以容纳新增信息

**修复**

- 修复非哨兵玩家看到“安全期 0 天”导致误以为机制在运行的问题
- 修复“距下一阶段还有 N 天”容易让玩家误以为天数即死期的误导
- 修复 `ShowOnlyWhenSentinel=true` 时留下空白窗口的问题
- 修复拖动窗口后位置可能不落盘的问题

### v1.0.1

**新增**：窗口自定义配置

- 右下角手柄可拖拽调整窗口尺寸，限制范围 260×180 ~ 800×700
- 面板背景透明度独立配置（`PanelOpacity`）
- 面板文字透明度独立配置（`TextOpacity`）
- 缩放手柄透明度独立配置（`HandleOpacity`）
- 面板宽高写入 cfg 可手动精确调节（`PanelWidth` / `PanelHeight`）
- 面板位置改存 `PlayerPrefs`（不再暴露到 cfg）

**修复**：崩溃与显示

- 修 `GUI.depth` 异常路径不恢复（改用 `try-finally`）
- 修 `GUI.DrawTexture` 被 IL2CPP 裁剪导致的每帧报错（改用 `GUI.Button` + `◢` 字符）
- 修关闭按钮 / 手柄跟随背景透明度变淡的问题
- 修文字靠左，改为居中显示
- 修日志双前缀（MelonLoader 自动加前缀，代码里不再手写）

### v1.0.0

首次发布

- F3 开关悬浮窗口
- 实时显示哨兵天数 / 损伤池
- 四阶段状态判定
- 提示打药建议

## 反馈渠道

- QQ 群：**@夏月的核桃**
- Nico wiki：https://probably-stolen.nicokobo.com/zh/mods