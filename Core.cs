using System;
using Il2Cpp;
using MelonLoader;
using MelonLoader.Preferences;
using UnityEngine;
// ── 消除类型歧义：System.Numerics / System.Drawing 同名类型 ──
using Vector2 = UnityEngine.Vector2;
using Color = UnityEngine.Color;

namespace SentinelTracker;

public class Core : MelonMod
{
    // ── 配置 ────────────────────────────────────────────────
    private static MelonPreferences_Category _cfgCategory;
    private static MelonPreferences_Entry<bool> _cfgShowUI;
    private static MelonPreferences_Entry<KeyCode> _cfgToggleKey;
    private static MelonPreferences_Entry<float> _cfgPanelOpacity;
    private static MelonPreferences_Entry<float> _cfgTextOpacity;
    private static MelonPreferences_Entry<float> _cfgPanelWidth;
    private static MelonPreferences_Entry<float> _cfgPanelHeight;
    private static MelonPreferences_Entry<float> _cfgHandleOpacity;
    private static MelonPreferences_Entry<bool> _cfgShowOnlyWhenSentinel;
    private static MelonPreferences_Entry<bool> _cfgShowImmunivaxHint;

    private const string PP_PANEL_X = "SentinelTracker.PanelX";
    private const string PP_PANEL_Y = "SentinelTracker.PanelY";

    // ── 运行时状态 ──────────────────────────────────────────
    private float _readTimer;
    private int _displayD;
    private int _displayPool;
    private bool _isSentinel;
    private bool _dataReady;
    private bool _isUIVisible;

    private Rect _windowRect = new Rect(20f, 20f, 360f, 340f);
    private bool _isResizing;
    private Vector2 _resizeStartMouse;
    private Vector2 _resizeStartSize;

    // ── 样式 ────────────────────────────────────────────────
    private GUIStyle _titleStyle;
    private GUIStyle _dataStyle;
    private GUIStyle _tipStyle;
    private GUIStyle _hotkeyStyle;
    private GUIStyle _waitStyle;
    private GUIStyle _barStyle;
    private GUIStyle _warnStyle;
    private bool _stylesReady;
    private static GUIStyle _handleStyle;

    // ── 常量 ────────────────────────────────────────────────
    private const float PANEL_MIN_W = 300f;
    private const float PANEL_MIN_H = 240f;
    private const float PANEL_MAX_W = 800f;
    private const float PANEL_MAX_H = 700f;
    private const float HANDLE_SIZE = 22f;
    private const int POOL_MAX = 20;
    private const int POOL_WARN_ADVANCED = 10;
    private const int POOL_WARN_FINAL = 15;
    private const int POOL_DEATH = 20;

