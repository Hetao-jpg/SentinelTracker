using System;
using Il2Cpp;
using MelonLoader;
using MelonLoader.Preferences;
using UnityEngine;
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
    private static MelonPreferences_Entry<float> _cfgUIScale;

    internal static MelonPreferences_Entry<bool> CfgReducePoolOnGenuineInject;

    private const string PP_PANEL_X = "SentinelTracker.PanelX";
    private const string PP_PANEL_Y = "SentinelTracker.PanelY";

    // ── 运行时状态 ──────────────────────────────────────────
    private float _readTimer;
    private int _displayD;
    private int _displayPool;
    private bool _isSentinel;
    private bool _dataReady;
    private bool _isUIVisible;

    /// <summary>当前 UI 缩放。每帧在 OnGUI 开头刷新。</summary>
    private float _S = 1f;

    private Rect _windowRect = new Rect(20f, 20f, 360f, 400f);
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
    private GUIStyle _closeStyle;
    private bool _stylesReady;
    private float _stylesScale = -1f;
    private static GUIStyle _handleStyle;

    // ── 常量（逻辑像素） ─────────────────────────────────────
    private const float PANEL_MIN_W = 300f;
    private const float PANEL_MIN_H = 240f;
    private const float PANEL_MAX_W = 900f;
    private const float PANEL_MAX_H = 800f;
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
            "PanelOpacity", 0.9f, "面板背景透明度", "1.0=不透明，0.1=几乎看不见",
            false, false, (ValueValidator)null, (string)null);

        _cfgTextOpacity = _cfgCategory.CreateEntry<float>(
            "TextOpacity", 1f, "文字透明度", "1.0=完全不透明，0.5=半透明",
            false, false, (ValueValidator)null, (string)null);

        _cfgPanelWidth = _cfgCategory.CreateEntry<float>(
            "PanelWidth", 360f, "面板宽度", "面板宽度（逻辑像素，拖右下角调整）",
            false, false, (ValueValidator)null, (string)null);

        _cfgPanelHeight = _cfgCategory.CreateEntry<float>(
            "PanelHeight", 400f, "面板高度", "面板高度（逻辑像素，拖右下角调整）",
            false, false, (ValueValidator)null, (string)null);

        _cfgHandleOpacity = _cfgCategory.CreateEntry<float>(
            "HandleOpacity", 0.6f, "缩放手柄透明度", "右下角缩放手柄透明度",
            false, false, (ValueValidator)null, (string)null);

        _cfgShowOnlyWhenSentinel = _cfgCategory.CreateEntry<bool>(
            "ShowOnlyWhenSentinel", false, "仅哨兵玩家显示",
            "非哨兵存档隐藏监控内容",
            false, false, (ValueValidator)null, (string)null);

        _cfgShowImmunivaxHint = _cfgCategory.CreateEntry<bool>(
            "ShowImmunivaxHint", true, "显示免疫宁提示",
            "在面板下方显示免疫宁成色效果速查",
            false, false, (ValueValidator)null, (string)null);

        _cfgUIScale = _cfgCategory.CreateEntry<float>(
            "UIScale", 0f, "UI 缩放",
            "0=自动按屏幕高度缩放（1080p≈1.2）。手动值 0.5~3.0。改后需重启。",
            false, false, (ValueValidator)null, (string)null);

        CfgReducePoolOnGenuineInject = _cfgCategory.CreateEntry<bool>(
            "ReducePoolOnGenuineInject", false, "真品免疫宁 -1 损伤池",
            "每注射一支真品免疫宁，损伤池 -1（原版不降）。改后需重启。",
            false, false, (ValueValidator)null, (string)null);

        _cfgCategory.SaveToFile(true);
        _isUIVisible = _cfgShowUI.Value;

        _windowRect.x = PlayerPrefs.GetFloat(PP_PANEL_X, 20f);
        _windowRect.y = PlayerPrefs.GetFloat(PP_PANEL_Y, 20f);
        _windowRect.width = Mathf.Clamp(_cfgPanelWidth.Value, PANEL_MIN_W, PANEL_MAX_W);
        _windowRect.height = Mathf.Clamp(_cfgPanelHeight.Value, PANEL_MIN_H, PANEL_MAX_H);

        try
        {
            new HarmonyLib.Harmony("SentinelTracker.Patches")
                .PatchAll(typeof(Core).Assembly);
            LoggerInstance.Msg("Harmony patch 已应用");
        }
        catch (Exception ex)
        {
            LoggerInstance.Error("Harmony patch 失败: " + ex);
        }

        LoggerInstance.Msg($"v1.4.0 已加载，按 {_cfgToggleKey.Value} 开关面板");
    }

    // ══════════════════════════════════════════════════════
    //  缩放系数
    // ══════════════════════════════════════════════════════
    private float ResolveScale()
    {
        float s;
        if (_cfgUIScale != null && _cfgUIScale.Value > 0f)
            s = Mathf.Clamp(_cfgUIScale.Value, 0.5f, 3.0f);
        else
            s = Mathf.Clamp(Screen.height / 1080f * 1.2f, 0.75f, 3.0f);

        // 归整到 0.25 的倍数，避免非整数缩放带来的视觉抖动
        return Mathf.Round(s * 4f) / 4f;
    }

    /// <summary>把逻辑坐标 / 尺寸按当前 scale 转成屏幕坐标。</summary>
    private Rect R(float x, float y, float w, float h)
        => new Rect(x * _S, y * _S, w * _S, h * _S);

    // ══════════════════════════════════════════════════════
    //  样式（延迟到 OnGUI，随 scale 变化重建）
    // ══════════════════════════════════════════════════════
    private void EnsureStyles(float S)
    {
        if (_stylesReady && Mathf.Abs(_stylesScale - S) < 0.01f) return;
        _stylesScale = S;

        int fsTitle = Mathf.RoundToInt(16 * S);
        int fsData = Mathf.RoundToInt(14 * S);
        int fsTip = Mathf.RoundToInt(13 * S);
        int fsHotkey = Mathf.RoundToInt(11 * S);
        int fsClose = Mathf.RoundToInt(14 * S);

        _titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = fsTitle,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
        };

        _dataStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = fsData,
            alignment = TextAnchor.MiddleCenter,
        };
        _dataStyle.normal.textColor = Color.white;

        _tipStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = fsTip,
            alignment = TextAnchor.MiddleCenter,
        };
        _tipStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f);

        _hotkeyStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = fsHotkey,
            alignment = TextAnchor.MiddleRight,
        };
        _hotkeyStyle.normal.textColor = new Color(0.5f, 0.5f, 0.5f);

        _waitStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = fsData,
            alignment = TextAnchor.MiddleCenter,
        };
        _waitStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);

        _barStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = fsData,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
        };
        _barStyle.normal.textColor = Color.white;

        _warnStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = fsData,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
        };

        _closeStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = fsClose,
            alignment = TextAnchor.MiddleCenter,
        };

        _handleStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = fsClose,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
        };
        _handleStyle.normal.textColor = new Color(1f, 0.95f, 0.6f);
        _handleStyle.hover.textColor = Color.white;
        _handleStyle.active.textColor = new Color(0.6f, 1f, 0.6f);

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
            _S = ResolveScale();
            EnsureStyles(_S);

            GUI.depth = 33000;
            float panelAlpha = Mathf.Clamp(_cfgPanelOpacity?.Value ?? 0.9f, 0.1f, 1f);
            GUI.color = new Color(1f, 1f, 1f, panelAlpha);

            HandleResizeInput();

            bool hideForNonSentinel =
                _dataReady &&
                !_isSentinel &&
                _cfgShowOnlyWhenSentinel != null &&
                _cfgShowOnlyWhenSentinel.Value;

            if (!hideForNonSentinel)
            {
                // 屏幕坐标交给 GUI.Window；返回后再转回逻辑坐标
                Rect scaledRect = R(_windowRect.x, _windowRect.y,
                                    _windowRect.width, _windowRect.height);

                Rect result = GUI.Window(
                    98765, scaledRect,
                    (Action<int>)DrawWindow,
                    "哨兵综合症监控");

                _windowRect.x = result.x / _S;
                _windowRect.y = result.y / _S;
                _windowRect.width = result.width / _S;
                _windowRect.height = result.height / _S;

                DrawResizeHandle();
            }

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
    //  鼠标 / 缩放
    // ══════════════════════════════════════════════════════
    /// <summary>返回 IMGUI 逻辑坐标（已除以 _S）。</summary>
    private Vector2 GetGuiMousePosition()
    {
        float s = _S > 0f ? _S : 1f;
        return new Vector2(
            Input.mousePosition.x / s,
            (Screen.height - Input.mousePosition.y) / s);
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
            float hx = (_windowRect.x + _windowRect.width - HANDLE_SIZE + 2f) * _S;
            float hy = (_windowRect.y + _windowRect.height - HANDLE_SIZE + 2f) * _S;
            GUI.Button(new Rect(hx, hy, 18f * _S, 18f * _S), "◢", _handleStyle);
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
    //  主绘制（所有 Rect 都走 R() 以乘 S）
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
            GUIStyle closeStyle = _closeStyle ?? GUI.skin.button;

            float W = _windowRect.width;
            float H = _windowRect.height;

            // 标题栏拖动
            GUI.DragWindow(R(0f, 0f, W - 30f, 30f));

            // 关闭按钮
            if (GUI.Button(R(W - 25f, 2f, 20f, 20f), "×", closeStyle))
            {
                _isUIVisible = false;
                _cfgShowUI.Value = false;
                MelonPreferences.Save();
            }

            Color savedColor = GUI.color;
            float textAlpha = Mathf.Clamp(_cfgTextOpacity?.Value ?? 1f, 0.3f, 1f);
            GUI.color = new Color(1f, 1f, 1f, textAlpha);

            float contentW = Mathf.Min(W - 24f, 320f);
            float contentX = (W - contentW) * 0.5f;
            float y = 34f;

            // ── 未就绪 ───────────────────────────────────
            if (!_dataReady)
            {
                GUI.Label(R(contentX, y + 60f, contentW, 24f),
                    "等待进入游戏或加载存档...", waitStyle);
                GUI.Label(R(contentX, H - 24f, contentW, 18f),
                    $"快捷键: {_cfgToggleKey.Value} 开关面板", hotkeyStyle);
                GUI.color = savedColor;
                return;
            }

            // ── 非哨兵 ───────────────────────────────────
            if (!_isSentinel)
            {
                titleStyle.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
                GUI.Label(R(contentX, y, contentW, 24f), "未选择哨兵 perk", titleStyle);
                y += 32f;
                GUI.Label(R(contentX, y, contentW, 22f), "哨兵机制未激活", tipStyle);
                y += 26f;
                GUI.Label(R(contentX, y, contentW, 22f),
                    "（选择 sentinel 起始 perk 后生效）", tipStyle);
                y += 26f;
                GUI.Label(R(contentX, y, contentW, 22f),
                    "原版哨兵：只涨池，不掉血；池 ≥ 20 触发哨兵崩溃", tipStyle);

                GUI.Label(R(contentX, H - 24f, contentW, 18f),
                    $"快捷键: {_cfgToggleKey.Value} · 池只增不减", hotkeyStyle);
                GUI.color = savedColor;
                return;
            }

            // ── 阶段标题 ─────────────────────────────────
            string phaseText = GetPhaseText(_displayD);
            titleStyle.normal.textColor = GetPhaseColor(_displayD);
            GUI.Label(R(contentX, y, contentW, 24f), phaseText, titleStyle);
            y += 32f;

            // ── 天数 ─────────────────────────────────────
            GUI.Label(R(contentX, y, contentW, 22f),
                $"距离上次打针：{_displayD} 天", dataStyle);
            y += 26f;

            // ── 涨速 ─────────────────────────────────────
            int rate = GetPoolRate(_displayD);
            string rateText;
            if (rate == 0)
                rateText = (_displayD == 6) ? "今晚仅弹提示，不涨池" : "当前涨速：0 / 晚";
            else
                rateText = $"当前涨速：+{rate} / 晚";
            GUI.Label(R(contentX, y, contentW, 22f), rateText, tipStyle);
            y += 26f;

            // ── 池进度 ───────────────────────────────────
            int pool = Mathf.Clamp(_displayPool, 0, POOL_MAX);
            barStyle.normal.textColor = GetPoolColor(pool);
            GUI.Label(R(contentX, y, contentW, 22f),
                $"损伤池：{pool} / {POOL_MAX}", barStyle);
            y += 26f;
            GUI.Label(R(contentX, y, contentW, 22f),
                BuildProgressBar(pool, POOL_MAX, 20), barStyle);
            y += 30f;

            // ── 死亡倒计时 ───────────────────────────────
            int nightsLeft = NightsToDeath(_displayD, _displayPool);
            if (nightsLeft == 0)
            {
                warnStyle.normal.textColor = new Color(1f, 0f, 0f);
                GUI.Label(R(contentX, y, contentW, 24f),
                    "⚠ 池已满，本晚必死", warnStyle);
            }
            else if (nightsLeft > 0)
            {
                warnStyle.normal.textColor = GetCountdownColor(nightsLeft);
                GUI.Label(R(contentX, y, contentW, 24f),
                    $"若不注射，预计 {nightsLeft} 晚后哨兵崩溃", warnStyle);
            }
            else
            {
                GUI.Label(R(contentX, y, contentW, 24f),
                    "不会崩溃（当前涨速为 0）", tipStyle);
            }
            y += 30f;

            // ── 建议 ─────────────────────────────────────
            GUI.Label(R(contentX, y, contentW, 22f),
                GetAdvice(_displayD, _displayPool, rate), tipStyle);
            y += 28f;

            // ── 免疫宁提示 ───────────────────────────────
            if (_cfgShowImmunivaxHint != null && _cfgShowImmunivaxHint.Value)
            {
                y += 6f;
                GUI.Label(R(contentX, y, contentW, 20f),
                    "免疫宁效果（按成色）", tipStyle);
                y += 22f;
                GUI.Label(R(contentX, y, contentW, 20f),
                    "真品 → 天数 0 ｜ 过期 → 3 ｜ 假货 → 3/4/5", tipStyle);
                y += 22f;
                GUI.Label(R(contentX, y, contentW, 20f),
                    "※ 任何针都不降损伤池", tipStyle);
                y += 22f;
            }

            // ── v1.2.0：真品 -1 池 提示 ──────────────────
            if (CfgReducePoolOnGenuineInject != null && CfgReducePoolOnGenuineInject.Value)
            {
                y += 4f;
                GUI.Label(R(contentX, y, contentW, 20f),
                    "★ 真品免疫宁：每次 -1 损伤池", tipStyle);
            }

            // ── 底部提示 ─────────────────────────────────
            GUI.Label(R(contentX, H - 24f, contentW, 18f),
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

    private static string BuildProgressBar(int value, int max, int width)
    {
        int filled = Mathf.Clamp(Mathf.RoundToInt((float)value / max * width), 0, width);
        int empty = width - filled;
        return "[" + new string('#', filled) + new string('-', empty) + "]";
    }

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

        return "💡 已开始涨池，打真品针可重置天数（池不变）。";
    }
}