    // ══════════════════════════════════════════════════════
    //  初始化
    // ══════════════════════════════════════════════════════
    public override void OnInitializeMelon()
    {
        _cfgCategory = MelonPreferences.CreateCategory("SentinelTracker", "哨兵综合症监控");

        _cfgShowUI = _cfgCategory.CreateEntry<bool>(
            "ShowUI", true, "显示监控面板", "是否默认显示哨兵监控面板",
            false, false, (ValueValidator)null, (string)null);

        _cfgToggleKey = _cfgCategory.CreateEntry<KeyCode>(
            "ToggleKey", (KeyCode)284, "面板开关按键", "默认 F3",
            false, false, (ValueValidator)null, (string)null);

        _cfgPanelOpacity = _cfgCategory.CreateEntry<float>(
            "PanelOpacity", 0.9f, "面板背景透明度",
            "1.0=不透明，0.1=几乎看不见",
            false, false, (ValueValidator)null, (string)null);

        _cfgTextOpacity = _cfgCategory.CreateEntry<float>(
            "TextOpacity", 1f, "文字透明度",
            "1.0=完全不透明，0.5=半透明",
            false, false, (ValueValidator)null, (string)null);

        _cfgPanelWidth = _cfgCategory.CreateEntry<float>(
            "PanelWidth", 360f, "面板宽度", "面板宽度（像素，拖右下角调整）",
            false, false, (ValueValidator)null, (string)null);

        _cfgPanelHeight = _cfgCategory.CreateEntry<float>(
            "PanelHeight", 340f, "面板高度", "面板高度（像素，拖右下角调整）",
            false, false, (ValueValidator)null, (string)null);

        _cfgHandleOpacity = _cfgCategory.CreateEntry<float>(
            "HandleOpacity", 0.6f, "缩放手柄透明度",
            "右下角缩放手柄透明度，独立于面板背景",
            false, false, (ValueValidator)null, (string)null);

        _cfgShowOnlyWhenSentinel = _cfgCategory.CreateEntry<bool>(
            "ShowOnlyWhenSentinel", false, "仅哨兵玩家显示",
            "非哨兵存档隐藏监控内容（防止误以为机制在运行）",
            false, false, (ValueValidator)null, (string)null);

        _cfgShowImmunivaxHint = _cfgCategory.CreateEntry<bool>(
            "ShowImmunivaxHint", true, "显示免疫宁提示",
            "在面板下方显示免疫宁成色效果速查",
            false, false, (ValueValidator)null, (string)null);

        _cfgCategory.SaveToFile(true);
        _isUIVisible = _cfgShowUI.Value;

        _windowRect.x = PlayerPrefs.GetFloat(PP_PANEL_X, 20f);
        _windowRect.y = PlayerPrefs.GetFloat(PP_PANEL_Y, 20f);
        _windowRect.width = Mathf.Clamp(_cfgPanelWidth.Value, PANEL_MIN_W, PANEL_MAX_W);
        _windowRect.height = Mathf.Clamp(_cfgPanelHeight.Value, PANEL_MIN_H, PANEL_MAX_H);

        LoggerInstance.Msg($"v1.1.0 已加载，按 {_cfgToggleKey.Value} 开关面板");
    }

    // ══════════════════════════════════════════════════════
    //  样式（延迟到 OnGUI，因为 GUI.skin 在 OnInitializeMelon 里为 null）
    // ══════════════════════════════════════════════════════
    private void EnsureStyles()
    {
        if (_stylesReady) return;

        _titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
        };

        _dataStyle = new GUIStyle(GUI.skin.label) { fontSize = 14 };
        _dataStyle.normal.textColor = Color.white;
        _dataStyle.alignment = TextAnchor.MiddleCenter;

        _tipStyle = new GUIStyle(GUI.skin.label) { fontSize = 13 };
        _tipStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f);
        _tipStyle.alignment = TextAnchor.MiddleCenter;

        _hotkeyStyle = new GUIStyle(GUI.skin.label) { fontSize = 11 };
        _hotkeyStyle.normal.textColor = new Color(0.5f, 0.5f, 0.5f);
        _hotkeyStyle.alignment = TextAnchor.MiddleRight;

        _waitStyle = new GUIStyle(GUI.skin.label) { fontSize = 14 };
        _waitStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
        _waitStyle.alignment = TextAnchor.MiddleCenter;

        _barStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
        };
        _barStyle.normal.textColor = Color.white;

        _warnStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
        };

        _stylesReady = true;
    }

    // ══════════════════════════════════════════════════════
    //  每帧
    // ══════════════════════════════════════════════════════
    public override void OnUpdate()
    {
        if (Input.GetKeyDown(_cfgToggleKey.Value))
        {
            _isUIVisible = !_isUIVisible;
            _cfgShowUI.Value = _isUIVisible;
            MelonPreferences.Save();
        }

        _readTimer += Time.deltaTime;
        if (_readTimer < 0.5f) return;
        _readTimer = 0f;
        ReadData();
    }

    private void ReadData()
    {
        try
        {
            if (!PlayerStore.IsInstanceExist()) { _dataReady = false; return; }
            PlayerStore instance = PlayerStore.Instance;
            if (instance == null) { _dataReady = false; return; }

            HealthData healthData = instance.healthData;
            if (healthData == null) { _dataReady = false; return; }

            _displayD = healthData.daysSinceSentinelInjection;
            _displayPool = healthData.sentinelPool;
            _isSentinel = CheckIsSentinel();
            _dataReady = true;
        }
        catch (Exception ex)
        {
            _dataReady = false;
            LoggerInstance.Error("❌ 读取数据异常: " + ex.Message);
        }
    }

    /// <summary>
    /// 优先调用 HealthData.IsSentinel()；失败时回退到手动遍历 startingPerks。
    /// </summary>
    private bool CheckIsSentinel()
    {
        try { return HealthData.IsSentinel(); }
        catch (Exception ex)
        {
            LoggerInstance.Warning("HealthData.IsSentinel() 调用失败，改用回退: " + ex.Message);
        }

        try
        {
            PlayerStore store = PlayerStore.Instance;
            if (store == null) return false;
            var perks = store.startingPerks;
            if (perks == null) return false;
            for (int i = 0; i < perks.Count; i++)
            {
                var p = perks[i];
                if (p != null && p.id == "sentinel") return true;
            }
        }
        catch (Exception ex)
        {
            LoggerInstance.Warning("遍历 startingPerks 失败: " + ex.Message);
        }
        return false;
    }

    // ══════════════════════════════════════════════════════
    //  OnGUI
    // ══════════════════════════════════════════════════════
    public override void OnGUI()
    {
        if (!_isUIVisible) return;

        int savedDepth = GUI.depth;
        Color savedColor = GUI.color;

        try
        {
            EnsureStyles();
            if (!_stylesReady) return;

            GUI.depth = 33000;
            float panelAlpha = Mathf.Clamp(_cfgPanelOpacity?.Value ?? 0.9f, 0.1f, 1f);
            GUI.color = new Color(1f, 1f, 1f, panelAlpha);

            HandleResizeInput();

            // “仅哨兵显示”且已读数据且当前不是哨兵 → 完全不画窗口
            bool hideForNonSentinel =
                _dataReady &&
                !_isSentinel &&
                _cfgShowOnlyWhenSentinel != null &&
                _cfgShowOnlyWhenSentinel.Value;

            if (!hideForNonSentinel)
            {
                // Il2CppInterop 提供 Action<int> → GUI.WindowFunction 的隐式转换
                _windowRect = GUI.Window(
                    98765, _windowRect,
                    (Action<int>)DrawWindow,
                    "哨兵综合症监控");

                DrawResizeHandle();
            }

            // 拖动/缩放后保存位置（放在 GUI.Window 之后更稳）
            if (Event.current != null && Event.current.type == EventType.MouseUp)
            {
                PlayerPrefs.SetFloat(PP_PANEL_X, _windowRect.x);
                PlayerPrefs.SetFloat(PP_PANEL_Y, _windowRect.y);
            }
        }
        catch (Exception ex)
        {
            LoggerInstance.Error($"❌ OnGUI 异常: {ex}");
        }
        finally
        {
            GUI.color = savedColor;
            GUI.depth = savedDepth;
        }
    }

    // ══════════════════════════════════════════════════════
    //  缩放
    // ══════════════════════════════════════════════════════
    private static Vector2 GetGuiMousePosition()
    {
        return new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
    }

    private void HandleResizeInput()
    {
        float hx = _windowRect.x + _windowRect.width - HANDLE_SIZE;
        float hy = _windowRect.y + _windowRect.height - HANDLE_SIZE;
        Rect handleRect = new Rect(hx, hy, HANDLE_SIZE, HANDLE_SIZE);

        Vector2 mouse = GetGuiMousePosition();
        bool overHandle = handleRect.Contains(mouse);

        if (Input.GetMouseButtonDown(0) && overHandle)
        {
            _isResizing = true;
            _resizeStartMouse = mouse;
            _resizeStartSize = new Vector2(_windowRect.width, _windowRect.height);
        }

        if (_isResizing && Input.GetMouseButton(0))
        {
            float dx = mouse.x - _resizeStartMouse.x;
            float dy = mouse.y - _resizeStartMouse.y;
            _windowRect.width = Mathf.Clamp(_resizeStartSize.x + dx, PANEL_MIN_W, PANEL_MAX_W);
            _windowRect.height = Mathf.Clamp(_resizeStartSize.y + dy, PANEL_MIN_H, PANEL_MAX_H);
        }

        if (_isResizing && Input.GetMouseButtonUp(0))
        {
            _isResizing = false;
            _cfgPanelWidth.Value = _windowRect.width;
            _cfgPanelHeight.Value = _windowRect.height;
            MelonPreferences.Save();
        }
    }

    private void DrawResizeHandle()
    {
        Color saved = GUI.color;
        float handleAlpha = Mathf.Clamp(_cfgHandleOpacity?.Value ?? 0.6f, 0.05f, 1f);
        GUI.color = new Color(1f, 1f, 1f, handleAlpha);
        try
        {
            if (_handleStyle == null)
            {
                _handleStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                };
                _handleStyle.normal.textColor = new Color(1f, 0.95f, 0.6f);
                _handleStyle.hover.textColor = Color.white;
                _handleStyle.active.textColor = new Color(0.6f, 1f, 0.6f);
            }

            float hx = _windowRect.x + _windowRect.width - HANDLE_SIZE;
            float hy = _windowRect.y + _windowRect.height - HANDLE_SIZE;
            GUI.Button(new Rect(hx + 2f, hy + 2f, 18f, 18f), "◢", _handleStyle);
        }
        catch (Exception ex)
        {
            LoggerInstance.Error("❌ DrawResizeHandle 异常: " + ex.Message);
        }
        finally
        {
            GUI.color = saved;
        }
    }

    // ══════════════════════════════════════════════════════
    //  主绘制
    // ══════════════════════════════════════════════════════
    private void DrawWindow(int windowID)
    {
        try
        {
            GUIStyle titleStyle = _titleStyle ?? GUI.skin.label;
            GUIStyle dataStyle = _dataStyle ?? GUI.skin.label;
            GUIStyle tipStyle = _tipStyle ?? GUI.skin.label;
            GUIStyle hotkeyStyle = _hotkeyStyle ?? GUI.skin.label;
            GUIStyle waitStyle = _waitStyle ?? GUI.skin.label;
            GUIStyle barStyle = _barStyle ?? GUI.skin.label;
            GUIStyle warnStyle = _warnStyle ?? GUI.skin.label;

            // 标题栏拖动区
            GUI.DragWindow(new Rect(0f, 0f, _windowRect.width - 30f, 30f));

            // 关闭按钮
            if (GUI.Button(new Rect(_windowRect.width - 25f, 2f, 20f, 20f), "×"))
            {
                _isUIVisible = false;
                _cfgShowUI.Value = false;
                MelonPreferences.Save();
            }

            Color savedColor = GUI.color;
            float textAlpha = Mathf.Clamp(_cfgTextOpacity?.Value ?? 1f, 0.3f, 1f);
            GUI.color = new Color(1f, 1f, 1f, textAlpha);

            float width = _windowRect.width;
            float height = _windowRect.height;
            float contentW = Mathf.Min(width - 24f, 320f);
            float contentX = (width - contentW) * 0.5f;
            float y = 34f;

            // ── 未就绪 ───────────────────────────────────
            if (!_dataReady)
            {
                GUI.Label(new Rect(contentX, y + 60f, contentW, 24f),
                    "等待进入游戏或加载存档...", waitStyle);
                GUI.Label(new Rect(contentX, height - 24f, contentW, 18f),
                    $"快捷键: {_cfgToggleKey.Value} 开关面板", hotkeyStyle);
                GUI.color = savedColor;
                return;
            }

            // ── 非哨兵 ───────────────────────────────────
            // 注：若开启“仅哨兵显示”，OnGUI 已经跳过整个窗口，不会走到这里。
            if (!_isSentinel)
            {
                titleStyle.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
                GUI.Label(new Rect(contentX, y, contentW, 22f), "未选择哨兵 perk", titleStyle);
                y += 32f;
                GUI.Label(new Rect(contentX, y, contentW, 20f), "哨兵机制未激活", tipStyle);
                y += 24f;
                GUI.Label(new Rect(contentX, y, contentW, 20f),
                    "（选择 sentinel 起始 perk 后生效）", tipStyle);
                y += 24f;
                GUI.Label(new Rect(contentX, y, contentW, 20f),
                    "原版哨兵：只涨池，不掉血；池 ≥ 20 触发哨兵崩溃", tipStyle);

                GUI.Label(new Rect(contentX, height - 24f, contentW, 18f),
                    $"快捷键: {_cfgToggleKey.Value} · 池只增不减", hotkeyStyle);
                GUI.color = savedColor;
                return;
            }

            // ── 哨兵：阶段标题 ────────────────────────────
            string phaseText = GetPhaseText(_displayD);
            titleStyle.normal.textColor = GetPhaseColor(_displayD);
            GUI.Label(new Rect(contentX, y, contentW, 22f), phaseText, titleStyle);
            y += 28f;

            // ── 天数 ─────────────────────────────────────
            GUI.Label(new Rect(contentX, y, contentW, 20f),
                $"距离上次打针：{_displayD} 天", dataStyle);
            y += 22f;

            // ── 涨速 ─────────────────────────────────────
            int rate = GetPoolRate(_displayD);
            string rateText;
            if (rate == 0)
                rateText = (_displayD == 6) ? "今晚仅弹提示，不涨池" : "当前涨速：0 / 晚";
            else
                rateText = $"当前涨速：+{rate} / 晚";
            GUI.Label(new Rect(contentX, y, contentW, 20f), rateText, tipStyle);
            y += 22f;

            // ── 池进度 ───────────────────────────────────
            int pool = Mathf.Clamp(_displayPool, 0, POOL_MAX);
            barStyle.normal.textColor = GetPoolColor(pool);
            GUI.Label(new Rect(contentX, y, contentW, 20f),
                $"损伤池：{pool} / {POOL_MAX}", barStyle);
            y += 22f;
            GUI.Label(new Rect(contentX, y, contentW, 20f),
                BuildProgressBar(pool, POOL_MAX, 20), barStyle);
            y += 26f;

            // ── 死亡倒计时 ───────────────────────────────
            int nightsLeft = NightsToDeath(_displayD, _displayPool);
            if (nightsLeft == 0)
            {
                warnStyle.normal.textColor = new Color(1f, 0f, 0f);
                GUI.Label(new Rect(contentX, y, contentW, 22f),
                    "⚠ 池已满，本晚必死", warnStyle);
            }
            else if (nightsLeft > 0)
            {
                warnStyle.normal.textColor = GetCountdownColor(nightsLeft);
                GUI.Label(new Rect(contentX, y, contentW, 22f),
                    $"若不注射，预计 {nightsLeft} 晚后哨兵崩溃", warnStyle);
            }
            else
            {
                GUI.Label(new Rect(contentX, y, contentW, 22f),
                    "不会崩溃（当前涨速为 0）", tipStyle);
            }
            y += 26f;

            // ── 建议 ─────────────────────────────────────
            GUI.Label(new Rect(contentX, y, contentW, 20f),
                GetAdvice(_displayD, _displayPool, rate), tipStyle);
            y += 24f;

            // ── 免疫宁提示 ───────────────────────────────
            if (_cfgShowImmunivaxHint != null && _cfgShowImmunivaxHint.Value)
            {
                y += 4f;
                GUI.Label(new Rect(contentX, y, contentW, 18f),
                    "免疫宁效果（按成色）", tipStyle);
                y += 18f;
                GUI.Label(new Rect(contentX, y, contentW, 18f),
                    "真品 → 天数 0 ｜ 过期 → 3 ｜ 假货 → 3/4/5", tipStyle);
                y += 18f;
                GUI.Label(new Rect(contentX, y, contentW, 18f),
                    "※ 任何针都不降损伤池", tipStyle);
            }

            // ── 底部提示 ─────────────────────────────────
            GUI.Label(new Rect(contentX, height - 24f, contentW, 18f),
                $"快捷键: {_cfgToggleKey.Value} · 池只增不减", hotkeyStyle);

            GUI.color = savedColor;
        }
        catch (Exception ex)
        {
            LoggerInstance.Error($"❌ DrawWindow 异常: {ex}");
        }
    }

    // ══════════════════════════════════════════════════════
    //  纯逻辑辅助
    // ══════════════════════════════════════════════════════
    private static string GetPhaseText(int days)
    {
        if (days < 6) return "【安全期】无累积";
        if (days < 7) return "【提示期】已发病";
        if (days < 10) return "【警告期】池 +1 / 晚";
        if (days < 14) return "【重损期】池 +2 / 晚";
        return "【濒死期】池 +3 / 晚";
    }

    private static Color GetPhaseColor(int days)
    {
        if (days < 6) return new Color(0.4f, 1f, 0.4f);
        if (days < 7) return new Color(1f, 0.9f, 0.4f);
        if (days < 10) return new Color(1f, 0.6f, 0f);
        if (days < 14) return new Color(1f, 0.2f, 0.2f);
        return new Color(1f, 0f, 0f);
    }

    private static int GetPoolRate(int days)
    {
        if (days >= 14) return 3;
        if (days >= 10) return 2;
        if (days >= 7) return 1;
        return 0;
    }

    private static Color GetPoolColor(int pool)
    {
        if (pool >= POOL_DEATH) return new Color(1f, 0f, 0f);
        if (pool >= POOL_WARN_FINAL) return new Color(1f, 0.2f, 0.2f);
        if (pool >= POOL_WARN_ADVANCED) return new Color(1f, 0.6f, 0f);
        return new Color(0.6f, 1f, 0.6f);
    }

    private static Color GetCountdownColor(int nights)
    {
        if (nights <= 1) return new Color(1f, 0f, 0f);
        if (nights <= 3) return new Color(1f, 0.2f, 0.2f);
        if (nights <= 6) return new Color(1f, 0.6f, 0f);
        return new Color(1f, 0.9f, 0.4f);
    }

    /// <summary>
    /// 文本进度条，形如 [#####---------------]
    /// </summary>
    private static string BuildProgressBar(int value, int max, int width)
    {
        int filled = Mathf.Clamp(Mathf.RoundToInt((float)value / max * width), 0, width);
        int empty = width - filled;
        return "[" + new string('#', filled) + new string('-', empty) + "]";
    }

    /// <summary>
    /// 按原版机制模拟：先 days++ 再按档位涨池，直到 pool >= 20。
    /// 返回预计还能活几晚；0 = 已满，本晚必死；-1 = 永不崩溃（涨速为 0）。
    /// </summary>
    private static int NightsToDeath(int days, int pool)
    {
        if (pool >= POOL_DEATH) return 0;
        int d = days;
        int p = pool;
        for (int n = 1; n <= 100; n++)
        {
            d++;
            p += GetPoolRate(d);
            if (p >= POOL_DEATH) return n;
        }
        return -1;
    }

    private static string GetAdvice(int days, int pool, int rate)
    {
        if (pool >= POOL_DEATH)
            return "已无法挽回，本晚结算即崩溃。";

        if (days < 7)
        {
            // safe = 从今晚起，还能“不涨池”的夜数
            // days==5 → 今晚结算 days→6，仍 +0 → safe=1
            // days==6 → 今晚结算 days→7，开始 +1  → safe=0
            int safe = 6 - days;
            if (safe < 0) safe = 0;
            if (safe == 0)
                return "⚠ 今晚结算后 days→7，开始 +1 涨池。";
            return $"✅ 还能安全 {safe} 晚（到第 6 晚为止），建议尽早打真品针归零。";
        }

        if (pool >= POOL_WARN_FINAL)
            return "🚨 池已 ≥15，立即打真品免疫宁。";
        if (pool >= POOL_WARN_ADVANCED)
            return "⚠ 池已 ≥10，尽快打真品免疫宁止损。";

        // days >= 7 时 rate 必然 ≥ 1，这里的 rate 只是保留参数以示可扩展
        return "💡 已开始涨池，打真品针可重置天数（池不变）。";
    }
}